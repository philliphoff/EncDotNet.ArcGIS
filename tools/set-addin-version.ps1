<#
.SYNOPSIS
Stamps a release version into the add-in's Config.daml and the features
plugin's Config.xml.

.DESCRIPTION
ArcGIS Pro shows the version and date from an add-in's Config.daml
(AddInInfo version and Date) and a plugin's Config.xml (Version and Date) in
its Add-In Manager. The source tree keeps 0.0.0 there; CI runs this script on
a release tag build, before packaging, so the released packages carry the
tag's version.

Pro's add-in versions are numeric (major.minor.patch), so a prerelease or
build suffix (-rc.1, +abc) is dropped. The files are edited in place with
their formatting kept.

.PARAMETER Version
The version to stamp, such as 1.2.3 or 1.2.3-rc.1.

.PARAMETER Date
The date to stamp. Defaults to today (UTC).

.EXAMPLE
tools/set-addin-version.ps1 -Version 0.1.0
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [string] $Version,
    [datetime] $Date = [datetime]::UtcNow
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($Version -notmatch '^(\d+\.\d+\.\d+)([-+].*)?$') {
    throw "'$Version' isn't a major.minor.patch version."
}
$numeric = $Matches[1]
$day = $Date.ToString('yyyy-MM-dd')

$repoRoot = Split-Path -Parent $PSScriptRoot

# Each edit: the file, and the patterns whose first group is kept and whose
# value is replaced. Every pattern must match exactly once.
$edits = @(
    @{
        Path = Join-Path $repoRoot 'src/EncDotNet.ArcGIS.SoundCharts/Config.daml'
        Replacements = @(
            @{ Pattern = '(<AddInInfo\b[^>]*\bversion=")[^"]*'; Value = $numeric },
            @{ Pattern = '(<Date>)[^<]*'; Value = $day }
        )
    },
    @{
        Path = Join-Path $repoRoot 'src/EncDotNet.ArcGIS.SoundCharts.Features/Config.xml'
        Replacements = @(
            @{ Pattern = '(<Version>)[^<]*'; Value = $numeric },
            @{ Pattern = '(<Date>)[^<]*'; Value = $day }
        )
    }
)

foreach ($edit in $edits) {
    $text = [System.IO.File]::ReadAllText($edit.Path)
    foreach ($replacement in $edit.Replacements) {
        $count = ([regex]::Matches($text, $replacement.Pattern)).Count
        if ($count -ne 1) {
            throw "Expected one match for $($replacement.Pattern) in $($edit.Path), found $count."
        }
        $value = $replacement.Value
        $text = [regex]::Replace($text, $replacement.Pattern, { param($m) $m.Groups[1].Value + $value })
    }
    if ($PSCmdlet.ShouldProcess($edit.Path, "Set version $numeric, date $day")) {
        # Keep the file's encoding: both are UTF-8 without a BOM.
        [System.IO.File]::WriteAllText($edit.Path, $text, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Set $(Split-Path -Leaf $edit.Path) to version $numeric, date $day"
    }
}
