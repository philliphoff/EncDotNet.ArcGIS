# Map time in SoundCharts for ArcGIS

How Pro's map time drives the **Water level** (S-104) and **Surface
currents** (S-111) layers, and what slice A8 found about Pro 3.7 on the
way. The decisions are the design brief's §6 and D29–D33; this page
records how they're carried out and the answers to open questions P1, P2,
P4 and P9.

## What happens

1. When a chart layer's server starts, the add-in reads the server's time
   steps from TileJSON (`s100.times`). These are also the WMTS `Time`
   dimension's `<Value>`s, so a step the add-in picks is one the server
   accepts. A layer with no steps (**Chart**) isn't followed.
2. On every map time change in the active map view
   (`MapViewTimeChangedEvent`), and when the active view changes, the add-in
   takes the end of the map time range as the instant (D30) and snaps it per
   layer (`TimeSteps.Resolve`):
   - in range, within half an interval of the first or last step, it picks
     the nearest step;
   - between steps, a nearest step more than one interval away is a gap;
   - outside the range, or in a gap, there's no data (D31).
3. With data, it writes the step into the layer's WMTS connection
   (`Dimensions["Time"]`), so Pro requests
   `.../wmts/1.0.0/s100/{style}/{time}/WebMercatorQuad/{z}/{row}/{col}.png`.
   Each step has its own URLs, so Pro caches each one.
4. With no data, it leaves the time alone and sets the layer's visibility
   time extent to one no map time reaches, so the layer draws nothing while
   its Contents check box stays as the user left it. With data, the extent
   is the data's range, which Pro applies by itself if map time moves out
   of range before the add-in hears of it.
5. The **SoundCharts Layer** tab's **Time** group says what each layer
   draws, in **Go to Nearest Data**'s tooltip: "Water level · 17 Dec 2025
   13:00 UTC", or "Water level · No data at map time · last 24 Dec 2025
   12:00 UTC, 3 h earlier". **Go to Nearest Data** moves map time to that
   data; **Now** sets map time to now, once.
6. The first time the add-in sees map time on a map with time layers, and
   it's either all time or the data's full extent (what Pro picks by itself
   when a time-aware layer arrives), it sets map time to now, snapped, or
   the nearer end of the data (D32). Any other map time is the user's and
   is never moved.
7. **Object information** samples water levels and currents at the
   instant, and says whether each S-124 warning is "In force at map time"
   or "Not in force at map time" (D33): not before its `publicationTime`,
   not from its `cancellationDate`, and only within a part's
   `fixedDateRange`s when every dated part has them.

The logic that doesn't need Pro is in `src/EncDotNet.ArcGIS.SoundCharts/MapTime/`
(`TimeSteps`, `TimeStatus`, `LayerTime`, `TimeText`) and tested in
`tests/EncDotNet.ArcGIS.SoundCharts.Tests/MapTime/`. `MapTimeController`
is the Pro side.

## What Pro 3.7 does

Found with arcpy (Pro 3.7, `python -I`) against an `s100 tiles serve` of the
Charleston S-104 sample, through a proxy that logged every request.

| Question | Answer |
|---|---|
| **P1** Can a CIM WMTS layer be time-aware so Pro sends `Time` from map time? | No. `CIMTiledServiceLayer` has no time definition, `enableTime()` fails on it, and `supports("TIME")` is true but means nothing. Pro does fill the template's `{Time}` from the connection's `dimensions` property set: with `"dimensions": {"type": "PropertySet", "propertySetItems": ["Time", "2025-12-17T20:00:00Z"]}` every tile request carried `2025-12-17T20:00:00Z`; without it, Pro used the capabilities' `<Default>`, the first step. In .NET the property is `IDictionary<string, object>`. So the add-in drives time (D29). |
| **P2** Can the slider take its extent and step from a feature layer? | A time-aware layer is needed: `MapView.Time` is null without one, and setting the map's `CIMMapTimeDisplay` alone doesn't make the map time-aware. A file geodatabase polygon layer with first and last time fields time-enabled with `enableTime` does. The add-in time-enables the **Coverage** layer (A7) with `MapTimeController.EnableTimeAsync`: its fields, the data's range, and the most common step as the interval. Until Coverage exists, any time-aware layer in the map gives the slider. |
| **P4** Can one layer draw different times in two views? | Not with one layer: the time is in the layer's connection. The layer follows the active view. A layout map frame draws whatever time the layer holds. |
| **P9** Can a layer stop drawing without touching its check box? | `useVisibilityTimeExtent` and `visibilityTimeExtent` (epoch milliseconds in JSON, `TimeExtent` in .NET) load and round-trip; a layer with them still draws when the map has no map time. arcpy can't drive a map frame's time, so whether Pro hides the layer outside the extent was left to a check by hand in Pro. Pro has no API for a Contents tooltip, so the status is on the tab instead. |

## Limits

- The server still snaps to its last step when asked for a time outside its
  data (U6); the add-in never asks for one.
- The server draws every S-124 warning whatever the time (U7), so **Chart**
  isn't filtered by validity; Object information is where it shows.
- The validity rule combines the library's typed S-124 values; it belongs
  in EncDotNet.S100 once the library says "in force" itself.
- **‹ Previous data** and **Next data ›** (§6.6) and the **Local time**
  format (D28, A5's setting) aren't in this slice.
