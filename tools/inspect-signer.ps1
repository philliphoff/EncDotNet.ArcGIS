#Requires -Version 7
<#
.SYNOPSIS
    Lists what ArcGIS Pro's add-in signing tool needs from a Pro install.

.DESCRIPTION
    Starting from ArcGISSignAddIn (the .exe, or the .dll beside it when the
    .exe is a .NET app host), follows every static dependency: .NET assembly
    references, P/Invoke targets, and native imports, delay-loaded ones
    included. Each is reported by where it resolved:

      pro        in Pro's bin folder: has to travel with the signer
      framework  in a .NET shared framework (the runtime provides it)
      windows    in System32 or an API set (Windows provides it)
      vcredist   the Visual C++ runtime, from System32 (the runner has it)
      missing    found nowhere: GAC, a Pro subfolder, or loaded another way

    Anything loaded at run time by name (NativeLibrary.Load, Assembly.Load,
    reflection, satellite resources) or read from the registry won't show
    here. Run the copied signer under Process Monitor for those (see
    tools/inspect-signer.md).

.PARAMETER ProBin
    Pro's bin folder. Defaults to InstallDir from the registry, plus bin.

.PARAMETER CopyTo
    Copies the signer and every "pro" dependency (with the entry point's
    .runtimeconfig.json, .deps.json and .config) into this folder, for a
    run on a machine without Pro.

.PARAMETER Report
    Writes the full report as JSON to this file.

.EXAMPLE
    pwsh tools/inspect-signer.ps1 -CopyTo C:\signer -Report signer.json
#>
[CmdletBinding()]
param(
    [string] $ProBin,
    [string] $Tool = 'ArcGISSignAddIn',
    [string] $CopyTo,
    [string] $Report
)

$ErrorActionPreference = 'Stop'

Add-Type -Language CSharp -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

public sealed class PeInfo
{
    public string Path;
    public string Machine;
    public bool IsManaged;
    public bool IsIlOnly;
    public string AssemblyName;
    public string TargetFramework;
    public List<string> AssemblyReferences = new List<string>();
    public List<string> PInvokeModules = new List<string>();
    public List<string> NativeImports = new List<string>();
    public List<string> DelayImports = new List<string>();

    public static PeInfo Read(string path)
    {
        var info = new PeInfo { Path = path };
        using (var stream = File.OpenRead(path))
        using (var pe = new PEReader(stream))
        {
            var headers = pe.PEHeaders;
            info.Machine = headers.CoffHeader.Machine.ToString();

            if (headers.PEHeader != null)
            {
                ReadImports(pe, headers.PEHeader.ImportTableDirectory, 20, 12, info.NativeImports);
                // IMAGE_DELAYLOAD_DESCRIPTOR: the DLL name RVA is at offset 4.
                ReadImports(pe, headers.PEHeader.DelayImportTableDirectory, 32, 4, info.DelayImports);
            }

            if (pe.HasMetadata)
            {
                info.IsManaged = true;
                info.IsIlOnly = (headers.CorHeader.Flags & CorFlags.ILOnly) != 0;
                var md = pe.GetMetadataReader();

                if (md.IsAssembly)
                {
                    var def = md.GetAssemblyDefinition();
                    info.AssemblyName = md.GetString(def.Name) + ", " + def.Version;
                    info.TargetFramework = ReadTargetFramework(md, def.GetCustomAttributes());
                }

                foreach (var handle in md.AssemblyReferences)
                {
                    var r = md.GetAssemblyReference(handle);
                    info.AssemblyReferences.Add(md.GetString(r.Name) + ", " + r.Version);
                }

                var modules = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var handle in md.MethodDefinitions)
                {
                    var method = md.GetMethodDefinition(handle);
                    if ((method.Attributes & MethodAttributes.PinvokeImpl) == 0) continue;
                    var import = method.GetImport();
                    if (import.Module.IsNil) continue;
                    modules.Add(md.GetString(md.GetModuleReference(import.Module).Name));
                }
                info.PInvokeModules.AddRange(modules);
            }
        }
        return info;
    }

    static void ReadImports(PEReader pe, DirectoryEntry dir, int size, int nameOffset, List<string> names)
    {
        if (dir.RelativeVirtualAddress == 0 || dir.Size == 0) return;
        var table = pe.GetSectionData(dir.RelativeVirtualAddress);
        for (int offset = 0; offset + size <= table.Length; offset += size)
        {
            var entry = table.GetContent(offset, size);
            bool empty = true;
            foreach (var b in entry) if (b != 0) { empty = false; break; }
            if (empty) break;

            int nameRva = entry[nameOffset] | entry[nameOffset + 1] << 8
                | entry[nameOffset + 2] << 16 | entry[nameOffset + 3] << 24;
            if (nameRva == 0) continue;
            var name = pe.GetSectionData(nameRva);
            var chars = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length && i < 512; i++)
            {
                byte c = name.GetContent(i, 1)[0];
                if (c == 0) break;
                chars.Append((char)c);
            }
            names.Add(chars.ToString());
        }
    }

    static string ReadTargetFramework(MetadataReader md, CustomAttributeHandleCollection attributes)
    {
        foreach (var handle in attributes)
        {
            var attribute = md.GetCustomAttribute(handle);
            if (attribute.Constructor.Kind != HandleKind.MemberReference) continue;
            var ctor = md.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
            if (ctor.Parent.Kind != HandleKind.TypeReference) continue;
            var type = md.GetTypeReference((TypeReferenceHandle)ctor.Parent);
            if (md.GetString(type.Name) != "TargetFrameworkAttribute") continue;
            var blob = md.GetBlobReader(attribute.Value);
            if (blob.ReadUInt16() != 1) continue;
            return blob.ReadSerializedString();
        }
        return null;
    }
}
'@

if (-not $ProBin) {
    $installDir = (Get-ItemProperty 'HKLM:\SOFTWARE\ESRI\ArcGISPro' -ErrorAction SilentlyContinue).InstallDir
    if (-not $installDir) { throw "ArcGIS Pro's install folder isn't in the registry: pass -ProBin." }
    $ProBin = Join-Path $installDir 'bin'
}
$ProBin = (Resolve-Path $ProBin).Path

# A .NET (Core) app is an app host .exe beside the .dll with the code.
$exe = Join-Path $ProBin "$Tool.exe"
$dll = Join-Path $ProBin "$Tool.dll"
if (-not (Test-Path $exe)) { throw "$exe doesn't exist." }

# Where non-Pro files resolve: every installed shared framework (newest
# version of each), and System32.
$dotnetRoot = if ($env:DOTNET_ROOT) { $env:DOTNET_ROOT } else { Join-Path $env:ProgramFiles 'dotnet' }
$frameworkDirs = @(
    Get-ChildItem (Join-Path $dotnetRoot 'shared') -Directory -ErrorAction SilentlyContinue |
        ForEach-Object {
            Get-ChildItem $_.FullName -Directory |
                Sort-Object { try { [version]($_.Name -replace '-.*$', '') } catch { [version]'0.0' } } |
                Select-Object -Last 1
        }
)
$system32 = [Environment]::SystemDirectory

function Resolve-Dependency([string] $name, [bool] $managed) {
    # Assembly references are bare names; P/Invoke names may omit .dll.
    $file = if (-not $managed -and $name -match '\.(dll|exe|drv|sys)$') { $name } else { "$name.dll" }

    $inPro = Join-Path $ProBin $file
    if (Test-Path $inPro) { return [pscustomobject]@{ Source = 'pro'; Path = $inPro } }

    if ($name -match '^(api|ext)-ms-') { return [pscustomobject]@{ Source = 'windows'; Path = $null } }

    foreach ($dir in $frameworkDirs) {
        $candidate = Join-Path $dir.FullName $file
        if (Test-Path $candidate) {
            return [pscustomobject]@{ Source = "framework ($($dir.Parent.Name) $($dir.Name))"; Path = $candidate }
        }
    }

    $inWindows = if ($system32) { Join-Path $system32 $file }
    if ($inWindows -and (Test-Path $inWindows)) {
        $source = if ($file -match '^(vcruntime|msvcp|vccorlib|concrt|ucrtbase)') { 'vcredist' } else { 'windows' }
        return [pscustomobject]@{ Source = $source; Path = $inWindows }
    }

    return [pscustomobject]@{ Source = 'missing'; Path = $null }
}

$entries = @($exe)
if (Test-Path $dll) { $entries += $dll }

$seen = @{}        # path -> PeInfo, for files under Pro's bin
$results = @{}     # dependency key -> report row
$queue = [System.Collections.Generic.Queue[string]]::new()
foreach ($entry in $entries) { $queue.Enqueue($entry) }

while ($queue.Count -gt 0) {
    $path = $queue.Dequeue()
    if ($seen.ContainsKey($path)) { continue }
    $info = [PeInfo]::Read($path)
    $seen[$path] = $info
    $from = Split-Path $path -Leaf

    $deps = @(
        $info.AssemblyReferences | ForEach-Object { [pscustomobject]@{ Name = ($_ -split ',')[0]; Kind = 'assembly'; Managed = $true } }
        $info.PInvokeModules     | ForEach-Object { [pscustomobject]@{ Name = $_; Kind = 'pinvoke'; Managed = $false } }
        $info.NativeImports      | ForEach-Object { [pscustomobject]@{ Name = $_; Kind = 'import'; Managed = $false } }
        $info.DelayImports       | ForEach-Object { [pscustomobject]@{ Name = $_; Kind = 'delay-import'; Managed = $false } }
    )

    foreach ($dep in $deps) {
        $key = "$($dep.Name)|$($dep.Managed)".ToLowerInvariant()
        if (-not $results.ContainsKey($key)) {
            $resolved = Resolve-Dependency $dep.Name $dep.Managed
            $results[$key] = [pscustomobject]@{
                Name = $dep.Name
                Kind = $dep.Kind
                Source = $resolved.Source
                Path = $resolved.Path
                UsedBy = [System.Collections.Generic.List[string]]::new()
            }
            if ($resolved.Source -eq 'pro') { $queue.Enqueue($resolved.Path) }
        }
        if (-not $results[$key].UsedBy.Contains($from)) { $results[$key].UsedBy.Add($from) }
    }
}

# The entry point.
$main = if (Test-Path $dll) { $seen[$dll] } else { $seen[$exe] }
Write-Host "Signer:            $exe"
Write-Host "Code:              $($main.Path)"
Write-Host "Managed:           $($main.IsManaged) (IL only: $($main.IsIlOnly))"
Write-Host "Target framework:  $($main.TargetFramework)"
Write-Host "Machine:           $($main.Machine)"

$sideFiles = @("$Tool.runtimeconfig.json", "$Tool.deps.json", "$Tool.exe.config", "$Tool.dll.config") |
    ForEach-Object { Join-Path $ProBin $_ } | Where-Object { Test-Path $_ }
foreach ($side in $sideFiles) { Write-Host "Side file:         $side" }
$runtimeConfig = Join-Path $ProBin "$Tool.runtimeconfig.json"
if (Test-Path $runtimeConfig) {
    Write-Host ''
    Get-Content $runtimeConfig | Write-Host
}

# Mixed-mode (C++/CLI) assemblies under Pro's bin are worth knowing about:
# they bring native imports and the VC++ runtime with them.
$mixed = $seen.Values | Where-Object { $_.IsManaged -and -not $_.IsIlOnly }
if ($mixed) {
    Write-Host ''
    Write-Host 'Mixed-mode (C++/CLI) assemblies:'
    $mixed | ForEach-Object { Write-Host "  $(Split-Path $_.Path -Leaf)" }
}

$rows = $results.Values | Sort-Object @{ Expression = {
        switch ($_.Source) { 'missing' { 0 } 'pro' { 1 } 'vcredist' { 2 } default { 3 } } } }, Name
Write-Host ''
$rows | Format-Table Name, Kind, Source, @{ Label = 'UsedBy'; Expression = { $_.UsedBy -join ', ' } } -AutoSize -Wrap | Out-String -Width 200 | Write-Host

$proFiles = @($seen.Keys | Sort-Object)
$size = ($proFiles | ForEach-Object { (Get-Item $_).Length } | Measure-Object -Sum).Sum
Write-Host ("{0} files from Pro's bin, {1:N1} MB. Missing: {2}." -f $proFiles.Count, ($size / 1MB), @($rows | Where-Object Source -eq 'missing').Count)

if ($Report) {
    [pscustomobject]@{
        Signer = $exe
        Entry = $main
        SideFiles = $sideFiles
        ProFiles = $proFiles
        Dependencies = $rows
    } | ConvertTo-Json -Depth 5 | Set-Content -Path $Report -Encoding utf8
    Write-Host "Report: $Report"
}

if ($CopyTo) {
    New-Item -ItemType Directory -Force -Path $CopyTo | Out-Null
    foreach ($file in $proFiles + $sideFiles) { Copy-Item $file -Destination $CopyTo -Force }
    Write-Host "Copied $(($proFiles + $sideFiles).Count) files to $CopyTo."
}
