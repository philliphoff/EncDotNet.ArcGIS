<#
.SYNOPSIS
Builds the pinned s100 chart server that the add-in bundles.

.DESCRIPTION
SoundCharts draws charts from `s100 tiles serve`, run out of process. The
add-in ships a pinned s100 build (design brief D21), one for each Windows
runtime: win-arm64 runs natively on Arm64 Windows, win-x64 everywhere else.

No EncDotNet.S100 release serves WMTS yet (v0.27.0 serves XYZ only), and the
nightly release's assets are replaced every night, so the pin is a commit:
tools/s100-server.json names the repository, the commit and the version to
stamp. This script fetches that commit and publishes the CLI self-contained
for each runtime into artifacts/server/<rid>/. The add-in project packages
whatever it finds there as server/<rid>/ in the .esriAddinX.

Unlike EncDotNet.S100's release archives, the publish is a compressed single
file with English resources only and no symbols or XML docs: about 80 MB a
runtime instead of 250 MB, since the add-in carries two. The native
libraries (SkiaSharp, SQLite) stay beside s100.exe rather than being
extracted to a temporary folder at start.

Run it once before building the add-in; it does nothing for a runtime that
is already built at the pinned commit. CI runs it too, and caches the result
by commit.

.PARAMETER RuntimeIdentifier
The runtimes to build. Defaults to every runtime in tools/s100-server.json.

.PARAMETER Output
The folder to publish into, one subfolder per runtime. Defaults to
artifacts/server.

.PARAMETER SourceDirectory
Where to fetch EncDotNet.S100. Defaults to artifacts/s100-src. Keep it short:
the upstream build writes deep paths, and Windows paths stop at 260
characters unless long paths are enabled.

.PARAMETER RestoreConfigFile
A NuGet.config to restore with, for machines that reach NuGet through a
mirror instead of api.nuget.org.

.PARAMETER RollForwardLatestMajor
Lets the upstream global.json roll forward to a newer major SDK, for machines
with only a later .NET SDK installed. The change stays in the fetched copy.

.PARAMETER Force
Rebuilds even when the output is already at the pinned commit.

.EXAMPLE
tools/build-server.ps1

.EXAMPLE
tools/build-server.ps1 -RuntimeIdentifier win-x64 -RestoreConfigFile ~/mirror.nuget.config -RollForwardLatestMajor
#>
[CmdletBinding()]
param(
    [string[]] $RuntimeIdentifier,
    [string] $Output,
    [string] $SourceDirectory,
    [string] $RestoreConfigFile,
    [switch] $RollForwardLatestMajor,
    [switch] $Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$pin = Get-Content -Raw (Join-Path $PSScriptRoot 's100-server.json') | ConvertFrom-Json

if (-not $RuntimeIdentifier) { $RuntimeIdentifier = $pin.runtimeIdentifiers }
if (-not $Output) { $Output = Join-Path $repoRoot 'artifacts/server' }
if (-not $SourceDirectory) { $SourceDirectory = Join-Path $repoRoot 'artifacts/s100-src' }

# The marker each runtime folder carries: the commit it was built from.
$markerName = 's100-server.commit'

function Invoke-Native {
    param([string] $FilePath, [string[]] $Arguments)
    # Native tools write progress to stderr; only the exit code means failure.
    $ErrorActionPreference = 'Continue'
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

$pending = @($RuntimeIdentifier | Where-Object {
    $marker = Join-Path (Join-Path $Output $_) $markerName
    $Force -or -not (Test-Path -LiteralPath $marker) -or ((Get-Content -Raw -LiteralPath $marker).Trim() -ne $pin.commit)
})

if ($pending.Count -eq 0) {
    Write-Host "s100 $($pin.version) ($($pin.commit.Substring(0, 7))) is already built for $($RuntimeIdentifier -join ', ')."
    return
}

# Fetch only the pinned commit.
if (-not (Test-Path -LiteralPath (Join-Path $SourceDirectory '.git'))) {
    New-Item -ItemType Directory -Force -Path $SourceDirectory | Out-Null
    Invoke-Native git @('-C', $SourceDirectory, 'init', '--quiet')
    Invoke-Native git @('-C', $SourceDirectory, 'remote', 'add', 'origin', $pin.repository)
    # Upstream test snapshots have paths longer than Windows' 260 characters.
    Invoke-Native git @('-C', $SourceDirectory, 'config', 'core.longpaths', 'true')
}

$head = (& git -C $SourceDirectory rev-parse --verify --quiet HEAD) 2>$null
if ($head -ne $pin.commit) {
    Invoke-Native git @('-C', $SourceDirectory, 'fetch', '--quiet', '--depth', '1', 'origin', $pin.commit)
    Invoke-Native git @('-C', $SourceDirectory, 'checkout', '--quiet', '--force', 'FETCH_HEAD')
}

if ($RollForwardLatestMajor) {
    $globalJson = Join-Path $SourceDirectory 'global.json'
    $sdk = Get-Content -Raw -LiteralPath $globalJson | ConvertFrom-Json
    $sdk.sdk.rollForward = 'latestMajor'
    $sdk | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $globalJson -Encoding utf8
}

$project = Join-Path $SourceDirectory 'tools/EncDotNet.S100.Cli/EncDotNet.S100.Cli.csproj'
foreach ($rid in $pending) {
    $target = Join-Path $Output $rid
    if (Test-Path -LiteralPath $target) {
        Remove-Item -LiteralPath $target -Recurse -Force
    }

    $arguments = @(
        'publish', $project,
        '--configuration', 'Release',
        '--runtime', $rid,
        '--self-contained', 'true',
        '-p:PublishSingleFile=true',
        '-p:EnableCompressionInSingleFile=true',
        '-p:SatelliteResourceLanguages=en',
        '-p:DebugType=none',
        '--output', $target,
        "-p:Version=$($pin.version)",
        "-p:SourceRevisionId=$($pin.commit)",
        '-nologo'
    )
    if ($RestoreConfigFile) {
        $arguments += "-p:RestoreConfigFile=$((Resolve-Path -LiteralPath $RestoreConfigFile).Path)"
    }

    Write-Host "Publishing s100 $($pin.version) ($($pin.commit.Substring(0, 7))) for $rid..."
    Invoke-Native dotnet $arguments
    # Symbols and XML docs of the referenced libraries aren't needed to run.
    Get-ChildItem -LiteralPath $target -File -Recurse |
        Where-Object { $_.Extension -in '.pdb', '.xml' } |
        Remove-Item -Force
    Set-Content -LiteralPath (Join-Path $target $markerName) -Value $pin.commit -Encoding ascii
}

Write-Host "Built s100 into $Output."
