# SoundCharts for ArcGIS: design brief

Status: **Proposed** (2026-11-13). Decisions D1–D36 are proposed for the
maintainer to accept, change or reject. Slice D,
[#2](https://github.com/philliphoff/EncDotNet.ArcGIS/issues/2), of epic
[#1](https://github.com/philliphoff/EncDotNet.ArcGIS/issues/1). The UI slices
build from the decisions at the end: A5
([#7](https://github.com/philliphoff/EncDotNet.ArcGIS/issues/7)), A6
([#8](https://github.com/philliphoff/EncDotNet.ArcGIS/issues/8)), A11
([#13](https://github.com/philliphoff/EncDotNet.ArcGIS/issues/13)) and A12
([#14](https://github.com/philliphoff/EncDotNet.ArcGIS/issues/14)) cite them,
and so do A2–A4 and A7–A10 where they touch the same model.

This brief decides how the add-in looks and behaves in ArcGIS Pro 3.7 before
the UI slices start, so they share one model: the ribbon and panes, how
SoundCharts layers sit in a map, how the chart server runs, where mariner
settings live, how Pro's map time reaches S-104, S-111 and S-124, and the
words the add-in uses. It's written as text, with no mocks: Pro's ribbon,
Contents pane and dockpanes are a fixed vocabulary, so the questions are
about structure and wording, not layout.

Upstream references are to EncDotNet.S100 `main` at `67e3a7cf`: the `s100`
command-line tool ([`docs/cli.md`](https://github.com/philliphoff/EncDotNet.S100/blob/main/docs/cli.md#tiles-serve)),
the SoundCharts viewer's strings
([`Strings.resx`](https://github.com/philliphoff/EncDotNet.S100/blob/main/src/EncDotNet.S100.Viewer/Resources/Strings.resx)),
and the viewer's design notes, chiefly
[tide predictions](https://github.com/philliphoff/EncDotNet.S100/blob/main/docs/design/tide-predictions/tide-predictions-design.md),
[time overlays](https://github.com/philliphoff/EncDotNet.S100/blob/main/docs/design/time-overlays.md),
[dataset collections](https://github.com/philliphoff/EncDotNet.S100/blob/main/docs/design/dataset-collections.md)
and [logs panel](https://github.com/philliphoff/EncDotNet.S100/blob/main/docs/design/logs-panel.md).

## 1. Starting point

### 1.1 Decisions already taken (epic #1)

- **Brand is SoundCharts**, never "S-100", in the ribbon, panes, Options,
  toolbox and DAML ids. Pro's built-in ENC support already says "S-100".
- **No file extension claims.** Data comes in through **Add SoundCharts
  Data** and **Open with SoundCharts**.
- **The chart server runs out of process**: `s100 tiles serve`, WMTS for
  display and WMS where needed. In-process library use is limited to
  identify, metadata and route checks.
- **The add-in owns its layer connections**, so it can change a layer's URL to
  stop Pro's tile cache showing stale settings.
- **Coexist with Pro's ENC layers**: every SoundCharts layer says where it
  came from.
- **Permits deferred** (S-100 Part 15, S-63). **Not for navigation.**

### 1.2 Step 0 facts that shape the design

| Fact | Consequence here |
|---|---|
| Pro 3.7.3 hosts x64 .NET 10, under emulation on ARM64. | The server is a separate process, so it can run as native win-arm64 (§4). |
| A 300 dpi layout export sends WMS 3.125× the pixels with no DPI hint, so symbols print smaller and SCAMIN content changes. | Display uses WMTS; export is A10's own path, not the display layer (D10). |
| WMTS and WMS pick different zoom levels for the same scale. | A map never mixes the two for one chart (D10). |
| Explore returns only the top GetFeatureInfo hit (`FEATURE_COUNT=1`). | Object information is a SoundCharts map tool that runs identify in-process (D6). |
| Below the dataset minimum zoom, the layer vanishes. | The chart layer gets a matching minimum scale and footprints show below it (D13). |
| arcpy reported `supports("TIME")` but the layer wasn't time-enabled, and Pro never sent `TIME`. | The add-in drives time itself (§6). |
| A short (8.3) working directory crashed the server. | The server runs from a long-path session folder (D19). |

### 1.3 What the server offers today

From `docs/cli.md` on `main`. This sets what the add-in can do without
upstream changes.

- **One layer, `s100`**, per server: WMTS on `WebMercatorQuad` and WMS 1.3.0.
  All datasets the server was started with are composited into it.
- **Styles are palettes**: `day`, `dusk`, `night`. Nothing else.
- **Display options are start-up options**: `--palette`, `--symbol-scale`,
  `--text-scale`, `--no-text`, `--hide`, `--national-language`,
  `--display-mode` (S-411). There's no safety contour, display category or
  other mariner setting.
- **Time**: a WMTS `Time` dimension (REST segment after the style, or KVP
  `TIME=`), and `TIME` on WMS. Time snaps to each dataset's nearest step;
  out of range it snaps to the last step without saying so.
- **Datasets are fixed at start.** The server re-checks files every
  `--refresh` seconds and reloads when they change, but has no call to add
  or remove a dataset.
- **Ports and tokens**: `--port` (default 8200), `--token` (letters, digits,
  `-`, `_`) as a path prefix, `/healthz` outside the token.
- **Disk cache**: `--cache-dir`, keyed by a fingerprint of input files,
  options and `s100` version; several servers can share one folder.
- **GetFeatureInfo** on WMS only, up to 200 features.

## 2. Ribbon and panes

### 2.1 The SoundCharts tab

One core tab, caption **SoundCharts**, always present. Commands that need a
map are disabled without one (DAML conditions), not hidden.

| Group | Command | Kind | What it does |
|---|---|---|---|
| **Data** | **Add SoundCharts Data** | Button (large) | Opens a file browser for datasets and exchange sets and adds them to the active map's SoundCharts chart (A3, D11). |
| | **Library** | Button (large) | Opens the **SoundCharts Library** pane (A12). |
| **Display** | **Palette** | Split button: **Day**, **Dusk**, **Night**, **Follow Pro theme** | Sets the project palette (D23, D26). |
| | **Display category** | Combo box: **Display Base**, **Standard**, **Other Information**, **All** | Sets the project display category (D23). |
| | **Soundings**, **Text** | Check boxes | Quick toggles over the same settings as the Options page. |
| | **Mariner Settings...** | Button (small) | Opens Pro's Options at the **SoundCharts** page. |
| **Time** | **Now** | Button | Sets map time to now, snapped (§6). Enabled while the map has a time-varying SoundCharts layer. |
| | **‹ Previous data**, **Next data ›** | Buttons | Step map time to the previous or next time step of any SoundCharts layer (§6). |
| **Inquire** | **Object information** | Map tool | Click a point to list everything there in the **SoundCharts Object information** pane (A6). |
| | **Route check** | Button | Opens the **SoundCharts Route check** pane (A11). |
| **Output** | **Export SoundCharts Chart...** | Button | A10's layout export at layout scale and DPI. |
| | **SoundCharts Tools** | Button | Opens the Python toolbox (A9) in the Geoprocessing pane. |
| **Help** | **Log** | Button | Opens the **SoundCharts Log** pane. |
| | **About SoundCharts** | Button | Versions (add-in, EncDotNet.S100, `s100`, Pro, .NET, process architecture) and the "not for navigation" notice (§7.1). |

- **No Time group of our own beyond these three buttons.** Pro already shows
  its **Time** contextual tab when a map has time-aware layers; the slider,
  span and playback stay Pro's. Ours only adds what Pro can't know: the data's
  steps and "now".
- **Group order follows a user's day**: get data, set the display, set the
  time, ask questions, produce output, get help.
- **Mariner settings get two quick toggles and a dialog, not a full group.**
  The four depth values are numbers a user sets once per project; the
  toggles are what people flip while working.

### 2.2 The SoundCharts Layer contextual tab

A contextual tab group, caption **SoundCharts Layer**, shown when the
Contents selection includes a SoundCharts layer (§3). One tab, caption
**SoundCharts**, the way Pro's **Feature Layer** group carries its own tabs.

| Group | Command | What it does |
|---|---|---|
| **Datasets** | **Zoom to Data** | Zooms to the footprints of the selected layer's datasets, not the union of all layers (A3, A7). |
| | **Datasets...** | Lists the chart's datasets (name, product, edition, footprint) with **Remove** (D12). |
| **Coverage** | **Footprints** | Check box: shows or hides the chart's **Coverage** layer. |
| | Scale note | A read-only label, "Zoom in to see the chart" when the map is below the chart's minimum scale (A7). |
| **Time** | Time status | A read-only label for time-varying layers: the step drawn, or "No data at map time · last 12 Nov 18:00, 3 h earlier" (§6). |
| | **Go to Nearest Data** | Moves map time to the layer's nearest step, as the viewer's **Go to nearest data**. |
| **Server** | Server status | "Running · 4 datasets", "Starting...", "Restarting...", "Stopped". |
| | **Restart** | Restarts the chart's server (§4). |
| | **Log** | Opens the **SoundCharts Log** pane filtered to this chart. |

### 2.3 Panes

All are Pro dockpanes, opened from the tabs above, and remember their dock
position the way Pro panes do. Captions start with "SoundCharts" because
they dock beside Pro's own panes, where **Library** or **Route check** alone
could be anyone's.

| Pane | Opened by | Content | Slice |
|---|---|---|---|
| **SoundCharts Object information** | The **Object information** tool | Every feature and coverage value at the clicked point, grouped by dataset, as the viewer's Object information panel | A6 |
| **SoundCharts Library** | **Library** | Dataset collections: sources, datasets, details and actions | A12 |
| **SoundCharts Route check** | **Route check**, and **Check Route** on a route | The verdict, clearance and findings, grouped **MUST REVIEW**, **CHECK**, **INFO** | A11 |
| **SoundCharts Log** | **Log** (both tabs) | Add-in events and server output, with the viewer's level filter and **Copy all** | A2 |

- **Object information replaces Explore, it doesn't fight it.** Explore on a
  SoundCharts WMTS layer finds nothing, because WMTS has no GetFeatureInfo.
  That's acceptable: the SoundCharts tool is one click away and returns
  every feature (D6).
- **The Log is a pane, not a file link.** A server failure is the most
  likely support question, and the user needs to see the server's stderr
  where they are. **Open Log Folder** in the pane's toolbar covers the file.
- **No Timeline pane.** Pro's time slider is the Timeline here (§6).
- **No Legend pane.** Pro's Contents shows a WMTS layer's legend image if the
  service has one; the server publishes none today, so legends are an
  upstream question (U9).

### 2.4 DAML ids

Every id starts `SoundCharts_`, for example `SoundCharts_Tab`,
`SoundCharts_LayerTabGroup`, `SoundCharts_AddData`,
`SoundCharts_ObjectInfoTool`, `SoundCharts_ObjectInfoPane`,
`SoundCharts_LibraryPane`, `SoundCharts_RouteCheckPane`,
`SoundCharts_LogPane`, `SoundCharts_OptionsPage`, and conditions such as
`SoundCharts_HasChartLayerCondition`. The module id is
`SoundCharts_Module`. Nothing uses `S100` or `ENC` in an id.

## 3. Layer model

### 3.1 One SoundCharts group per map

Recommendation: each map holds at most one **SoundCharts** group layer, and
**Add SoundCharts Data** adds to it. Inside the group:

```text
SoundCharts · 4 datasets             group layer
├── Surface currents                 WMTS, time-aware        (S-111 datasets)
├── Water level                      WMTS, time-aware        (S-104 datasets)
├── Chart                            WMTS                    (everything else)
└── Coverage                         feature layer           (footprints, A7)
```

- **One composite chart, not one layer per dataset.** The server composites
  datasets: S-101 cells of different usage bands are drawn best-scale-first,
  text is decluttered across them, and S-102 sits under S-101. Pro stacking
  one WMTS layer per cell would overdraw overlapping bands, double text and
  cost a server (or a layer in one) per cell. Pro's own ENC support puts one
  layer per cell; ours is a chart, the way SoundCharts draws one.
- **Split by time behaviour, not by product.** S-104 and S-111 change with
  map time; the rest don't. In one composite layer, every time step would
  re-request the whole chart. Separate layers let the static **Chart** stay
  cached while only the time layers refetch. **Water level** and **Surface
  currents** are the viewer's Timeline names for S-104 and S-111. They're
  drawn above **Chart**, as their portrayal is an overlay.
- **S-124 stays in Chart for now.** It's time-dependent by validity, but the
  server doesn't filter warnings by time (U7). It moves to its own
  **Navigational warnings** layer when it does.
- **A layer exists only while it has datasets.** A map with only S-101 has
  only **Chart** and **Coverage**.
- **One group per map, not per Add.** A second **Add SoundCharts Data** joins
  the same chart, so cells from two exchange sets composite as one chart.
  Rejected: a group per add (overlapping cells from two adds overdraw), and a
  layer per dataset (above).

### 3.2 Footprints, findings and feature layers

- **Coverage** (A7) is a feature layer in a project-local file geodatabase
  (D15), one feature per dataset: name, product, edition, issue date,
  dataset path, and, for time-varying datasets, first and last time. Outline
  only, labelled below the chart's minimum scale. It's how a user selects a
  dataset: select a footprint, then **Datasets...** or **Remove from
  chart**.
- **Route check findings** (A11) are not in the SoundCharts group. They
  belong to a route, not to the chart: a **Route check · {route name}** group
  with **Findings (points)** and **Findings (segments)** layers, placed above
  the SoundCharts group. Running the check again replaces its features.
- **Plugin datasource layers** (A13) are ordinary feature layers named
  **{feature type} · {dataset}** in a **SoundCharts features · {dataset}**
  group, outside the chart group. They're data, not the chart, and Pro
  symbolizes them.

### 3.3 Telling SoundCharts layers from Esri ENC layers

A SoundCharts layer says so in five places, so it's never mistaken for Pro's
ENC layer:

1. **The group name**: **SoundCharts · {source}**, where source is the cell
   or exchange set name for one dataset or set ("SoundCharts · US5MA1BO"),
   and "{n} datasets" otherwise. Child layers keep short names
   (**Chart**, **Water level**), since the group says SoundCharts.
2. **The contextual tab**: **SoundCharts Layer** appears for it and for no
   other layer.
3. **Layer metadata**: title, summary "SoundCharts portrayal of {products}
   from {producers}", credits naming the producers, and the use limitation
   "Not for navigation." (§7.1).
4. **The source**: Layer Properties > Source shows the
   `http://127.0.0.1:{port}/{token}/wmts/...` connection, which doesn't look
   like Esri's.
5. **The add-in's own marker** (§3.4), so a renamed layer or one dragged out
   of the group is still recognized.

Rejected: "SoundCharts" in every child name ("Chart · SoundCharts"), which
doubles the Contents width for a word the group already says.

### 3.4 How the add-in finds its layers

The add-in keeps a registry in the project (Pro module settings, saved with
the `.aprx`): for each SoundCharts layer, its layer URI, kind (chart, water
level, surface currents, coverage), dataset list and chart id. The registry,
not the name or URL, is what makes a layer a SoundCharts layer. A layer that
has lost its registry entry (copied to another project by hand) is shown
with its last URL and offered **Reconnect** if the datasets can be found.

Pro may offer custom properties on a CIM layer that would carry the marker
in the layer itself, which survives copy and paste; that's open question P3.

## 4. Server lifecycle (A2)

### 4.1 One server per chart layer, pooled by content

Recommendation: a server is one `s100 tiles serve` process for one
**dataset set**: the datasets of one SoundCharts layer plus the start-up
options that change its tiles. The add-in keeps a pool keyed by that set.
Two layers with the same set share a server.

- **Today's server has one layer**, so each WMTS layer in §3.1 needs its own
  process: a map with S-101, S-104 and S-111 runs three. They're small (one
  per layer, not per dataset) and they portray different data, so nothing is
  duplicated.
- **Pooling covers layouts.** An overview or inset map with the same data as
  the main map gets the same dataset set and shares its server.
- **Per layer, not per session.** One process for everything would restart
  every chart to add one dataset to one map, and a crash would blank every
  map. Per layer keeps a restart local.
- **Upstream would reduce it to one process per map**: several WMTS layers
  from one server (U3) and adding datasets without a restart (U2). The pool
  key stays the same.

### 4.2 Start, stop and restart

- **Start on demand.** A server starts when a project with SoundCharts
  layers opens (for every layer in the registry, whether or not its map is
  open, because layouts draw closed maps), and when a layer is added.
  Start-up is cheap; portrayal happens on the first tile.
- **Stop when unused.** A server stops when no layer in any open project uses
  its set: when the layer is removed, its datasets change, or the project
  closes. All stop when Pro exits.
- **Die with Pro.** Every server is in a Windows job object with
  kill-on-close, so a Pro crash doesn't leave servers running.
- **Health.** The add-in asks `/healthz` every 10 seconds. A server that
  exits or fails three checks is restarted, at most three times in five
  minutes. After that it's left stopped: the layer's status says
  **Stopped**, a Pro notification says "SoundCharts chart server stopped:
  {reason}" with **Show Log**, and **Restart** tries again.
- **Restart repoints.** A restarted server gets a new port and token. The
  add-in rewrites the WMTS connection of every layer using it (it owns
  them), which also busts Pro's tile cache for that layer.
- **Change of datasets is a restart.** Adding or removing a dataset starts a
  server for the new set, repoints the layer, then stops the old one, so the
  map shows the old chart until the new one answers. With `--cache-dir`, a
  restart with an unchanged set serves its disk tiles at once; a changed set
  renders anew (U2 asks for better).

### 4.3 Ports, tokens and folders

- **Loopback only.** `--host 127.0.0.1`, always.
- **Port**: the add-in asks Windows for a free loopback port and passes it
  with `--port`, retrying with another if the bind fails. `tiles serve`
  doesn't document `--port 0` (U4).
- **Token**: a new random 128-bit token per start (`--token`, base64url), so
  another local process can't guess the URL, and every start has a new URL.
  `/healthz` has no token.
- **Refresh off.** `--refresh 0`: the add-in watches the dataset files itself
  and restarts on a change (§5.4). A server that reloads by itself would serve
  new data under the old URL, and Pro would mix cached old tiles with new
  ones (U5).
- **Viewer off.** `--no-viewer`.
- **Folders** under `%LOCALAPPDATA%\SoundCharts\ArcGIS\`, always on the long
  path:
  - `sessions\{Pro process id}\`: each server's working directory, deleted at
    exit;
  - `tile-cache\`: one `--cache-dir` shared by all servers, capped by the
    Options page (default 2048 MB);
  - `logs\`: one log file per day.
- **Which `s100`.** A pinned `s100` build bundled in the add-in, win-arm64 on
  ARM64 Windows and win-x64 otherwise. A path on the Options page overrides
  it, for testing a newer build. *About* shows which one runs.

### 4.4 Logs and failures

- **One log**, in the **SoundCharts Log** pane and the daily file: add-in
  events (server start, stop, restart, repoint, dataset changes, settings
  hash changes) and each server's stdout and stderr, tagged with the chart.
  Levels and filter wording match the viewer's Logs tab: **Info and above**,
  **Warnings and errors**, **Errors**, **Everything, Debug included**.
- **Times** in the log follow the **Time format** setting (D28), with the
  date.
- **Nothing in a server reaches Pro as an exception.** Process I/O runs on
  background threads; failures become log entries, a layer status and, for a
  stop, one notification.
- **Paths are redacted** to `~` in **Copy all**, as the viewer does.

## 5. Settings and the settings hash (A5)

### 5.1 Scope: per project, with application defaults

Recommendation: mariner settings and palette are **project** settings, saved
with the `.aprx`, and every SoundCharts layer in the project uses them. New
projects start from **application** defaults.

- **One set for all charts**, as in SoundCharts and an ECDIS: the safety
  contour is the vessel's, not a layer's. Two layers with different safety
  contours on one map would draw two inconsistent charts.
- **Per project, not per user**, because Pro work is shared and printed. A
  layout made with a 10 m safety contour must print the same on a colleague's
  machine.
- **Pro's Options dialog has both scopes.** A **SoundCharts** page under
  **Project** holds the project's settings; a **SoundCharts** page under
  **Application** holds the defaults for new projects, the server and cache
  options, and **Time format**.
- **Per layer: only what Pro already does** (visibility, transparency, scale
  range). Rejected: per-layer mariner settings, for the reason above.
- **Per map palette** is open question P8: a layout comparing day and night
  would need it.

Every settings page and the tab's **Display** group carry one line: "These
settings apply to SoundCharts layers only, not to Esri ENC layers."

### 5.2 The settings

Labels are the viewer's, from `Strings.resx`.

| Setting (viewer label) | Values | Scope | Server today | Upstream |
|---|---|---|---|---|
| Palette (**Day**, **Dusk**, **Night**) | | Project | WMTS style | — |
| **Follow Pro theme** | Off by default | Application | Add-in only | — |
| Display category (**Display Base**, **Standard**, **Other Information**, **All**) | **Standard** | Project | None | U1 |
| **Safety Contour** | 30 m | Project | None | U1 |
| **Safety Depth** | 30 m | Project | None | U1 |
| **Shallow Contour** | 2 m | Project | None | U1 |
| **Deep Contour** | 30 m | Project | None | U1 |
| **Four-shade depth scheme** | Off | Project | None | U1 |
| **Highlight shallow-water dangers** | Off | Project | None | U1 |
| **Plain boundaries** | Off | Project | None | U1 |
| **Simplified symbols** | Off | Project | None | U1 |
| **Full light lines** | Off | Project | None | U1 |
| Soundings | On | Project | None (`--hide` can't select soundings) | U1 |
| Text | On | Project | `--no-text` | — |
| **Symbol Scale**, **Text Scale** | 1.0 | Project | `--symbol-scale`, `--text-scale` | — |
| **National Language** | the catalogue's | Project | `--national-language` | — |
| **Ignore scale minimum** | Off | Project | None | U1 |
| **Depth Unit** | metres | Application | None | U1 |
| **Time format** (**Local time**, **UTC**) | **UTC** | Application | Add-in only | — |
| Draft, UKC margin | none | Project | Add-in only (route check, in-process) | — |

The defaults shown are the `s100` tool's where it has one (safety contour and
depth, 30 m) and placeholders otherwise. The rule is the viewer's defaults:
A5 checks each against the viewer and corrects this table.

**Draft** and the UKC margin are route-check inputs (A11), on the
**SoundCharts** project page under **Route check**, beside a route's own
values. The viewer says "draft", so the add-in does too, not "draught".

### 5.3 Follow Pro theme

Off by default, because a printed chart wants **Day** whatever the screen
looks like. When on: Pro's **Light** theme gives **Day**, **Dark** gives
**Dusk**, as the viewer's S-100 Dusk chrome picks Dusk. **Night** is only
ever chosen by hand: it's for a dark bridge, not a dark UI. The add-in
listens for Pro's theme change and applies it at once.

### 5.4 The settings hash

Every SoundCharts WMTS URL carries a **render key**, so a change to anything
that changes the tiles gives new URLs, and Pro's tile cache can't show old
tiles.

- **What's hashed**: the project's settings that the server uses, in a
  canonical form (fixed key order, invariant culture, metres, the schema
  version), plus the `s100` version and the **data revision** of the layer's
  datasets (each file's path, size and last write time, update files and
  exchange-set catalogues included: the server's own fingerprint inputs).
  The palette isn't in the hash: it's already the style.
- **Form**: SHA-256 of that text, the first 8 bytes as 16 lower-case hex
  characters. Stable across machines for the same settings and data.
- **Target, with U1**: the WMTS style is `{palette}-{hash}`, for example
  `night-3f9a1c02e47b5d18`, and the server keeps the settings each hash
  stands for. A settings change is a new style, and a new style is a new URL.
- **Today, without U1**: the start-up options are the only settings, so a
  change to them is a server restart (§4.2), and the new token puts a new
  path in every URL. The hash goes into the token as its suffix
  (`{random}-{hash}`), so the log and URL still show which settings a tile
  was drawn with. Palette changes need no restart: they change the style.
- **Data revision**: with `--refresh 0`, the add-in watches the files and,
  when a change has held for two checks 10 seconds apart (the server's rule),
  restarts with the new revision. U5 asks the server to publish its own
  revision so it can reload in place.
- **Settings that don't reach the server** (Time format, Follow Pro theme,
  Draft) aren't hashed.

### 5.5 Applying a change

A change on the Options page or the tab applies at once to every
SoundCharts layer in the project: the add-in recomputes the hash, rewrites
the affected WMTS connections (and restarts servers whose start-up options
changed), and logs "Mariner settings applied", the viewer's wording. Pro
redraws the layers from the new URLs.

## 6. Time (A8)

### 6.1 Who drives time

Recommendation: Pro's time slider is the control; the add-in turns Pro's map
time into the WMTS time segment of each time-varying layer's URL.

- **Pro's native WMTS time isn't relied on.** In step 0, Pro never sent
  `TIME`. Whether a CIM WMTS layer can be made time-aware so Pro sends the
  `Time` dimension is open question P1. If it can, the add-in still snaps
  and checks the range (§6.3), because the server doesn't say when it's out
  of range.
- **The add-in listens to map time changes** on each map view and, for each
  time-varying SoundCharts layer, writes the snapped step into the REST path:
  `wmts/1.0.0/s100/{style}/{time}/WebMercatorQuad/{z}/{y}/{x}.png`. Each step
  has its own URL, so Pro caches each one and stepping back is instant.
- **The slider needs a time-aware layer.** The **Coverage** layer is
  time-enabled from its first and last time fields, for time-varying
  datasets, so Pro shows the slider with the data's full extent and the
  map's time properties offer it. Its step interval is set to the datasets'
  most common step (for example 1 h for S-104, 30 min for some S-111). If
  Pro can't take the extent and step from a feature layer this way, that's
  open question P2.
- **Several maps, several times.** Each map view has its own time. A layer
  shown in two views is the same layer, so it can only have one URL; the
  add-in follows the **active** map view's time. This is open question P4.

### 6.2 Which instant

Pro's map time is a range. The instant the add-in uses is the range's
**end**: with a span, Pro's slider shows the window up to the current time,
and the end is "now" in that window. With no span, start and end are the
same.

Pro's time is converted to UTC before snapping; the server takes UTC.

### 6.3 Snapping and out of range

- **Snap to the nearest step**, per layer, from the steps the add-in reads
  from the datasets in-process (cheap metadata, §1.1). The server snaps each
  dataset to its own nearest step, which is the same rule, so the URL's time
  is the layer's nearest step and the server's choice per dataset is
  unchanged.
- **In range** means within half a step of the first or last step. Between
  steps the nearest is drawn, as SoundCharts does.
- **Out of range**, the layer draws nothing, and says so. The add-in points
  the layer's time segment at an out-of-range time once U6 lands, so the
  server answers empty tiles; until then it turns the layer's drawing off
  without touching the Contents check box (P9). The contextual tab's time status and the layer's
  tooltip in the Contents pane say "No data at map time · last {time},
  {span} earlier" or "No data at map time · next {time}, {span} later", the
  viewer's wording, with **Go to Nearest Data**. Rejected: the server's
  current behaviour, drawing the last step, which shows yesterday's water
  level as today's.
- **Gaps** inside the range (a missing run) are treated like out of range
  when the nearest step is more than one step interval away.

### 6.4 The default time

When the first time-varying SoundCharts layer is added to a map without map
time, the add-in sets map time to **now**, snapped, if now is in the data's
range; otherwise to the nearest end of the range (a forecast that starts
tomorrow opens at its first step; last week's observations at their last).
It never moves map time that the user has already set.

**Now** on the tab sets map time to now, once. There's no **Live** mode that
keeps up with the clock: Pro's slider doesn't follow the clock, and an
add-in that moved map time by itself would fight the user. That's open
question P5 if users ask for it.

### 6.5 Products

| Product | Time comes from | Map time does |
|---|---|---|
| S-104 water level | Time steps of the series (fixed stations and grids) | Picks the step drawn in **Water level**; Object information samples there |
| S-111 surface currents | Time steps | Picks the step drawn in **Surface currents**; Object information samples there |
| S-124 navigational warnings | Each warning's validity | Nothing on the map yet (U7). Object information says "In force at map time" or "Not in force at map time" per warning, from the library |
| Others (S-101, S-102, S-122...) | None | Nothing |

Weather (GRIB2, NetCDF) and S-411 have time steps in the server too. They
aren't in the MVP's product list; when they are, each gets a time layer by
the same rule as S-104 and S-111.

### 6.6 Previous and next data

**‹ Previous data** and **Next data ›** move map time to the previous or
next step of any time-varying SoundCharts layer in the active map (the union
of their steps), as the viewer's data steps do. They move the slider; Pro's
own **Time** tab still steps by its interval.

## 7. Wording

### 7.1 Not for navigation and producer credit

The viewer says "Not for navigation" in the Library, the route check footer
("Not for navigation · computed from {0} for your settings") and pick cards.
The add-in says it in the same words, where a user would otherwise take the
chart as authoritative:

- **About SoundCharts**: "SoundCharts for ArcGIS draws S-100 and S-57 data
  for analysis and planning. It isn't certified for navigation. Not for
  navigation."
- **Layer metadata** of every SoundCharts layer: use limitation "Not for
  navigation.", and credits "Data: {producers}. Portrayal: SoundCharts
  ({s100 version})."
- **Route check pane footer**: the viewer's footer, word for word.
- **Object information**: when a dataset's producer marks it not for
  navigation, the viewer's **NOT FOR NAVIGATION** strip and its tooltip "The
  data provider marks this data as not for navigation".
- **Export SoundCharts Chart...** (A10) checks the layout for a text element
  holding "Not for navigation" and, if there's none, offers to add one:
  "SoundCharts · Not for navigation · Data: {producers}". It doesn't add it
  without asking: the layout is the user's.

**Producers** are named the way the viewer's exchange-set pane does
("Producer: {0}"), from the exchange set catalogue or the dataset's metadata,
by agency name where known and code otherwise. A chart with data from
several producers lists them all, in order of dataset count.

No banner or watermark on the map itself. The map is the user's to print or
screenshot, and a watermark in the tiles would also show in analysis
exports.

### 7.2 Observed, predicted and forecast

S-104 and S-111 (Edition 2.0.0) say how each series was produced, in
`dataDynamicity`. The viewer reads it but doesn't show it yet. The add-in
labels every water level and current value with it, so a user never takes a
forecast for an observation:

| `dataDynamicity` | Label | Example in Object information |
|---|---|---|
| 1 observation | **Observed** | "Water level · observed 12 Nov 14:00 UTC · 1.32 m" |
| 2 astronomical prediction | **Predicted** | "Water level · predicted 12 Nov 14:00 UTC · 1.30 m · astronomical tide only, not a forecast" |
| 3 analysis or hybrid | **Analysis** | "Water level · analysis 12 Nov 14:00 UTC · 1.31 m" |
| 4 hydrodynamic hindcast | **Hindcast** | "Water level · hindcast 12 Nov 14:00 UTC · 1.29 m" |
| 5 hydrodynamic forecast (S-111: 6) | **Forecast** | "Water level · forecast, run 12 Nov 12:00 UTC · 1.35 m" |
| absent | no label | "Water level · 12 Nov 14:00 UTC · 1.31 m" |

- **The words are the viewer's**: "Predicted", "astronomical tide only, not a
  forecast", "run {0}", "analysis", "forecast". "Not a forecast" goes with
  every astronomical prediction, as the viewer's tide cards do.
- **Run time** for forecasts from the dataset's issue time, as "run {time}".
- **Uncertainty**: shown when the dataset gives it; otherwise "S-104
  uncertainty not provided", the viewer's wording.
- **The same labels** appear in the **Water level** and **Surface currents**
  layers' metadata summary ("S-104 · forecast, run 12 Nov 12:00 UTC") and in
  route check findings that use water level.
- **Harmonic tide predictions** (the viewer's NOAA stations) aren't in this
  epic. If they come, they use the viewer's **Predicted · {station}** wording
  and dashed-means-predicted (tide predictions D7, D10).

### 7.3 Names and terms

| Write | Not |
|---|---|
| SoundCharts (the add-in, in the UI) | S-100, the S-100 add-in, ENC |
| SoundCharts for ArcGIS (the product, in docs and *About*) | SoundCharts Pro, ArcGIS SoundCharts |
| chart (the composited SoundCharts layer) | map service, ENC layer |
| dataset, exchange set, Library | cell (except for S-57/S-101 cells), catalog (Pro's word), collection (in UI text; it's the file format's word) |
| Object information | Identify, Info, pick report |
| Water level, Surface currents, Navigational warnings | S-104 layer, S-111 layer |
| Day, Dusk, Night | day mode, night theme |
| Mariner Settings | display settings, chart settings |
| chart server (in the log and messages) | tile server, WMTS server, backend |
| Not for navigation | not to be used for navigation, unofficial |

Pro's own words win for Pro's own things: **Contents**, **Catalog**,
**Options**, **map time**, **time slider**, **layout**. Product
specifications are named as EncDotNet.S100's docs style says (S-101, S-104,
never S101). Times follow **Time format**: UTC as `12 Nov 2026 14:00 UTC`,
local in the system culture, as the viewer.

## 8. Open questions for upstream (EncDotNet.S100)

To file there. The epic lists some already; this is the full list the design
needs, in the order the slices need them.

| # | Question | Needed by | Without it |
|---|---|---|---|
| U1 | **Mariner settings in `tiles serve`**: display category, safety contour and depth, shallow and deep contours, four shades, soundings, shallow-water dangers, plain boundaries, simplified symbols, full light lines, ignore SCAMIN, depth unit. Proposal: the client registers a settings set (`PUT /{token}/settings/{hash}`, JSON) and uses `{palette}-{hash}` as the WMTS style and WMS `STYLES`; or the same values as start-up options at least. | A5 | Palette, scales, text and national language only |
| U2 | **Add and remove datasets at run time**, or a manifest file input (`tiles serve session.json`) re-read on change, with the disk cache keyed per dataset so one new cell doesn't re-render the rest. | A2, A3 | Restart with the new set; full re-render |
| U3 | **Several WMTS layers in one server**, one per group of datasets (`chart`, `S104`, `S111`, `S124`), each with its own time dimension. | A2 | One process per layer |
| U4 | **`--port 0`** with a machine-readable start-up line (JSON with the bound URLs). | A2 | The add-in picks a port and retries |
| U5 | **Publish the data revision** (the disk-cache fingerprint) in TileJSON, capabilities and `/healthz`, so a client can put it in its URLs and let the server reload in place. | A5 | `--refresh 0` and add-in restarts |
| U6 | **Strict time**: a time more than half a step outside a dataset's range answers `204`, and TileJSON says which datasets have no data at that time. Today it snaps to the last step silently. | A8 | The add-in hides the layer itself |
| U7 | **S-124 by validity**: filter warnings to those in force at `TIME` / `t`. | A8 | S-124 always draws every warning |
| U8 | **DPI-aware WMS GetMap**: `DPI`, `MAP_RESOLUTION`, `FORMAT_OPTIONS=dpi:N`; zoom and symbol scale chosen from DPI; more than 4096 px when DPI > 96. | A10 | A10 renders through the library or `s100 render` |
| U9 | **Legends**: a WMTS `LegendURL` per style, from the viewer's Legend sections, so Pro's Contents shows a key for S-102 shades and S-111 speed bands. | A5 (later) | No legend in Contents |
| U10 | **WMS and WMTS pick different zoom levels for the same scale**: make WMS GetMap choose the zoom WMTS would at that scale. | A10 | Only WMTS for display (D10) |
| U11 | **An overview below the minimum zoom**, so the chart doesn't vanish when zoomed out. | A7 | Footprints only |
| U12 | **A released CLI with WMS and WMTS** (v0.27.0 serves XYZ only), and a fix for the `PhysicalFilesWatcher.OnRenamed` crash in `tiles serve`. | A2 | Pin a nightly build |
| U13 | **Show `dataDynamicity` in the viewer** with the labels in §7.2, so both products say "observed", "forecast" the same way. | A6 (consistency) | The add-in labels first |
| U14 | **Publish dataset metadata the add-in needs without opening datasets twice**: time steps, footprints, producer, not-for-navigation flag, as JSON (for example `tiles.json` `s100.datasets`). | A3, A7, A8 | The add-in opens the datasets in-process for metadata |

## 9. Open questions about Pro (to settle in the slices)

These are Pro SDK capabilities this brief assumes or works around, not yet
checked in Pro 3.7. Each names the slice that settles it and the fallback.

| # | Question | Slice | Fallback in this brief |
|---|---|---|---|
| P1 | Can a CIM WMTS layer be made time-aware so Pro sends the WMTS `Time` dimension from map time? | A8 | The add-in writes the time into the URL (§6.1) |
| P2 | Can a feature layer's time extent and step interval be set from the SDK so the slider shows the data's steps? | A8 | Set map time properties directly |
| P3 | Do CIM layers carry custom properties the add-in can use as its marker? | A3 | The project registry (§3.4) |
| P4 | Can one layer in two map views draw different times (for example a layer per view)? | A8 | Follow the active view's time |
| P5 | Do users want a live mode that follows the clock? | after A8 | **Now** button only |
| P6 | Does Pro's local cache keep WMTS tiles across sessions, and is a new URL enough to bypass it in every case (layouts, exports, the Catalog thumbnail)? | A5 | Assume a new URL is enough, as step 0 saw |
| P7 | Do layer credits show in the map's attribution bar for a WMTS layer, and can the SDK set layer metadata (use limitation, credits) on a service layer? | A3 | Credits in the layer description and the Object information footer |
| P8 | Should palette be per map (for layouts comparing day and night)? | A5 | Per project |
| P9 | Can the add-in turn a layer's drawing off without changing its Contents check box, and show a tooltip there? | A8 | Turn the layer off and say why in the time status |
| P10 | Is the Options dialog's Project scope the right home for mariner settings, or do users expect them on the map's properties? | A5 | Project scope |

## Decisions

Proposed, for the slices to cite once accepted.

### Ribbon and panes

1. **D1 SoundCharts tab.** One core tab, **SoundCharts**, with groups
   **Data**, **Display**, **Time**, **Inquire**, **Output** and **Help**, and
   the commands in §2.1. Commands that need a map are disabled without one,
   not hidden.
2. **D2 SoundCharts Layer contextual tab.** A tab group, **SoundCharts
   Layer**, with one tab, **SoundCharts**, shown when the Contents selection
   includes a SoundCharts layer. Groups **Datasets**, **Coverage**, **Time**
   and **Server** (§2.2).
3. **D3 Pane names.** **SoundCharts Object information**, **SoundCharts
   Library**, **SoundCharts Route check**, **SoundCharts Log**. No Timeline or
   Legend pane.
4. **D4 DAML ids.** Every id starts `SoundCharts_`; none contains `S100` or
   `ENC`.
5. **D5 Mariner settings on the ribbon.** **Palette**, **Display category**,
   **Soundings** and **Text** on the tab; everything else on the Options page,
   reached by **Mariner Settings...**.
6. **D6 Object information tool.** A SoundCharts map tool, not Pro's
   Explore. A click runs the library's identify in-process on the datasets
   of every visible SoundCharts layer, lists all features and coverage values
   at the point grouped by dataset, at map time, and highlights the selected
   feature's geometry. Explore on a SoundCharts layer finds nothing, and
   that's accepted.
7. **D7 Route check pane.** Findings grouped **MUST REVIEW**, **CHECK**,
   **INFO**, as the viewer; each finding links to Object information at its
   position.
8. **D8 Log pane.** Add-in events and server output in one list, the viewer's
   level filters, **Copy all** with paths redacted, **Open Log Folder**.
9. **D9 Library pane.** The viewer's Library model (sources, indexed
   datasets, nothing loaded until asked) with the actions **Add to Map**,
   **Show Footprint**, **Validate**, **Open Containing Folder**. Drag and drop
   onto the pane only.

### Layer model

10. **D10 WMTS for display.** Every SoundCharts display layer is WMTS. WMS
    is used only by A10's export and never beside WMTS for the same chart.
11. **D11 One group per map.** A map has at most one **SoundCharts · {source}**
    group; **Add SoundCharts Data** adds to it.
12. **D12 Layers by time behaviour.** In the group: **Surface currents**
    (S-111), **Water level** (S-104), **Chart** (everything else, S-124
    included until U7), **Coverage** (footprints). A layer exists only while
    it has datasets. Removing a dataset is done from **Datasets...** or a
    selected footprint.
13. **D13 Zoom floor.** The chart layers' minimum scale is the scale of
    their minimum zoom, so Contents shows them out of scale; **Coverage** is
    labelled below that scale and the contextual tab says "Zoom in to see the
    chart".
14. **D14 Findings and feature layers outside the group.** Route check
    findings go in a **Route check · {route}** group; plugin datasource layers
    in a **SoundCharts features · {dataset}** group.
15. **D15 Project geodatabase.** Coverage and findings are written to a
    `SoundCharts.gdb` in the project's home folder, so they're saved and
    shared with the project.
16. **D16 Provenance.** A SoundCharts layer is identified by the project
    registry (§3.4), and says it's SoundCharts in its group name, the
    contextual tab, its metadata and credits.

### Server

17. **D17 One server per dataset set.** One `s100 tiles serve` process per
    distinct set of datasets and start-up options, pooled so layers with the
    same set share it. Started when a project with SoundCharts layers opens or
    a layer is added; stopped when no open project uses it.
18. **D18 Supervision.** Job object with kill-on-close; `/healthz` every 10 s;
    restart at most three times in five minutes, then **Stopped** with one
    notification and **Restart**.
19. **D19 Process settings.** `--host 127.0.0.1`, a free port chosen by the
    add-in, a new random token per start, `--refresh 0`, `--no-viewer`, a
    shared `--cache-dir`, and a working folder under
    `%LOCALAPPDATA%\SoundCharts\ArcGIS\sessions\` on the long path.
20. **D20 Restart repoints.** After any restart, the add-in rewrites the
    connection of every layer the server serves. Changing a layer's datasets
    starts the new server before stopping the old one.
21. **D21 Bundled server.** A pinned `s100` build ships in the add-in,
    win-arm64 on ARM64 and win-x64 otherwise, overridable on the Options
    page. *About* shows the version that runs.
22. **D22 Server failures stay out of Pro.** No server event throws into
    Pro; it becomes a log entry, a layer status, and at most one
    notification per stop.

### Settings

23. **D23 Project scope.** Palette and mariner settings are per project,
    saved with the `.aprx`, shared by every SoundCharts layer in it, seeded
    from application defaults. No per-layer mariner settings.
24. **D24 Options pages.** A **SoundCharts** page under **Project** (mariner
    settings, palette, route check draft and UKC margin) and one under
    **Application** (defaults, **Follow Pro theme**, **Time format**, the
    `s100` path, cache size).
25. **D25 Viewer labels and defaults.** Settings use the viewer's labels
    and defaults (§5.2). Each page says "These settings apply to SoundCharts
    layers only, not to Esri ENC layers."
26. **D26 Follow Pro theme.** Off by default; Light gives **Day**, Dark gives
    **Dusk**; **Night** only by hand.
27. **D27 Render key.** A 16-hex-character SHA-256 prefix of the canonical
    server-side settings, the `s100` version and the data revision. With U1
    it's the style suffix, `{palette}-{hash}`; until then, settings changes
    restart the server and the hash is the token's suffix. Palette changes
    are a style change, never a restart.
28. **D28 Time format.** **UTC** (default) or **Local time**, application
    wide, for every time the add-in shows: panes, log, layer tooltips.

### Time

29. **D29 The add-in drives time.** Pro's slider is the control; the add-in
    writes the snapped step into each time-varying layer's WMTS path for the
    active map view. The **Coverage** layer is time-enabled to give the
    slider the data's extent and step.
30. **D30 Instant and snapping.** The instant is the end of Pro's map time
    range, in UTC, snapped per layer to the nearest step.
31. **D31 Out of range.** More than half a step outside the range, or more
    than one step from the nearest step inside it, a layer draws nothing and
    says "No data at map time · last {time}, {span} earlier" (or "next ...
    later"), with **Go to Nearest Data**. Never the last step silently.
32. **D32 Default time.** When the first time-varying layer arrives in a map
    with no map time: now if in range, otherwise the nearest end. User-set
    map time is never moved. **Now** sets it once; no live mode.
33. **D33 Products.** S-104 and S-111 are time layers. S-124 is evaluated
    for validity at map time in Object information only, until U7.

### Wording

34. **D34 Not for navigation.** In *About*, every layer's metadata (use
    limitation and credits), the route check footer, producer-marked datasets
    in Object information, and offered (never forced) as a layout text
    element by **Export SoundCharts Chart...**. No map watermark.
35. **D35 Data kind labels.** Water level and current values carry
    **Observed**, **Predicted**, **Analysis**, **Hindcast** or **Forecast**
    from `dataDynamicity`, forecasts with "run {time}", predictions with
    "astronomical tide only, not a forecast". Absent, no label.
36. **D36 Terms.** The words in §7.3: SoundCharts never S-100 in the UI,
    Object information, Library, Mariner Settings, Day/Dusk/Night, chart
    server, Not for navigation, draft; Pro's own words for Pro's things.

### Which slices cite what

| Slice | Decisions |
|---|---|
| A2 Managed chart server | D8, D17–D22 |
| A3 Add SoundCharts Data and the chart layer | D1, D2, D4, D10–D12, D15, D16, D27 |
| A4 Open with SoundCharts | D9, D11, D36 |
| A5 Palette and mariner settings | D5, D23–D28 |
| A6 Object information | D3, D6, D33, D35 |
| A7 Coverage and the zoom floor | D12, D13, D15 |
| A8 Map time | D29–D33 |
| A9 Python toolbox | D19, D21 |
| A10 Export at layout scale and DPI | D10, D34 |
| A11 Route check | D3, D7, D14, D24, D35 |
| A12 Library pane | D3, D9, D11 |
| A13 Plugin datasource | D14, D16 |
