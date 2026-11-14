# EncDotNet.ArcGIS

[![CI](https://github.com/philliphoff/EncDotNet.ArcGIS/actions/workflows/ci.yml/badge.svg)](https://github.com/philliphoff/EncDotNet.ArcGIS/actions/workflows/ci.yml)

## Overview

**SoundCharts for ArcGIS** is an ArcGIS Pro add-in that brings S-100
nautical chart portrayal from
[EncDotNet.S100](https://github.com/philliphoff/EncDotNet.S100) into ArcGIS
Pro: chart layers with mariner settings, time-aware S-104 and S-111 layers,
object information, route checking and geoprocessing tools.

It's a work in progress, planned in the
[SoundCharts for ArcGIS Pro epic](https://github.com/philliphoff/EncDotNet.ArcGIS/issues/1).
So far the add-in adds a **SoundCharts** tab to the ribbon with a **Log**
button, which opens the **SoundCharts Log** pane, and an **About
SoundCharts** button, which shows the versions of the add-in,
EncDotNet.S100, the chart server, ArcGIS Pro and .NET. It ships the chart
server that later slices draw charts from: a pinned `s100 tiles serve`,
run and supervised outside Pro's process.

SoundCharts charts are not for navigation.

## Requirements

- ArcGIS Pro 3.7 or later, on Windows (x64, or Arm64 with Pro running under
  emulation).

## Install

1. Download `EncDotNet.ArcGIS.SoundCharts.esriAddinX` from the **addin**
   artifact of a [CI run](https://github.com/philliphoff/EncDotNet.ArcGIS/actions/workflows/ci.yml).
2. Close ArcGIS Pro.
3. Double-click the `.esriAddinX` file, or run
   [`tools/deploy-addin.ps1`](tools/deploy-addin.ps1) with `-Path` set to it.
4. Start ArcGIS Pro and open the **SoundCharts** tab.

## Building

The add-in builds with Visual Studio's MSBuild. Esri's packaging targets
don't run under `dotnet build`, which compiles and tests the add-in but
doesn't produce the `.esriAddinX`.

```powershell
tools/build-server.ps1
msbuild EncDotNet.ArcGIS.slnx /restore /p:Configuration=Release
dotnet test EncDotNet.ArcGIS.slnx --no-build --configuration Release
tools/deploy-addin.ps1
```

See [CONTRIBUTING.md](CONTRIBUTING.md) for prerequisites and details.

## Repository layout

| Path | Contents |
|---|---|
| `src/EncDotNet.ArcGIS.SoundCharts/` | The ArcGIS Pro add-in: `Config.daml`, the module, ribbon controls, panes and windows |
| `src/EncDotNet.ArcGIS.SoundCharts.Core/` | The add-in's logic that doesn't use the Pro SDK: dataset sets, the chart server and its supervisor, the SoundCharts log |
| `tests/EncDotNet.ArcGIS.SoundCharts.Tests/` | xunit tests for the add-in's logic that doesn't need Pro |
| `tools/` | Developer scripts: `build-server.ps1` (the pinned chart server, pinned in `s100-server.json`) and `deploy-addin.ps1` |
| `docs/` | Contributor documentation and design notes |

## License

This project is licensed under the [MIT License](LICENSE).
