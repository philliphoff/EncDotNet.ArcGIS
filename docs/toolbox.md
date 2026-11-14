# SoundCharts Tools

**SoundCharts Tools** is a Python toolbox of geoprocessing tools for
SoundCharts for ArcGIS. It adds SoundCharts chart layers to a map, exports
tiles and images, validates datasets and exchange sets, and converts between
S-57 and S-101. Run the tools from the Geoprocessing pane, in ModelBuilder,
or from Python.

Each tool runs `s100`, EncDotNet.S100's command-line tool, which ships inside
the add-in. The tools take the same inputs and options as `s100`; see
[EncDotNet.S100's command-line docs](https://github.com/philliphoff/EncDotNet.S100/blob/main/docs/cli.md)
for what each option does to the output.

Charts are not for navigation.

| Tool | Name in Python | What it does | `s100` command |
|---|---|---|---|
| [Add SoundCharts Layer](#add-soundcharts-layer) | `AddSoundChartsLayer` | Adds datasets to a map as a SoundCharts chart | `info` |
| [Export Tiles](#export-tiles) | `ExportTiles` | Writes Web Mercator raster tiles: XYZ, PMTiles, MBTiles, GeoPackage or Cloud-Optimized GeoTIFF | `tiles export` |
| [Render to Image](#render-to-image) | `RenderToImage` | Writes a PNG, JPEG or WebP image | `render` |
| [Validate Dataset](#validate-dataset) | `ValidateDataset` | Checks datasets against their product specification's rules | `validate` |
| [Verify Exchange Set](#verify-exchange-set) | `VerifyExchangeSet` | Checks an exchange set's signatures and checksums | `validate` |
| [Convert Dataset](#convert-dataset) | `ConvertDataset` | Converts S-57 to S-101 or S-401, or S-101 to S-57 | `s57 convert`, `s101 convert` |

## Open the toolbox

The toolbox is part of the add-in. When Pro loads SoundCharts for ArcGIS, it
copies the toolbox to
`%LOCALAPPDATA%\ESRI\ArcGISPro\Toolboxes\{ffae4aca-2edb-461d-8702-45b62936cf11}`
and lists **SoundCharts Tools** in the Geoprocessing pane's **Toolboxes** tab.
Search the pane for a tool's name to open it.

The first time you open a tool, Pro may ask whether you trust the toolbox's
Python code.

To use the toolbox without installing the add-in, add
`src/EncDotNet.ArcGIS.SoundCharts/Toolboxes/SoundChartsTools.pyt` from a
clone of this repository to a project (**Insert** > **Toolbox** > **Add
Toolbox**). Keep the `soundcharts_tools` folder beside it.

## Run a tool from Python

Import the toolbox, then call a tool by its name with the parameter names
below:

```python
import arcpy

arcpy.ImportToolbox(r"C:\Tools\SoundChartsTools.pyt", "soundcharts")
arcpy.soundcharts.ExportTiles(
    in_data=r"C:\Charts\US5MA1BO\US5MA1BO.000",
    out_tiles=r"C:\Tiles\US5MA1BO.pmtiles",
    max_zoom=15)
```

Messages from `s100` appear in `arcpy.GetMessages()`. A tool that fails
raises `arcpy.ExecuteError`.

## The s100 the tools run

The tools look for `s100` in this order:

1. The file that the `SOUNDCHARTS_S100_PATH` environment variable names. The
   add-in uses the same variable. When it's set to a file that doesn't
   exist, the tools stop instead of running a different build.
2. The build inside the installed add-in, under
   `%LOCALAPPDATA%\ESRI\ArcGISPro\AssemblyCache\{ffae4aca-2edb-461d-8702-45b62936cf11}\server`:
   the Arm64 build on Arm64 Windows, otherwise the x64 build.
3. A `server` folder beside the toolbox, and in a clone of this repository,
   `artifacts\server` from `tools/build-server.ps1`.
4. `s100` on `PATH`.

Every tool says which `s100` it ran, and with which arguments, in its first
messages.

## Add SoundCharts Layer

Adds datasets, exchange sets or folders to a map as a SoundCharts chart: a
**SoundCharts · {source}** group with a **Chart** layer, and **Water level**
(S-104) and **Surface currents** (S-111) layers when the data has them. These
are the layers **Add SoundCharts Data** makes on the ribbon, and the add-in
treats them the same way.

The tool reads each dataset with `s100 info`, writes a layer file (`.lyrx`)
and adds it to the map. The add-in then starts a chart server for the layers'
datasets and connects them, as it does when a project opens. Without the
add-in, the layers draw nothing unless you give a chart server URL.

| Parameter | Name | Default | Description |
|---|---|---|---|
| Input datasets, exchange sets or folders | `in_data` | required | Dataset files (`.000`, `.h5`, `.gml`), exchange sets (a folder, `CATALOG.XML` or `CATALOG.031`) and folders, searched recursively. Extract a `.zip` first. |
| Map | `in_map` | the active map | The map to add the chart to. |
| Output layer file | `out_layer_file` | the scratch folder | Where to write the `.lyrx`. |
| Palette | `palette` | `Day` | `Day`, `Dusk` or `Night`. |
| Chart server WMTS capabilities URL | `server_url` | none | A running `s100 tiles serve`'s WMTS capabilities URL, for a map without the add-in. The add-in replaces it with its own server. |
| Output layer | `out_layer` | derived | The group layer. |

Datasets `s100` can't read are skipped with a warning. Update files (`.001`
and so on) are applied to their base cell.

The layer file records each dataset's product and edition. Unlike **Add
SoundCharts Data**, it takes the producer from the file name (`US5MA1BO`
gives `US`) and records no footprint, so **Zoom to Data** has nothing to zoom
to for these layers.

## Export Tiles

Renders datasets, an exchange set or a folder as raster tiles on the Web
Mercator grid, with the chart's own portrayal. Runs `s100 tiles export`.

| Parameter | Name | Default | Description |
|---|---|---|---|
| Input datasets, exchange set or folder | `in_data` | required | One exchange set or folder, or any number of dataset files. `s100` can't combine an exchange set or folder with other inputs. |
| Output tiles | `out_tiles` | required | A folder, or a `.pmtiles`, `.mbtiles`, `.gpkg` or `.tif` file. An existing file is replaced. |
| Container | `container` | from the output | `XYZ folder`, `PMTiles`, `MBTiles`, `GeoPackage` or `Cloud-Optimized GeoTIFF`. It must match the output's extension. |
| Minimum zoom | `min_zoom` | from the data | 0 to 24. |
| Maximum zoom | `max_zoom` | from the data | 0 to 24. |
| Extent | `extent` | the data's extent | Writes only the tiles in this extent. |
| Palette | `palette` | `Day` | `Day`, `Dusk` or `Night`. |
| Time step | `time_step` | `0` | Zero-based time step of S-104 and S-111 datasets. One tile set holds one step. |
| Hide text | `no_text` | off | Leaves out text. |
| Draw the offline land basemap | `offline_basemap` | off | Draws the bundled Natural Earth land layer under the chart. |
| Tile size | `tile_size` | `256` | `256`, or `512` for high-DPI tiles of the same grid. |
| Tile format | `tile_format` | `PNG` | `PNG`, `JPEG` or `WebP`. |
| Quality (JPEG, WebP) | `quality` | `90` | 1 to 100. |
| Skip empty tiles | `skip_empty` | off | Doesn't write tiles with nothing drawn on them. |
| Products (exchange sets and folders) | `only` | all | Tiles only these products, comma-separated, such as `S101,S102`. |
| Allow more than 100,000 tiles | `allow_large` | off | Without it, `s100` stops before writing more than 100,000 tiles. Narrow the extent or zoom range first. |

The progress bar follows the zoom levels as `s100` finishes them.

Add a GeoPackage or Cloud-Optimized GeoTIFF to a map like any raster. A
PMTiles or MBTiles archive is for web maps and tile servers, such as
`s100 tiles serve`.

## Render to Image

Renders datasets, or every dataset in an exchange set, to one image. Runs
`s100 render`. The image isn't georeferenced.

| Parameter | Name | Default | Description |
|---|---|---|---|
| Input datasets or exchange set | `in_data` | required | As for Export Tiles. |
| Output image | `out_image` | required | A `.png`, `.jpg`, `.jpeg` or `.webp` file. The extension sets the format. |
| Width (pixels) | `width` | `1024` | 1 to 16384. |
| Height (pixels) | `height` | `768` | 1 to 16384. |
| Extent | `extent` | fit to the data | The area to draw. |
| Palette | `palette` | `Day` | `Day`, `Dusk` or `Night`. |
| Time step | `time_step` | `0` | Zero-based time step of S-104 and S-111 datasets. |
| Hide text | `no_text` | off | Leaves out text. |
| Draw the offline land basemap | `offline_basemap` | off | Draws the bundled Natural Earth land layer under the chart. |
| Background colour | `background` | opaque white | `#RRGGBB` or `#AARRGGBB`. |
| Quality (JPEG, WebP) | `quality` | `90` | 1 to 100. |

## Validate Dataset

Checks each dataset against its product specification's rule pack. Runs
`s100 validate`. Findings are warnings: the tool succeeds and sets **Valid**
to false when a dataset fails, so a model can branch on it.

| Parameter | Name | Default | Description |
|---|---|---|---|
| Input datasets | `in_datasets` | required | One or more `.000`, `.h5` or `.gml` files. |
| Fail on warnings | `strict` | off | A dataset with warnings fails as well as one with errors. |
| Suppress rules | `suppress` | none | Rule ids to ignore, comma-separated, with `*` as a wildcard, such as `S101-R-1.2,S101-*`. |
| Ignore S-101 update files | `no_updates` | off | Validates an S-101 base cell without its updates. |
| Output report (JSON) | `out_report` | none | Writes every dataset's full report. |
| Valid | `out_valid` | derived | True when every dataset passes. |

The messages list up to 200 findings per dataset; the JSON report has them
all. A product without a rule pack, such as S-401, gives a warning that
nothing was checked.

The report is:

```json
{
  "valid": false,
  "datasets": [
    { "dataset": "C:\\Charts\\101AA00DS0003.000", "report": { "specification": "S-101", "findings": [] } }
  ]
}
```

where each `report` is `s100 validate --format json`'s output.

## Verify Exchange Set

Verifies the signature and checksum of each file in an exchange set: S-100
Part 15 for a `CATALOG.XML` exchange set, and CRCs and signatures for an S-57
or S-63 `CATALOG.031`. Runs `s100 validate` on the exchange set. It doesn't
check the datasets against their rules; use Validate Dataset for that.

| Parameter | Name | Default | Description |
|---|---|---|---|
| Input exchange set | `in_exchange_set` | required | A folder, `CATALOG.XML`, `CATALOG.031` or `.zip`. |
| Fail unsigned files and files without a checksum | `strict` | off | Also fails files that aren't signed or have no checksum. |
| Output report (JSON) | `out_report` | none | Writes `s100 validate --format json`'s report. |
| Valid | `out_valid` | derived | True when every file passes. |

## Convert Dataset

Converts one cell between S-57 and S-101. Runs `s100 s57 convert` or
`s100 s101 convert`.

| Parameter | Name | Default | Description |
|---|---|---|---|
| Input cell | `in_cell` | required | An S-57 or S-101 base cell (`.000`). Update files beside it are applied first. |
| Conversion | `conversion` | `S-57 to S-101` | `S-57 to S-101`, `S-57 inland ENC to S-401` or `S-101 to S-57`. |
| Output cell or folder | `out_data` | required | The `.000` to write, or with **Write an S-57 exchange set**, the folder. |
| Write an S-57 exchange set | `exchange_set` | off | S-101 to S-57 only. Writes `ENC_ROOT\CATALOG.031` and the cell. |
| Cell name | `cell_name` | the input's name | With an exchange set, the cell's eight-character name, such as `XX5CELL1`. |
| Output translation report (JSON) | `out_report` | none | What was translated, dropped or folded, from `--report`. |
| Ignore update files | `no_updates` | off | Converts the base cell without its updates. |

An S-57 cell made from S-101 follows S-65 Annex C. It's for testing and for
tools that read only S-57, not a production ENC.

## Messages and errors

Each tool relays `s100`'s output as geoprocessing messages: progress as
messages, warnings (such as a skipped dataset) as warnings, and errors as
errors. When `s100` fails, the tool fails with what its exit code means:

| Exit code | Meaning |
|---|---|
| `1` | `s100` stopped with an unexpected error. |
| `2` | `s100` couldn't detect the product, or couldn't read or resolve the inputs. |
| `3` | The product specification doesn't support headless rendering. |
| `4` | The dataset is recognised, but its structure or encoding isn't supported. |
| `5` | The dataset doesn't conform to its product specification. |
| `6` | Validation found failing findings, or failed signature or checksum checks. |
| other | `s100` rejected the arguments or the inputs, such as a missing file. |

**Cancel** stops `s100`. When Export Tiles replaces a PMTiles, MBTiles,
GeoPackage or GeoTIFF file, a cancelled export leaves the old file as it was:
`s100` writes the new one beside it and moves it into place when it's done.

The tools turn off `s100`'s update check, so they don't go online.

## Test the toolbox

The toolbox's logic is in the `soundcharts_tools` package, which doesn't
use arcpy. Its tests run with any Python 3.9 or later, including Pro's:

```bash
"C:\Program Files\ArcGIS\Pro\bin\Python\envs\arcgispro-py3\python.exe" -I -m unittest discover -s tests/toolbox -v
```

The tests load `SoundChartsTools.pyt` against a stand-in for arcpy. The ones
that run `s100` need a build the toolbox can find (see
[The s100 the tools run](#the-s100-the-tools-run)) and
`SOUNDCHARTS_S100_TEST_DATASETS` set to EncDotNet.S100's `tests/datasets`
folder; they skip otherwise. CI runs them all.

`tests/toolbox/test_layerfile.py` checks that the layer file is the add-in's
layer document, byte for byte, against the add-in's snapshots in
`tests/EncDotNet.ArcGIS.SoundCharts.Tests/ChartLayers/Snapshots`. When you
change the layers the add-in writes, update the toolbox's
`soundcharts_tools/layerfile.py` to match.
