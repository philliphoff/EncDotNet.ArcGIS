<#
.SYNOPSIS
Downloads the hurricane-at-a-port showcase data and writes a collection
manifest for the SoundCharts Library.

.DESCRIPTION
Gathers public NOAA data for one hurricane crossing one port into a folder
laid out for the SoundCharts Library, one subfolder per group:

  Storm/    the NHC advisory: forecast track and cone, wind radii and best
            track zips, in one folder (www.nhc.noaa.gov)
  Charts/   NOAA ENC cells, S-57, one folder per cell (charts.noaa.gov)
  Weather/  GFS-Wave hours from one run, appended into one GRIB2 file
            (AWS Open Data, noaa-gfs-bdp-pds)
  Water/    S-104 water level and S-111 surface currents for the same hours
            (AWS Open Data, noaa-s104-pds and noaa-s111-pds), when NOAA's
            archive still holds them

It then writes <Storm>.s100collection.json at the root, with a group for each
subfolder that holds data. Add that file to the Library with Add > Collection
manifest.

The defaults are Hurricane Milton (AL142024) crossing Tampa Bay on 9-10
October 2024. NOAA's S-104 and S-111 buckets keep about 31 days of runs, so
they hold nothing for Milton and the Water group is left out; the script says
so. For a storm inside that window, pass -Storm, -Advisory, -Start, -End,
-BoundingBox, -Cell and -Model.

All of the data is public domain or, for NOAA ENCs, free to use and
redistribute under NOAA's ENC user agreement. None of it is for navigation:
the charts are a snapshot of today's ENC editions, and the storm, waves and
water data are forecasts from 2024.

Files that are already there are skipped, so the script can be run again
after a failed download.

.PARAMETER Path
The folder to download into. Defaults to artifacts/showcase/<Storm>.

.PARAMETER Title
The collection's title in the Library. Defaults to "Hurricane Milton at Tampa
Bay".

.PARAMETER Storm
The storm's ATCF id, such as al142024.

.PARAMETER Advisory
The NHC advisory number, such as 018 or 019A.

.PARAMETER Start
The start of the showcase window, in UTC. GFS-Wave and the water data cover
the window.

.PARAMETER End
The end of the showcase window, in UTC.

.PARAMETER BoundingBox
West, south, east and north in degrees: the S-104 tiles to fetch.

.PARAMETER Cell
The NOAA ENC cells to fetch. Defaults to the cells over Tampa Bay and its
approaches, from the Gulf of America overview to the band 5 harbour cells.

.PARAMETER Model
The NOAA operational forecast system whose S-111 runs to fetch, such as
tbofs. Empty to skip S-111.

.PARAMETER WaveGrid
The GFS-Wave grid, such as atlocn.0p16 (Atlantic, 1/6 degree).

.PARAMETER Skip
Groups not to download: Storm, Charts, Weather, Water.

.EXAMPLE
tools/get-hurricane-showcase.ps1

.EXAMPLE
tools/get-hurricane-showcase.ps1 -Path D:\Showcase\Milton -Skip Weather
#>
#Requires -Version 7.0
[CmdletBinding()]
param(
    [string] $Path,
    [string] $Title = 'Hurricane Milton at Tampa Bay',
    [ValidatePattern('^(al|ep|cp)\d{6}$')]
    [string] $Storm = 'al142024',
    [ValidatePattern('^\d{3}[A-Z]?$')]
    [string] $Advisory = '018',
    [datetime] $Start = '2024-10-09T12:00:00Z',
    [datetime] $End = '2024-10-11T00:00:00Z',
    [ValidateCount(4, 4)]
    [double[]] $BoundingBox = @(-83.2, 26.9, -82.2, 28.2),
    [string[]] $Cell = @(
        # Overview and general: the storm's whole run across the Gulf.
        'US1GLBCF', 'US2GOACD',
        # Coastal: the approaches from Sarasota to Clearwater.
        'US3FL1DD', 'US3FL1DE', 'US3FL1ED', 'US3FL1EE',
        # Approach: the bay, Sarasota Bay and the coast.
        'US4FL1NP', 'US4FL1NQ', 'US4FL1NR',
        'US4FL1OP', 'US4FL1OQ', 'US4FL1OR',
        'US4FL1PP', 'US4FL1PQ', 'US4FL1PR',
        'US4FL1QP', 'US4FL1QQ', 'US4FL1QR',
        # Harbour: the entrance channels, Port Manatee, St. Petersburg and Tampa.
        'US5TPACB', 'US5TPACC', 'US5TPACD', 'US5TPACE', 'US5TPACF', 'US5TPACG',
        'US5TPADC', 'US5TPADD', 'US5TPADE', 'US5TPADF', 'US5TPADG', 'US5TPADH',
        'US5TPAEE', 'US5TPAEF', 'US5TPAEG', 'US5TPAEH', 'US5TPAEI',
        'US5TPAFD', 'US5TPAFE', 'US5TPAFG', 'US5TPAFH', 'US5TPAFI', 'US5TPAFJ',
        'US5TPAGD', 'US5TPAGH', 'US5TPAGI', 'US5TPAGJ',
        'US5TPAHD', 'US5TPAHI', 'US5TPAHJ'
    ),
    [string] $Model = 'tbofs',
    [string] $WaveGrid = 'atlocn.0p16',
    [ValidateSet('Storm', 'Charts', 'Weather', 'Water')]
    [string[]] $Skip = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $Path) { $Path = Join-Path $repoRoot "artifacts/showcase/$Storm" }
# A time without a zone is UTC.
function ConvertTo-Utc([datetime] $Time) {
    if ($Time.Kind -eq 'Unspecified') { return [datetime]::SpecifyKind($Time, 'Utc') }
    return $Time.ToUniversalTime()
}
$Start = ConvertTo-Utc $Start
$End = ConvertTo-Utc $End
if ($End -le $Start) { throw "-End ($($End.ToString('u'))) must be after -Start ($($Start.ToString('u')))." }

$nhc = 'https://www.nhc.noaa.gov/gis'
$encs = 'https://charts.noaa.gov/ENCs'
$gfs = 'https://noaa-gfs-bdp-pds.s3.amazonaws.com'
$s104 = 'https://noaa-s104-pds.s3.amazonaws.com'
$s111 = 'https://noaa-s111-pds.s3.amazonaws.com'

function Get-File {
    param([string] $Uri, [string] $OutFile)
    if (Test-Path -LiteralPath $OutFile) { return }
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutFile) | Out-Null
    $partial = "$OutFile.partial"
    for ($attempt = 1; ; $attempt++) {
        try {
            Invoke-WebRequest -Uri $Uri -OutFile $partial -MaximumRetryCount 2 -RetryIntervalSec 2
            Move-Item -LiteralPath $partial -Destination $OutFile -Force
            return
        }
        catch {
            Remove-Item -LiteralPath $partial -Force -ErrorAction SilentlyContinue
            if ($attempt -ge 4) { throw "Couldn't download ${Uri}: $($_.Exception.Message)" }
            Start-Sleep -Seconds ([math]::Pow(2, $attempt))
        }
    }
}

# Lists an S3 bucket's keys under a prefix, following continuation tokens.
function Get-S3Key {
    param([string] $Bucket, [string] $Prefix)
    $token = $null
    do {
        $uri = "$Bucket/?list-type=2&prefix=$([uri]::EscapeDataString($Prefix))"
        if ($token) { $uri += "&continuation-token=$([uri]::EscapeDataString($token))" }
        $xml = [xml](Invoke-WebRequest -Uri $uri -MaximumRetryCount 3 -RetryIntervalSec 2).Content
        $result = $xml.ListBucketResult
        foreach ($item in @($result.ChildNodes | Where-Object LocalName -eq 'Contents')) {
            [pscustomobject]@{ Key = $item.Key; Size = [long]$item.Size }
        }
        $token = if ($result.IsTruncated -eq 'true') { $result.NextContinuationToken } else { $null }
    } while ($token)
}

# The run time in a NOAA S-104 or S-111 file name: ..._20261122T00Z.h5.
function Get-RunTime {
    param([string] $Key)
    if ($Key -match '_(\d{8}T\d{2})Z(_[A-Z0-9]+)?\.h5$') {
        return [datetime]::ParseExact($Matches[1], 'yyyyMMdd\THH', [cultureinfo]::InvariantCulture,
            [System.Globalization.DateTimeStyles]::AssumeUniversal -bor [System.Globalization.DateTimeStyles]::AdjustToUniversal)
    }
    return $null
}

$groups = [System.Collections.Generic.List[object]]::new()

# Storm: one advisory's three zips in one folder, which SoundCharts reads as
# one dataset, and the Library lists as one item at the folder.
if ('Storm' -notin $Skip) {
    $folder = Join-Path $Path "Storm/${Storm}_$Advisory"
    Write-Host "Storm: $($Storm.ToUpperInvariant()) advisory $Advisory..."
    Get-File "$nhc/forecast/archive/${Storm}_5day_$Advisory.zip" (Join-Path $folder "${Storm}_5day_$Advisory.zip")
    Get-File "$nhc/forecast/archive/${Storm}_fcst_$Advisory.zip" (Join-Path $folder "${Storm}_fcst_$Advisory.zip")
    Get-File "$nhc/best_track/${Storm}_best_track.zip" (Join-Path $folder "${Storm}_best_track.zip")
    $groups.Add([ordered]@{
            id = 'storm'; name = 'Storm'
            description = "NHC advisory $Advisory for $($Storm.ToUpperInvariant()): forecast track, cone, wind radii and best track. Not for navigation."
            paths = @('Storm')
        })
}

# Charts: each NOAA ENC zip is an exchange set with its own CATALOG.031, so
# each goes in its own folder.
if ('Charts' -notin $Skip) {
    Write-Host "Charts: $($Cell.Count) NOAA ENC cells..."
    foreach ($cellName in $Cell) {
        $folder = Join-Path $Path "Charts/$cellName"
        if (Test-Path -LiteralPath (Join-Path $folder "ENC_ROOT/$cellName/$cellName.000")) { continue }
        $zip = Join-Path $Path "Charts/$cellName.zip"
        Get-File "$encs/$cellName.zip" $zip
        Expand-Archive -LiteralPath $zip -DestinationPath $folder -Force
        Remove-Item -LiteralPath $zip
    }
    $groups.Add([ordered]@{
            id = 'charts'; name = 'Charts'
            description = 'NOAA ENCs (S-57), today''s editions.'
            paths = @('Charts')
        })
}

# Weather: GFS-Wave from the run at or before the start, one file per forecast
# hour, appended into one GRIB2 file.
if ('Weather' -notin $Skip) {
    $cycle = $Start.Date.AddHours([math]::Floor($Start.Hour / 6) * 6)
    $hours = [int][math]::Ceiling(($End - $cycle).TotalHours)
    if ($hours -gt 120) { throw "GFS-Wave is hourly to 120 hours; the window needs $hours hours from the $($cycle.ToString('u')) run." }
    $run = "gfs.$($cycle.ToString('yyyyMMdd'))/$($cycle.ToString('HH'))/wave/gridded"
    $stem = "gfswave.t$($cycle.ToString('HH'))z.$WaveGrid"
    $target = Join-Path $Path "Weather/$stem.$($cycle.ToString('yyyyMMdd')).f000-f$($hours.ToString('000')).grib2"
    if (-not (Test-Path -LiteralPath $target)) {
        Write-Host "Weather: GFS-Wave $WaveGrid, $($cycle.ToString('u')) run, hours 0-$hours..."
        $parts = Join-Path $Path 'Weather/parts'
        $files = foreach ($hour in 0..$hours) {
            $file = Join-Path $parts "$stem.f$($hour.ToString('000')).grib2"
            Get-File "$gfs/$run/$stem.f$($hour.ToString('000')).grib2" $file
            $file
        }
        # GRIB2 messages stand alone, so files append byte for byte.
        $output = [System.IO.File]::Create("$target.partial")
        try {
            foreach ($file in $files) {
                $source = [System.IO.File]::OpenRead($file)
                try { $source.CopyTo($output) } finally { $source.Dispose() }
            }
        }
        finally { $output.Dispose() }
        Move-Item -LiteralPath "$target.partial" -Destination $target -Force
        Remove-Item -LiteralPath $parts -Recurse -Force
    }
    $groups.Add([ordered]@{
            id = 'weather'; name = 'Weather'
            description = "GFS-Wave wave height, period and direction, and wind, from the $($cycle.ToString('yyyy-MM-dd HH'))Z run. Not for navigation."
            paths = @('Weather')
        })
}

# Water: S-104 tiles in the bounding box, and the S-111 runs of the model,
# that cover the window. NOAA keeps about 31 days of S-111 runs, and S-104 is
# a pilot, so for an older storm there's nothing to fetch.
if ('Water' -notin $Skip) {
    $found = 0
    Write-Host 'Water: looking for S-104 water level...'
    $catalog = [xml](Invoke-WebRequest -Uri "$s104/ed2.0.0/_CATALOG/CATALOG.XML" -MaximumRetryCount 3).Content
    $ns = [System.Xml.XmlNamespaceManager]::new($catalog.NameTable)
    $ns.AddNamespace('S100XC', 'http://www.iho.int/s100/xc/5.2')
    $ns.AddNamespace('gex', 'http://standards.iso.org/iso/19115/-3/gex/1.0')
    $ns.AddNamespace('gco', 'http://standards.iso.org/iso/19115/-3/gco/1.0')
    foreach ($entry in $catalog.SelectNodes('//S100XC:S100_DatasetDiscoveryMetadata', $ns)) {
        $file = $entry.SelectSingleNode('S100XC:fileName', $ns).InnerText -replace '^file:\.\./', ''
        $box = foreach ($side in 'westBoundLongitude', 'southBoundLatitude', 'eastBoundLongitude', 'northBoundLatitude') {
            [double]$entry.SelectSingleNode(".//gex:$side/gco:Decimal", $ns).InnerText
        }
        $first = $entry.SelectSingleNode('.//S100XC:timeInstantBegin', $ns)
        $last = $entry.SelectSingleNode('.//S100XC:timeInstantEnd', $ns)
        $inBox = $box[0] -lt $BoundingBox[2] -and $box[2] -gt $BoundingBox[0] -and $box[1] -lt $BoundingBox[3] -and $box[3] -gt $BoundingBox[1]
        $inWindow = $first -and $last -and
            (ConvertTo-Utc ([datetime]$first.InnerText)) -le $End -and (ConvertTo-Utc ([datetime]$last.InnerText)) -ge $Start
        if ($inBox -and $inWindow) {
            Get-File "$s104/ed2.0.0/$file" (Join-Path $Path "Water/S-104/$(Split-Path -Leaf $file)")
            $found++
        }
    }

    if ($Model) {
        Write-Host "Water: looking for S-111 surface currents from $($Model.ToUpperInvariant())..."
        # Runs issued from two days before the window to its end.
        $runs = for ($day = $Start.Date.AddDays(-2); $day -le $End.Date; $day = $day.AddDays(1)) {
            Get-S3Key $s111 "ed1.0.1/model_forecast_guidance/$Model/$($day.ToString('yyyy/MM/dd'))/" |
                Where-Object { $_.Key -match '/dcf2/regional/[^/]+\.h5$' } |
                ForEach-Object { [pscustomobject]@{ Key = $_.Key; Time = (Get-RunTime $_.Key) } }
        }
        $runs = @($runs | Where-Object Time | Sort-Object Time)
        # The latest run at or before the start, then a run every 48 hours, so
        # the forecasts follow each other without overlapping.
        $pick = @($runs | Where-Object { $_.Time -le $Start } | Select-Object -Last 1)
        if (-not $pick) { $pick = @($runs | Where-Object { $_.Time -le $End } | Select-Object -First 1) }
        while ($pick -and $pick[-1].Time.AddHours(48) -lt $End) {
            $next = $runs | Where-Object { $_.Time -gt $pick[-1].Time -and $_.Time -le $pick[-1].Time.AddHours(48) } | Select-Object -Last 1
            if (-not $next) { break }
            $pick += $next
        }
        foreach ($run in $pick) {
            Get-File "$s111/$($run.Key)" (Join-Path $Path "Water/S-111/$(Split-Path -Leaf $run.Key)")
            $found++
        }
    }

    if ($found -gt 0) {
        $groups.Add([ordered]@{
                id = 'water'; name = 'Water'
                description = 'NOAA S-104 water level and S-111 surface currents for the same hours. Forecast guidance, not for navigation.'
                paths = @('Water')
            })
    }
    else {
        Write-Warning ("NOAA's S-104 and S-111 archives hold nothing for $($Start.ToString('u')) to $($End.ToString('u')). " +
            'S-111 runs are kept for about 31 days and S-104 is a pilot, so the collection has no Water group.')
    }
}

if ($groups.Count -eq 0) { throw 'Nothing to write: every group was skipped.' }

$manifest = [ordered]@{
    format = 'encdotnet-s100-collection'
    version = 1
    title = $Title
    description = ("$($Storm.ToUpperInvariant()) advisory $Advisory, $($Start.ToString('yyyy-MM-dd HH:mm')) to $($End.ToString('yyyy-MM-dd HH:mm')) UTC. " +
        'Gathered by tools/get-hurricane-showcase.ps1 from public NOAA data. Not for navigation.')
    groups = $groups
}
$manifestPath = Join-Path $Path "$Storm.s100collection.json"
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8NoBOM

Write-Host "Wrote $manifestPath with $($groups.Count) groups: $(($groups | ForEach-Object name) -join ', ')."
