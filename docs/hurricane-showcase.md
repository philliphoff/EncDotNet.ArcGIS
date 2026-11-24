# Show a hurricane crossing a port

Put a hurricane, its forecast cone and wind radii, a wave and wind forecast,
and the charts of the port it crosses on one map, and step through the
landfall with Pro's time slider. Pro draws none of these layers by itself:
the storm comes from National Hurricane Center (NHC) shapefile zips, the waves
from a GRIB2 file, and the charts from S-57 cells, all drawn by SoundCharts.

The showcase uses Hurricane Milton (AL142024), which made landfall near Siesta
Key, south of Tampa Bay, at about 00:30 UTC on 10 October 2024.

> [!WARNING]
> Nothing in the showcase is for navigation. The storm and the waves are 2024
> forecasts, and the charts are today's NOAA ENC editions, not the editions in
> force in October 2024.

## What the showcase holds

A script downloads public NOAA data into one folder, with a subfolder for each
group, and writes a collection manifest for the **Library**:

| Group | Data | Source | Layer in Pro |
|---|---|---|---|
| **Storm** | NHC advisory 18 (11 AM EDT, 9 October 2024): forecast track and cone, wind radii and best track | [NHC GIS archive](https://www.nhc.noaa.gov/gis/) | **Weather** |
| **Charts** | 48 NOAA ENC cells (S-57), from the Gulf of America overview to the band 5 harbour cells of Tampa Bay | [NOAA ENCs](https://charts.noaa.gov/ENCs/ENCs.shtml) | **Chart** |
| **Weather** | GFS-Wave, the 12 UTC run of 9 October 2024, hours 0 to 36: wave height, period and direction, and wind | [NOAA GFS on AWS](https://registry.opendata.aws/noaa-gfs-bdp-pds/) | **Weather** |

The download is about 50 MB. NHC, GFS and NOAA's S-100 data are US government
works in the public domain. NOAA's
[ENC user agreement](https://charts.noaa.gov/ENCs/ENCs.shtml) lets you use and
redistribute the ENCs without restriction.

### No water level or currents for Milton

The showcase has no S-104 water level or S-111 surface currents. NOAA
publishes both on AWS Open Data, but keeps only recent runs:

- `noaa-s111-pds` holds about the last 31 days of each forecast model's runs,
  including Tampa Bay's (TBOFS).
- `noaa-s104-pds` is a pilot: four Charleston tiles from one run in December
  2025.
- NOAA's raw model output, `noaa-nos-ofs-pds`, has no TBOFS runs for October
  2024.

No US hurricane landfall falls inside those archives, so no storm has a Water
group. For a storm inside the 31 days, run the script with that storm's
parameters, as in [Gather another storm](#gather-another-storm). It adds a
**Water** group whenever NOAA's buckets hold S-104 or S-111 data for the hours
you ask for.

## Prerequisites

- SoundCharts for ArcGIS, installed in ArcGIS Pro 3.7 or later. See
  [Install SoundCharts for ArcGIS](install.md).
- PowerShell 7 or later, to run the download script.
- A clone or download of this repository, for the script.

## Download the data

From the repository root, run
[`tools/get-hurricane-showcase.ps1`](https://github.com/philliphoff/EncDotNet.ArcGIS/blob/main/tools/get-hurricane-showcase.ps1):

```powershell
pwsh tools/get-hurricane-showcase.ps1 -Path path\to\showcase
```

Without `-Path`, the data goes in `artifacts\showcase\al142024` in the
repository. The script says what it downloads, warns that there's no S-104 or
S-111 data for these hours, and ends with:

```text
Wrote path\to\showcase\al142024.s100collection.json with 3 groups: Storm, Charts, Weather.
```

The folder looks like this:

```text
showcase/
  al142024.s100collection.json
  Storm/al142024_018/    al142024_5day_018.zip, al142024_fcst_018.zip, al142024_best_track.zip
  Charts/US5TPAEG/ENC_ROOT/...   one folder per ENC cell
  Weather/gfswave.t12z.atlocn.0p16.20241009.f000-f036.grib2
```

Files that are already there are skipped, so you can run the script again if a
download fails. The manifest lists the groups by folder:

```json
{
  "format": "encdotnet-s100-collection",
  "version": 1,
  "title": "Hurricane Milton at Tampa Bay",
  "description": "AL142024 advisory 018, 2024-10-09 12:00 to 2024-10-11 00:00 UTC. Gathered by tools/get-hurricane-showcase.ps1 from public NOAA data. Not for navigation.",
  "groups": [
    { "id": "storm", "name": "Storm", "description": "…", "paths": [ "Storm" ] },
    { "id": "charts", "name": "Charts", "description": "…", "paths": [ "Charts" ] },
    { "id": "weather", "name": "Weather", "description": "…", "paths": [ "Weather" ] }
  ]
}
```

The format is described in EncDotNet.S100's
[collection manifest reference](https://github.com/philliphoff/EncDotNet.S100/blob/main/docs/local-collection-manifest.md).

## Add the showcase to the Library

1. In Pro, open a map. On the **SoundCharts** tab, choose **Library**.
2. In the **SoundCharts Library** pane, choose **Add** > **Add a collection
   manifest...**, and open `al142024.s100collection.json`.

The collection appears as **Hurricane Milton at Tampa Bay**, with a node for
each group. The **Storm** group lists the advisory as one dataset,
**al142024_018**: its folder, whose three zips are read together. The
**Weather** group lists the GFS-Wave run as one dataset too: the script
appends the run's hours into one GRIB2 file.

## Add the groups to the map

1. Select the **Charts** group and choose **Add All to Map**. A **Chart**
   layer draws the ENCs.
2. Select the **Storm** group and choose **Add All to Map**, then do the same
   for the **Weather** group. Both go in a **Weather** layer, which follows
   map time.

The **Coverage** layer makes the map time-aware, so Pro shows the time slider.
The first time it does, SoundCharts sets map time to the end of the data
nearer to now: 12:00 UTC on 14 October 2024, the advisory's last forecast
point, after the landfall.

## Step through the landfall

1. Zoom to Tampa Bay and its approaches, at about 1:2,000,000, so the storm,
   its wind radii and Tampa Bay are all in view.
2. On the time slider, set map time to 18:00 UTC on 9 October 2024.
3. On the **SoundCharts** tab, choose **Next Data** to step map time an hour
   at a time, to 06:00 UTC on 10 October.

At each step, look at:

- **The storm.** The advisory's forecast points are at 12:00 UTC on 9 October
  and 00:00 UTC on 10 October, the hour of landfall. Between them, the
  storm, drawn in a ring with its class, wind and pressure, and its 34, 50
  and 64 knot wind radii, moves along the forecast track each hour, at a
  position estimated between the points: its ring is dashed and its label
  says "est.". The cone and the forecast track don't move.
- **The wind and waves.** The wind barbs and the wave height fill change every
  hour, as the winds turn around the storm.
- **The charts.** The Chart layer doesn't follow map time. Zoom in for the
  harbour cells: the Southwest Channel, Egmont Key and Port Manatee.

**Go to Nearest Data**'s tooltip names the time the **Weather** layer draws,
such as "Weather · 10 Oct 2024 00:00 UTC".

The storm draws at map time itself, rounded down to a quarter of an hour, so
you can also drag the time slider between the steps. Before the advisory,
the storm is drawn on its past track, from the best track. Before the track
starts or after the last forecast point, the cone and track still draw,
without the storm, and the tooltip says "Before track" or "Beyond forecast".

## Gather another storm

The script takes the storm, the hours and the area as parameters, so it can
gather another storm crossing another port. For example, for a storm whose
S-111 runs are still in NOAA's archive:

```powershell
pwsh tools/get-hurricane-showcase.ps1 -Storm <atcf id> -Advisory <number> -Start <first hour, UTC> -End <last hour, UTC> -Model <ofs model> -BoundingBox <west>,<south>,<east>,<north> -Cell <cell>,<cell> -Title "<title>"
```

| Parameter | What it sets |
|---|---|
| `-Storm`, `-Advisory` | The NHC storm, such as `al142024`, and advisory, such as `018` or `019A`. |
| `-Start`, `-End` | The hours, in UTC, that the GFS-Wave and water data cover. |
| `-Model` | The NOAA operational forecast model whose S-111 runs to fetch, such as `tbofs`, `cbofs` or `ngofs2`. |
| `-BoundingBox` | The area whose S-104 tiles to fetch. |
| `-Cell` | The NOAA ENC cells to fetch. |
| `-WaveGrid` | The GFS-Wave grid, such as `atlocn.0p16` for the Atlantic or `epacif.0p16` for the eastern Pacific. |
| `-Skip` | Groups not to download: `Storm`, `Charts`, `Weather`, `Water`. |

With S-111 data, the collection has a **Water** group, which goes in a
**Surface currents** layer, and S-104 a **Water level** layer. Both follow
map time with the **Weather** layer.
