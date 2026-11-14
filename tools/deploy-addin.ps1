<#
.SYNOPSIS
Installs the SoundCharts add-in for the current user, or removes it.

.DESCRIPTION
Copies an .esriAddinX to ArcGIS Pro's per-user add-in folder,
<Documents>\ArcGIS\AddIns\ArcGISPro\{add-in id}\, which is where
RegisterAddIn.exe puts it. The Documents folder is resolved with
Environment.GetFolderPath, so a redirected Documents folder (OneDrive, a
network share, a VM's shared folder) is followed.

Use it when the build's own deploy step can't run RegisterAddIn.exe: Esri's
packaging targets run it by name from Pro's bin folder, which fails when Pro
isn't installed, when the build doesn't use Visual Studio's MSBuild, or when
NoDefaultCurrentDirectoryInExePath is set. Also use it to install an
.esriAddinX downloaded from CI.

Pro reads add-ins when it starts, so restart Pro after deploying.

.PARAMETER Path
The .esriAddinX to install. Defaults to the add-in built by
`msbuild EncDotNet.ArcGIS.slnx /restore` in the given configuration.

.PARAMETER Configuration
The build configuration to take the default .esriAddinX from: Release
(default) or Debug.

.PARAMETER Remove
Removes the installed add-in instead of installing it.

.EXAMPLE
tools/deploy-addin.ps1

.EXAMPLE
tools/deploy-addin.ps1 -Path ~/Downloads/EncDotNet.ArcGIS.SoundCharts.esriAddinX

.EXAMPLE
tools/deploy-addin.ps1 -Remove
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $Path,
    [ValidateSet('Release', 'Debug')]
    [string] $Configuration = 'Release',
    [switch] $Remove
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression.FileSystem

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectDir = Join-Path $repoRoot 'src/EncDotNet.ArcGIS.SoundCharts'

function Get-AddInId([xml] $daml) {
    $id = $daml.ArcGIS.AddInInfo.id
    if (-not $id) {
        throw 'Config.daml has no AddInInfo id.'
    }
    return $id
}

$documents = [Environment]::GetFolderPath([Environment+SpecialFolder]::MyDocuments)
$addInsRoot = Join-Path $documents 'ArcGIS\AddIns\ArcGISPro'

if ($Remove) {
    $id = Get-AddInId ([xml] (Get-Content -Raw (Join-Path $projectDir 'Config.daml')))
    $target = Join-Path $addInsRoot $id
    if (Test-Path -LiteralPath $target) {
        if ($PSCmdlet.ShouldProcess($target, 'Remove add-in')) {
            Remove-Item -LiteralPath $target -Recurse -Force
            Write-Host "Removed $target"
        }
    }
    else {
        Write-Host "Not installed: $target"
    }
    return
}

if (-not $Path) {
    $Path = Join-Path $projectDir "bin/$Configuration/net10.0-windows/win-x64/EncDotNet.ArcGIS.SoundCharts.esriAddinX"
}

if (-not (Test-Path -LiteralPath $Path)) {
    throw "No add-in at $Path. Build it first: msbuild EncDotNet.ArcGIS.slnx /restore /p:Configuration=$Configuration"
}

$Path = (Resolve-Path -LiteralPath $Path).Path

# Read the add-in id from the package's own Config.daml, so a downloaded
# package installs under its id even if it differs from the source tree's.
$zip = [System.IO.Compression.ZipFile]::OpenRead($Path)
try {
    $entry = $zip.GetEntry('Config.daml')
    if (-not $entry) {
        throw "$Path has no Config.daml; it isn't an add-in package."
    }
    $reader = New-Object System.IO.StreamReader($entry.Open())
    try {
        $id = Get-AddInId ([xml] $reader.ReadToEnd())
    }
    finally {
        $reader.Dispose()
    }
}
finally {
    $zip.Dispose()
}

$target = Join-Path $addInsRoot $id
if ($PSCmdlet.ShouldProcess($target, "Install $(Split-Path -Leaf $Path)")) {
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    # One package per add-in folder: drop any earlier file name.
    Get-ChildItem -LiteralPath $target -Filter '*.esriAddinX' | Remove-Item -Force
    Copy-Item -LiteralPath $Path -Destination $target
    Write-Host "Installed $(Split-Path -Leaf $Path) to $target"

    if (Get-Process -Name ArcGISPro -ErrorAction SilentlyContinue) {
        Write-Warning 'ArcGIS Pro is running. Restart it to load the new add-in.'
    }
}
