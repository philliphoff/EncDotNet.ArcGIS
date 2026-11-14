# Exporting SoundCharts layouts

How **Export SoundCharts Chart...** prints a layout with SoundCharts layers
at the layout's scale and resolution, and why it renders them the way it
does. This is slice A10
([#12](https://github.com/philliphoff/EncDotNet.ArcGIS/issues/12)); the
decisions it carries out are the design brief's D10 (WMTS for display,
export on its own path) and D34 (the "Not for navigation" text).

## The problem

Step 0 exported layouts with Pro's own **Export Layout**:

- A 300 dpi export sends WMS the same box as a 96 dpi one at 3.125 times
  the pixels (`WIDTH=2251` against `721`), with no `DPI`, `MAP_RESOLUTION`
  or `FORMAT_OPTIONS` hint. The server's `WmsMapComposer` stitches tiles at
  the zoom nearest that pixel size, so symbols print about half size,
  content gated by scale (SCAMIN soundings) changes, and past zoom 15 the
  image is enlarged and soft.
- A WMTS export reuses the screen's tiles: the 96 and 300 dpi exports drew
  the same 18 zoom 14 tiles, so 300 dpi is 96 dpi enlarged.
- WMS and WMTS pick different zooms for the same scale.

## What the export does

1. It finds the active layout's visible map frames and, in each, the
   visible SoundCharts chart layers in scale range that map time doesn't
   hide (A8's empty visibility time extent).
2. It offers the "Not for navigation" text (D34) when no text element
   says it: "SoundCharts · Not for navigation · Data: {producers}", at the
   page's lower left. It never adds it unasked.
3. For each layer, it renders the frame's Web Mercator extent with the
   layer's own datasets and server options (A5's, through
   `ChartLayerManager.ExportSource`), palette (the WMTS style) and time step
   (A8's WMTS `Time`, as the nearest step in the server's TileJSON).
4. It adds each rendered GeoTIFF as a raster layer just above its chart
   layer, with no stretch or gamma, the alpha band, and bilinear
   resampling, hides the chart layer, and exports the layout with Pro.
   PDF keeps each frame's coordinate system; PNG and JPEG get a world file
   and TIFF GeoTIFF tags for the largest frame with SoundCharts layers.
5. It removes the raster layers and shows the chart layers again, whether
   the export worked or not. A layer that couldn't be rendered stays as it
   was, so the export draws it from the server's tiles, and the message
   says how many.

## How a layer is rendered

`ChartTilePlan` picks the zoom the frame shows on screen: the
WebMercatorQuad level whose resolution is nearest the frame's at 96 dpi.
Above 96 dpi it asks for 512-pixel tiles of that zoom. `TilesExportRenderer`
runs the bundled `s100 tiles export --container geotiff` for that one zoom
and the frame's bounds, in the session's working folder.

- **Content**: SCAMIN is decided per zoom level, so every resolution shows
  the screen's content.
- **Size**: 512-pixel tiles draw the same zoom with every symbol, line,
  pattern and text twice as many pixels across, so each keeps its size on
  paper.
- **Detail**: up to 192 dpi. Above that, Pro resamples the tiles to the
  export resolution, so a 300 or 600 dpi export is sharper than a WMTS
  export but not as sharp as the resolution allows.

`IChartRasterRenderer` is the seam: a renderer that draws at any pixel
density can replace `TilesExportRenderer` without changing the export
(see [Upstream](#upstream)).

## Alternatives rejected

- **`s100 render --bbox` with `--symbol-scale` and `--text-scale` of
  DPI/96.** It's sharp at any resolution, but its composite form (`--layer`)
  draws everything whatever the scale, and the symbol and text scales leave
  line widths, dash patterns and area fills at screen size, so they print a
  third as wide at 300 dpi. Its scale comes from the pixel size, too, so it
  can't separate the two.
- **WMS GetMap.** It has no DPI parameter yet (U8).
- **Rendering in Pro's process with the library.** EncDotNet.S100 0.27.0's
  `S100CompositeOptions.PixelDensity` does what's needed, but rendering is
  long-running work the brief keeps out of Pro's process, and the composite
  facade doesn't cull by scale.
- **A helper process of our own over the library.** It would repeat the
  CLI's portrayal set-up and ship another copy of the runtime and library
  beside `s100`.

## Limits

- A time-varying layer is drawn at one step index (`--time-step`). For a
  layer of one time series, or of several on the same steps, that's the
  server's picture; for series on different steps, each dataset draws its
  own step at that index, not its step nearest map time.
- The reference latitude of SCAMIN is the frame's middle, where the server
  uses its data's middle; over a small chart they're the same zoom's scale.
- Map frames are rendered north up over their Web Mercator envelope; Pro
  rotates and projects the rendered image into the frame.
- Each export adds and removes layers, which Pro records as edits to the
  map.

## Upstream

Asks of EncDotNet.S100 (not filed; the maintainer has approved the first):

- **U8, DPI-aware WMS GetMap**: `DPI`, `MAP_RESOLUTION` and
  `FORMAT_OPTIONS=dpi:N`, choosing the zoom from the scale and the pixel
  density from the DPI, and more than 4096 pixels when DPI is over 96. A
  renderer over it would give full detail at 300 and 600 dpi.
- **`tiles export --time <instant>`**, snapping each dataset to its own
  nearest step as `tiles serve` does.
- **`--tile-size` above 512** (the library's `ChartTileOptions.PixelRatio`
  already goes to 4).

## Testing

`tests/export/test_export_dpi.py` exports a layout over US5MA1BO with arcpy
at 96 and at 300 dpi, rendered as the add-in renders it, and checks that
magenta symbols cover the same share of the frame and that the 300 dpi
frame, averaged down, shows the same marks in the same places. A control
rendered the way DPI-unaware WMS does fails both. It needs ArcGIS Pro's
Python, so CI doesn't run it; see [CONTRIBUTING.md](../../CONTRIBUTING.md#testing).
