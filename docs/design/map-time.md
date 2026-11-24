# Map time in SoundCharts for ArcGIS

How Pro's map time drives the time layers, **Water level** (S-104),
**Surface currents** (S-111) and **Weather** (every other dataset with more
than one time step), and what slice A8 found about Pro 3.7 on the way. The decisions are the design brief's §6 and D29–D33; this page
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
   per layer by the layer's selection (`TimeSteps.Resolve`). The step comes
   from EncDotNet.S100's `TimeStepSelector`, the rule the server draws each
   dataset by (issue #72), with the layer's interval as the tolerance:
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
   or "Not in force at map time" (D33): not before its `publicationTime`,
   not from its `cancellationDate`, and only within a part's
   `fixedDateRange`s when every dated part has them.

The **Coverage** layer's first and last times come from the HDF5 metadata
for S-104 and S-111, and from the processor's time steps for **Weather**
datasets. A storm's first time starts its validity earlier, at the start
of its past track, as the server's `timeRange` does; the time slider's
extent includes it too. **Chart** datasets
get no times, so the time-enabled **Coverage** layer never hides a
footprint whose chart draws at every time.

The logic that doesn't need Pro is in `src/EncDotNet.ArcGIS.SoundCharts/MapTime/`
(`TimeSteps`, `TimeSelection`, `DatasetTimes`, `TimeStatus`, `LayerTime`, `TimeText`) and tested in
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
- **Chart** gets no time, so the server draws every S-124 warning in it
  (U7); Object information is where validity shows. The pinned server can
  draw warnings by validity at a time asked for (EncDotNet.S100 #1525),
  which the add-in doesn't use yet.
- The validity rule combines the library's typed S-124 values; it belongs
  in EncDotNet.S100 once the library says "in force" itself.
- A **Weather** layer with a storm and a GRIB2 run beside it is continuous,
  so its tile URLs change every 15 minutes even while the grid's step
  doesn't, and it's never hidden: past the grid's last step, the server
  holds that step (U6) beside the storm's cone and track. A layer with nearest and at-or-before datasets resolves at or
  before, so its nearest datasets can lag by up to an interval.
- The add-in's interval, which is the tolerance it selects with, is the
  most common interval between steps; the server's is the dataset's
  validity, or else the median interval. They're the same for regular
  steps, and for EncDotNet.S100's TC, S-104 and S-111 test datasets
  (`TimeStepSelectorAgreementTests`), but a dataset with irregular steps or
  a validity of its own can draw a step the add-in calls a gap, or the
  other way round. A nearest layer's range is half an interval beyond its
  first and last steps, where the server's is a whole one, so the add-in
  hides it a little earlier than the server stops drawing it.
- Times in the tab and the log follow A5's **Time format** setting (D28).
