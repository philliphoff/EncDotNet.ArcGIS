<#
.SYNOPSIS
Installs the SoundCharts add-in and its features plugin for the current
user, or removes them.

.DESCRIPTION
Copies the .esriAddinX to ArcGIS Pro's per-user add-in folder,
<Documents>\ArcGIS\AddIns\ArcGISPro\{add-in id}\, and the SoundCharts
features plugin datasource (.esriPlugin) to
<Documents>\ArcGIS\AddIns\ArcGISPro<version>\{plugin id}\, which is where
RegisterAddIn.exe puts each: a plugin goes in the folder of the Pro version
its Config.xml targets. The Documents folder is resolved with
Environment.GetFolderPath, so a redirected Documents folder (OneDrive, a
network share, a VM's shared folder) is followed.

Use it when the build's own deploy step can't run RegisterAddIn.exe: Esri's
packaging targets run it by name from Pro's bin folder, which fails when Pro
isn't installed, when the build doesn't use Visual Studio's MSBuild, or when
NoDefaultCurrentDirectoryInExePath is set. Also use it to install packages
downloaded from CI.

Pro reads add-ins and plugins when it starts, so restart Pro after
deploying.

.PARAMETER Path
The .esriAddinX to install. Defaults to the add-in built by
`msbuild EncDotNet.ArcGIS.slnx /restore` in the given configuration.

.PARAMETER PluginPath
The .esriPlugin to install. Defaults to the plugin built beside the add-in;
skipped with a warning when it isn't there.

.PARAMETER Configuration
The build configuration to take the default packages from: Release
(default) or Debug.

.PARAMETER Remove
Removes the installed add-in and plugin instead of installing them.

.EXAMPLE
tools/deploy-addin.ps1

.EXAMPLE
tools/deploy-addin.ps1 -Path ~/Downloads/EncDotNet.ArcGIS.SoundCharts.esriAddinX -PluginPath ~/Downloads/EncDotNet.ArcGIS.SoundCharts.Features.esriPlugin

.EXAMPLE
tools/deploy-addin.ps1 -Remove
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $Path,
    [string] $PluginPath,
    [ValidateSet('Release', 'Debug')]
    [string] $Configuration = 'Release',
    [switch] $Remove
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression.FileSystem

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectDir = Join-Path $repoRoot 'src/EncDotNet.ArcGIS.SoundCharts'
$pluginDir = Join-Path $repoRoot 'src/EncDotNet.ArcGIS.SoundCharts.Features'

function Get-AddInId([xml] $daml) {
    $id = $daml.ArcGIS.AddInInfo.id
    if (-not $id) {
        throw 'Config.daml has no AddInInfo id.'
    }
    return $id
}

# The plugin's id and the Pro version folder RegisterAddIn.exe installs it in.
function Get-PluginTarget([xml] $config) {
    $id = $config.'ESRI.Configuration'.AddInID
    $version = $config.'ESRI.Configuration'.Targets.Target.version
    if (-not $id -or -not $version) {
        throw 'Config.xml has no AddInID or Target version.'
    }
    return @{ Id = $id; Folder = "ArcGISPro$version" }
}

function Read-PackageEntry([string] $package, [string] $name) {
    $zip = [System.IO.Compression.ZipFile]::OpenRead($package)
    try {
        $entry = $zip.GetEntry($name)
        if (-not $entry) {
            throw "$package has no $name; it isn't the expected package."
        }
        $reader = New-Object System.IO.StreamReader($entry.Open())
        try {
            return $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $zip.Dispose()
    }
}

function Install-Package([string] $package, [string] $target, [string] $filter) {
    if ($PSCmdlet.ShouldProcess($target, "Install $(Split-Path -Leaf $package)")) {
        New-Item -ItemType Directory -Force -Path $target | Out-Null
        # One package per folder: drop any earlier file name.
        Get-ChildItem -LiteralPath $target -Filter $filter | Remove-Item -Force
        Copy-Item -LiteralPath $package -Destination $target
        Write-Host "Installed $(Split-Path -Leaf $package) to $target"
    }
}

function Remove-Folder([string] $target, [string] $what) {
    if (Test-Path -LiteralPath $target) {
        if ($PSCmdlet.ShouldProcess($target, "Remove $what")) {
            Remove-Item -LiteralPath $target -Recurse -Force
            Write-Host "Removed $target"
        }
    }
    else {
        Write-Host "Not installed: $target"
    }
}

$documents = [Environment]::GetFolderPath([Environment+SpecialFolder]::MyDocuments)
$addInsRoot = Join-Path $documents 'ArcGIS\AddIns'

if ($Remove) {
    $id = Get-AddInId ([xml] (Get-Content -Raw (Join-Path $projectDir 'Config.daml')))
    Remove-Folder (Join-Path $addInsRoot "ArcGISPro\$id") 'add-in'
    $plugin = Get-PluginTarget ([xml] (Get-Content -Raw (Join-Path $pluginDir 'Config.xml')))
    Remove-Folder (Join-Path $addInsRoot "$($plugin.Folder)\$($plugin.Id)") 'plugin'
    return
}

if (-not $Path) {
    $Path = Join-Path $projectDir "bin/$Configuration/net10.0-windows/win-x64/EncDotNet.ArcGIS.SoundCharts.esriAddinX"
}

if (-not (Test-Path -LiteralPath $Path)) {
    throw "No add-in at $Path. Build it first: msbuild EncDotNet.ArcGIS.slnx /restore /p:Configuration=$Configuration"
}

$Path = (Resolve-Path -LiteralPath $Path).Path

# Read the ids from the packages' own Config.daml and Config.xml, so a
# downloaded package installs under its id even if it differs from the
# source tree's.
$id = Get-AddInId ([xml] (Read-PackageEntry $Path 'Config.daml'))
Install-Package $Path (Join-Path $addInsRoot "ArcGISPro\$id") '*.esriAddinX'

$pluginGiven = [bool] $PluginPath
if (-not $PluginPath) {
    $PluginPath = Join-Path $pluginDir "bin/$Configuration/net10.0-windows/win-x64/EncDotNet.ArcGIS.SoundCharts.Features.esriPlugin"
}

if (Test-Path -LiteralPath $PluginPath) {
    $PluginPath = (Resolve-Path -LiteralPath $PluginPath).Path
    $plugin = Get-PluginTarget ([xml] (Read-PackageEntry $PluginPath 'Config.xml'))
    Install-Package $PluginPath (Join-Path $addInsRoot "$($plugin.Folder)\$($plugin.Id)") '*.esriPlugin'
}
elseif ($pluginGiven) {
    throw "No plugin at $PluginPath."
}
else {
    Write-Warning "No features plugin at $PluginPath; Add as Feature Layers won't work until it's installed."
}

if (Get-Process -Name ArcGISPro -ErrorAction SilentlyContinue) {
    Write-Warning 'ArcGIS Pro is running. Restart it to load the new add-in.'
}
