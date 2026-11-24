# Map time in SoundCharts for ArcGIS

How Pro's map time drives the time layers, **Water level** (S-104),
**Surface currents** (S-111) and **Weather** (every other dataset with more
than one time step), and the **Navigational warnings** layer (S-124), and
what slice A8 found about Pro 3.7 on the way. The decisions are the design brief's §6 and D29–D33; this page
records how they're carried out and the answers to open questions P1, P2,
P4 and P9.

## What happens

1. When **Add SoundCharts Data** inspects a dataset, it records whether the
   dataset has more than one time step
   (`ITimeAwareDatasetProcessor.AvailableTimes`); the layer marker keeps it
   as `timeVarying`. That, not the product name, puts a tropical cyclone
   advisory, CAP warning, GRIB2 or NetCDF grid, S-411 or S-412 dataset in
   **Weather** (design brief §3.1). Markers saved before **Weather** don't
   say, and load as they were.
2. When a chart layer's server starts, the add-in reads the server's time
   steps from TileJSON (`s100.times`), and from `/datasets` how each
   time-varying dataset picks what it draws (`timeSelection`) and the span it
   draws in (`timeRange`), issue #64. The steps are also the WMTS `Time`
   dimension's `<Value>`s; the server takes any time, but stepping between
   its steps keeps tile URLs few. **Chart** isn't followed, even when it
   holds a dataset with a single step; every time layer with steps is.
3. On every map time change in the active map view
   (`MapViewTimeChangedEvent`), and when the active view changes, the add-in
   takes the end of the map time range as the instant (D30) and resolves it
   per layer by the layer's selection (`TimeSteps.Resolve`), as the server
   does for each dataset:
   - **nearest** (S-111, GRIB2, NetCDF): in range, within half an interval
     of the first or last step, the nearest step; between steps, a nearest
     step more than one interval away is a gap;
   - **at or before** (S-104, S-411, S-412, CAP, surface analyses): the
     latest step at or before the instant, never a later one, up to one
     interval after it; before the first step, or more than an interval
     after a step, is a gap;
   - **continuous** (tropical cyclones): the instant itself, rounded down to
     15 minutes, anywhere in the server's `timeRange`, from the start of the
     past track to the last forecast point;
   - outside the range, or in a gap, there's no data (D31).

   A layer whose datasets select differently is continuous when any of them
   is, so a storm still moves; otherwise at or before, so a CAP warning is
   never drawn before it's in force.
4. With data, it writes the step, or for a continuous layer the rounded
   time, into the layer's WMTS connection (`Dimensions["Time"]`), so Pro
   requests
   `.../wmts/1.0.0/s100/{style}/{time}/WebMercatorQuad/{z}/{row}/{col}.png`.
   Each time has its own URLs, so Pro caches each one. The server draws a
   storm to the minute; 15 minutes keeps a quarter of an hour's tiles in
   Pro's cache while the storm moves at most about 10 km, under 2 pixels at
   the 1:2,000,000 a storm is seen at, and forecast and best-track points,
   on the hour, are drawn exactly.
5. With no data, it leaves the time alone and sets the layer's visibility
   time extent to one no map time reaches, so the layer draws nothing while
   its Contents check box stays as the user left it. With data, the extent
   is the data's range, which Pro applies by itself if map time moves out
   of range before the add-in hears of it. A continuous layer has no
   visibility extent and still gets map time outside its range: the server
   draws a storm's cone and track there, without the storm, as the viewer
   does.
6. The **SoundCharts Layer** tab's **Time** group says what each layer
   draws, in **Go to Nearest Data**'s tooltip: "Water level · 17 Dec 2025
   13:00 UTC", or "Water level · No data at map time · last 24 Dec 2025
   12:00 UTC, 3 h earlier". A storm has the viewer's words: "Weather · est.
   28 Sep 2022 03:00 UTC, between forecast points", "Weather · 25 Sep 2022
   12:00 UTC · best track", and outside its range "Weather · Beyond forecast
   · last forecast point 2 Oct 2022 00:00 UTC, 3 days earlier" or "Before
   track · track starts …". **Go to Nearest Data** moves map time to that
   data; **Now** sets map time to now, once. **‹ Previous data** and **Next
   data ›** step between the layers' steps, a storm's forecast points
   included.
7. The first time the add-in sees map time on a map with time layers, and
   it's either all time or the data's full extent (what Pro picks by itself
   when a time-aware layer arrives), it sets map time to now, snapped, or
   the nearer end of the data (D32). Any other map time is the user's and
   is never moved.
8. **Object information** samples water levels and currents at the
   instant, and says whether each S-124 warning is "In force at map time"
   or "Not in force at map time" (D33), by the rule the map draws by
   (below): a picked part is drawn at the instant.

## Navigational warnings

S-124 warnings have no time steps, but which of them are in force depends on
map time (U7, issue #75). The chart server draws only the warnings in force
at a time it's asked for, and every warning without one (EncDotNet.S100
#1525). The add-in gives S-124 a layer of its own, **Navigational
warnings**, rather than giving **Chart** a time: a time on **Chart** would
refetch every chart tile whenever a warning starts or ends.

1. When the layer's server starts, the add-in reads the layer's S-124
   datasets in-process and builds the library's `NavigationalWarningSet`
   from them, as the server does (`NavigationalWarnings`). A warning is
   drawn when `IsDrawnAt` says so, without showing warnings not yet in
   force: not outside its validity ranges, not from its cancellation date,
   not once a warning that cancels it is published, and not once a later
   in-force bulletin for its series leaves it out.
2. From the set it works out when the warnings drawn change
   (`WarningTimes`): each range's start, the tick after each range's end,
   each cancellation date, and the publication of each cancellation and
   in-force bulletin, less the times after which the same warnings draw as
   before.
3. On every map time change it asks for the latest of those at or before
   the instant, or for `1900-01-01T00:00:00Z` before the first. Every map
   time between two changes has the same tile URLs, so Pro's cache
   refetches only when the warnings in force change. A range ends a tick
   after its end, so such a time is written with seven fractional digits,
   such as `2026-10-20T00:00:00.0000001Z`; the server reads it.
4. With no map time (the time slider off) it asks for no time, and every
   warning draws, as before. The layer is never hidden.

With S-124 alone the server's capabilities advertise no `Time` dimension,
so their tile template has no `{Time}` for Pro to fill from
`Dimensions` (P1). The add-in sets `Dimensions["Time"]` and also writes the
time into the connection's tile template, `TemplateUrl`:
`.../wmts/1.0.0/s100/{Style}/2026-10-12T00:00:00Z/{TileMatrixSet}/{TileMatrix}/{TileRow}/{TileCol}.png`.
Without map time it clears both, so Pro uses the capabilities' template.
Whether Pro requests tiles from `TemplateUrl` is open question P11 in the
design brief; if it doesn't, the layer draws every warning, as before. A
restart or a palette change carries the time over to the new connection.

The pinned server (`6731f4c7`) was checked with EncDotNet.S100's S-124
fixtures at v0.30.2. Asking for the add-in's time gave the same tile as
asking for map time itself at every one of 201 map times, six hours apart
from 25 Sep to 8 Nov 2026 and on both sides of every change. The map times
covered NAVAREA X 120/26's parts (ended 5 Oct, in force 10 to 20 Oct, from
25 Oct), NAVAREA X 121/26, cancelled on 12 Oct, and, among the bulletin
fixtures, NAVAREA X 105/26, which the 12 Oct in-force bulletin leaves out.
The REST time segment and WMTS KVP `Time=` both filter; a `?TIME=` query on
the REST URL doesn't.

The **Coverage** layer's first and last times come from the HDF5 metadata
for S-104 and S-111, and from the processor's time steps for **Weather**
datasets. A storm's first time starts its validity earlier, at the start
of its past track, as the server's `timeRange` does; the time slider's
extent includes it too. **Chart** datasets
get no times, so the time-enabled **Coverage** layer never hides a
footprint whose chart draws at every time.

The logic that doesn't need Pro is in `src/EncDotNet.ArcGIS.SoundCharts/MapTime/`
(`TimeSteps`, `TimeSelection`, `DatasetTimes`, `TimeStatus`, `LayerTime`, `TimeText`,
`NavigationalWarnings`, `WarningTimes`) and tested in
`tests/EncDotNet.ArcGIS.SoundCharts.Tests/MapTime/`. `MapTimeController`
is the Pro side.

## What Pro 3.7 does

Found with arcpy (Pro 3.7, `python -I`) against an `s100 tiles serve` of the
Charleston S-104 sample, through a proxy that logged every request.

| Question | Answer |
|---|---|
| **P1** Can a CIM WMTS layer be time-aware so Pro sends `Time` from map time? | No. `CIMTiledServiceLayer` has no time definition, `enableTime()` fails on it, and `supports("TIME")` is true but means nothing. Pro does fill the template's `{Time}` from the connection's `dimensions` property set: with `"dimensions": {"type": "PropertySet", "propertySetItems": ["Time", "2025-12-17T20:00:00Z"]}` every tile request carried `2025-12-17T20:00:00Z`; without it, Pro used the capabilities' `<Default>`, the first step. In .NET the property is `IDictionary<string, object>`. So the add-in drives time (D29). |
| **P2** Can the slider take its extent and step from a feature layer? | A time-aware layer is needed: `MapView.Time` is null without one, and setting the map's `CIMMapTimeDisplay` alone doesn't make the map time-aware. A file geodatabase polygon layer with first and last time fields time-enabled with `enableTime` does. The add-in time-enables the **Coverage** layer (A7) from its `FirstTime` and `LastTime` fields, with the data's range and the most common step as the interval, after every coverage sync and whenever a time layer's steps arrive (`MapTimeController.EnableCoverageTimeAsync`). |
| **P4** Can one layer draw different times in two views? | Not with one layer: the time is in the layer's connection. The layer follows the active view. A layout map frame draws whatever time the layer holds. |
| **P9** Can a layer stop drawing without touching its check box? | `useVisibilityTimeExtent` and `visibilityTimeExtent` (epoch milliseconds in JSON, `TimeExtent` in .NET) load and round-trip; a layer with them still draws when the map has no map time. arcpy can't drive a map frame's time, so whether Pro hides the layer outside the extent was left to a check by hand in Pro. Pro has no API for a Contents tooltip, so the status is on the tab instead. |

## Limits

- Without `--strict-time`, the server still holds a nearest or at-or-before
  dataset's last step when asked for a time outside its data (U6); the
  add-in hides those layers itself.
- **Navigational warnings** draws by map time only if Pro uses the
  connection's `TemplateUrl` (P11). Upstream advertising a `Time` dimension
  for S-124 alone, with the change times as its values, would let Pro fill
  `{Time}` as it does for the other time layers (U7).
- S-124 alone doesn't make the map time-aware: the **Coverage** layer is
  time-enabled from the time layers' steps, so a map with only charts and
  warnings has no time slider, and draws every warning.
- Projects saved before the **Navigational warnings** layer keep S-124 in
  **Chart**, drawing every warning, until the warnings are added again.
- A **Weather** layer with a storm and a GRIB2 run beside it is continuous,
  so its tile URLs change every 15 minutes even while the grid's step
  doesn't, and it's never hidden: past the grid's last step, the server
  holds that step (U6) beside the storm's cone and track. A layer with nearest and at-or-before datasets resolves at or
  before, so its nearest datasets can lag by up to an interval.
- Times in the tab and the log follow A5's **Time format** setting (D28).
