# Contributing to EncDotNet.ArcGIS

Thanks for your interest in contributing! EncDotNet.ArcGIS is SoundCharts for
ArcGIS: an ArcGIS Pro add-in that brings S-100 portrayal from
[EncDotNet.S100](https://github.com/philliphoff/EncDotNet.S100) into ArcGIS
Pro. This guide covers how to build, test and submit changes. The
repository follows EncDotNet.S100's layout and conventions; where this guide
is silent, do what EncDotNet.S100 does.

## Prerequisites

- Windows. ArcGIS Pro and its add-ins are Windows-only.
- Visual Studio 2022 (17.14 or later) or Visual Studio 2026, or their Build
  Tools, with the .NET desktop workload.
  Esri's add-in packaging targets use `CodeTaskFactory`, which only the .NET
  Framework MSBuild in Visual Studio supports.
- The [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or
  later. `global.json` asks for 10.0.100 and rolls forward to the newest
  installed SDK, including a later major version such as .NET 11; the
  projects target .NET 10 either way, which is the runtime Pro 3.7 hosts.
- ArcGIS Pro 3.7 or later, to run the add-in. It isn't needed to build or
  test: the Pro API reference assemblies come from the
  `Esri.ArcGISPro.Extensions30` NuGet package.
- A git client.

## Building

### The chart server

The add-in draws charts with `s100 tiles serve` from EncDotNet.S100, run as
a separate process, and ships a pinned build of it (design brief D21). No
EncDotNet.S100 release serves WMTS yet, so the pin is a commit, in
`tools/s100-server.json`. Build it once, before the add-in:

```powershell
tools/build-server.ps1
```

The script fetches that commit into `artifacts/s100-src` and publishes the
CLI for `win-x64` and `win-arm64` into `artifacts/server/<rid>/`, a few
minutes the first time. The add-in project packages whatever is there as
`server/<rid>/` in the `.esriAddinX`; without it the add-in still builds
and loads, but its chart servers fail with "isn't installed". On Arm64
Windows the add-in runs the `win-arm64` server natively, although Pro
itself runs as x64.

`-RestoreConfigFile` restores through a NuGet mirror, and
`-RollForwardLatestMajor` lets the upstream build use a newer major .NET
SDK. To move the pin, change `commit` and `version` in
`tools/s100-server.json`; CI caches the build by both.

To try another `s100` build without rebuilding the add-in, set
`SOUNDCHARTS_S100_PATH` to its `s100.exe` before starting Pro. *About
SoundCharts* shows which server runs.

### The add-in

Build and package the add-in with Visual Studio's MSBuild, from a
**Developer PowerShell for VS** (or with the full path to `MSBuild.exe`):

```powershell
msbuild EncDotNet.ArcGIS.slnx /restore /p:Configuration=Release
```

This writes
`src/EncDotNet.ArcGIS.SoundCharts/bin/Release/net10.0-windows/win-x64/EncDotNet.ArcGIS.SoundCharts.esriAddinX`.

`dotnet build` also works and is enough for compiling and testing, but it
skips packaging: under the .NET MSBuild, `src/ArcGISPro.DotNetBuild.targets`
replaces Esri's packaging targets, which would otherwise fail with MSB4801.

The add-in builds for `win-x64` only. Pro is an x64 process, including on
Arm64 Windows, where it runs under emulation. Building for one runtime also
keeps SkiaSharp's natives for other platforms out of the package; with all
of them the package's paths pass the Windows 260-character path limit.

> [!NOTE]
> If your clone sits in a deep folder and the build fails on long paths,
> clone it to a shorter path, or map a drive letter to it with
> `subst W: path\to\EncDotNet.ArcGIS` and build from `W:\`.

## Running the add-in in Pro

Esri's packaging targets install the add-in after each Visual Studio build
by running `RegisterAddIn.exe` from Pro's `bin` folder. That step can't run
when Pro isn't installed, under `dotnet build`, or when Windows'
`NoDefaultCurrentDirectoryInExePath` setting stops `cmd` finding the
program in its working folder. Install with the deploy script instead:

```powershell
tools/deploy-addin.ps1
```

It copies the `.esriAddinX` to
`<Documents>\ArcGIS\AddIns\ArcGISPro\{add-in id}\`, following a redirected
Documents folder. `-Path` installs another package, such as one from CI, and
`-Remove` uninstalls the add-in. Restart Pro after installing.

When Pro starts, the add-in writes a line with its versions, and one naming
the chart server it found, to the SoundCharts log,
`%LOCALAPPDATA%\SoundCharts\ArcGIS\logs\soundcharts-<yyyyMMdd>.log`, which
shows it loaded without opening the ribbon. The same log, with each chart
server's output, is in the **SoundCharts Log** pane (**SoundCharts** tab >
**Log**).

To try the add-in, open a map and choose **SoundCharts** > **Add
SoundCharts Data** (or **Add Folder**) with one of EncDotNet.S100's test
datasets, for example `tests/datasets/S57/US5MA1BO/US5MA1BO.000`. A
**SoundCharts · US5MA1BO** group appears with a **Chart** layer drawn by its
own chart server; **Object information** then picks in it. Selecting the
group or a layer shows the **SoundCharts Layer** tab, with **Zoom to Data**
and the chart's **Log**.

To try **Object information** on any map without adding data, build the
Debug configuration and start Pro with `SOUNDCHARTS_DEBUG_DATASETS` set to
dataset paths separated by `;`. The tool then looks in those datasets
whatever the map's layers. Release builds ignore the variable.

## Testing

```powershell
dotnet test EncDotNet.ArcGIS.slnx --configuration Release
```

Test conventions:

- Every bug fix and new piece of logic is accompanied by an xunit test in
  `tests/EncDotNet.ArcGIS.SoundCharts.Tests`.
- Tests run without Pro. The Pro assemblies in the build are reference
  assemblies only, so a test fails if it reaches a Pro type at runtime. Put
  logic that you want to test (settings, URL building, `.lyrx` authoring,
  the chart server) in `src/EncDotNet.ArcGIS.SoundCharts.Core`, which has no
  Pro reference, and keep the Pro-facing classes (modules, buttons, panes)
  thin.
- The chart server supervisor is tested with fake processes, and once
  against the real bundled `s100` (`BundledServerTests`), which is skipped
  when `tools/build-server.ps1` hasn't run.
- Tests that need optional external data, such as real S-100 datasets, skip
  dynamically with xunit v3's `Assert.SkipUnless(...)` /
  `Assert.SkipWhen(...)`, so CI doesn't fail without them.
- The Object information coverage tests (S-102, S-104, S-111) and the
  content detection tests of Add SoundCharts Data use EncDotNet.S100's own
  test datasets. Set `SOUNDCHARTS_S100_TEST_DATASETS` to the
  `tests/datasets` folder of an EncDotNet.S100 clone to run them; CI checks
  that folder out at the pinned release.
- The CIM the add-in authors is tested as `.lyrx` JSON snapshots in
  `tests/EncDotNet.ArcGIS.SoundCharts.Tests/ChartLayers/Snapshots`. After a
  deliberate change, run the tests with `SOUNDCHARTS_UPDATE_SNAPSHOTS=1`,
  review the snapshot diff, and load the new `.lyrx` in Pro (or with
  `arcpy.mp.LayerFile`) before committing it.
- **Never commit real ENC data files.** Use small synthetic fixtures, or skip
  tests that need live data.
- Check behaviour in Pro by hand: install the add-in, restart Pro and use it.
  Say in the pull request what you checked.

## Package management

All NuGet versions are managed centrally with **Central Package Management**
in `Directory.Packages.props`:

- Don't add `Version` attributes to individual `.csproj` files.
- Every `EncDotNet.S100.*` package uses the `EncDotNetS100Version` property,
  so they move together. To try another EncDotNet.S100 release locally, pass
  `-p:EncDotNetS100Version=<version>`.
- Before introducing a new dependency, run the `gh-advisory-database`
  security check and confirm the package has no known advisories. NuGet
  audit fails the build on a vulnerable direct or transitive package.
- The add-in runs inside Pro's process, alongside Pro's own copies of common
  assemblies. Prefer dependencies EncDotNet.S100 already brings, and check a
  new one doesn't conflict with an assembly Pro loads.

## Skills and instructions

Agent guidance lives in `.github/copilot-instructions.md`, with
file-specific rules in `.github/instructions/` and task skills in
`.github/skills/<name>/SKILL.md`:

| Area | Skill / instruction |
|---|---|
| Add-in code, DAML, Pro SDK, threading, packaging | `arcgis-pro-addin` |
| Developer docs under `docs/` and READMEs | `docs-writing` |

For S-100 product semantics (feature catalogues, portrayal, a product's
encoding), use EncDotNet.S100's own product skills, in that repository's
`.github/skills/`. Product logic belongs in EncDotNet.S100, not in the
add-in.

## Coding style

The normative style rules are in the
[C# coding style guide](docs/coding-style.md); read it before submitting
code. In brief:

- `PascalCase` for types, methods and properties, `camelCase` for locals and
  parameters, `_camelCase` for private fields.
- File-scoped namespaces, 4-space indents, Allman braces.
- Nullable reference types are enabled everywhere; avoid `!` suppression.
- All public APIs carry XML doc comments.
- Code namespaces start `EncDotNet.SoundCharts`, not
  `EncDotNet.ArcGIS.SoundCharts`. Inside a namespace with an `ArcGIS`
  segment, the Pro SDK's `ArcGIS.*` namespaces stop resolving, and the WPF
  code generator doesn't qualify them with `global::`.

The formatting rules are encoded in [`.editorconfig`](.editorconfig) and
checked in CI (the **Format check** job). Before pushing, apply and verify
formatting:

```powershell
dotnet format whitespace EncDotNet.ArcGIS.slnx
dotnet format style EncDotNet.ArcGIS.slnx --diagnostics IDE0005
dotnet format EncDotNet.ArcGIS.slnx --diagnostics IDE0005 IDE1006 --severity warn --verify-no-changes
```

## Documentation

- Write pages and READMEs to the
  [documentation style guide](docs/docs-style.md): pick a page type, use its
  structure, and describe rather than promote. Agents load the
  `docs-writing` skill.
- Design notes for the add-in live in `docs/design/`.
- UI text is localizable: strings shown by windows and panes live in
  `src/EncDotNet.ArcGIS.SoundCharts/Resources/Strings.resx`, and ribbon
  captions and tooltips in `Config.daml`. Copy UI labels from there exactly.

## Branch and pull request workflow

1. Create a topic branch off `main`.
2. Make focused changes that fully address one concern.
3. Make sure the build, the tests and the format check pass locally.
4. Open a pull request against `main` and fill in the
   [pull request template](.github/pull_request_template.md).
5. CI must pass before review. It builds the add-in with Visual Studio's
   MSBuild, runs the tests, and uploads the `.esriAddinX` as the **addin**
   artifact.

## Reporting bugs and requesting features

Use the issue templates under
[`.github/ISSUE_TEMPLATE/`](.github/ISSUE_TEMPLATE/). For questions and
general discussion, see [SUPPORT.md](SUPPORT.md). To report a security
vulnerability, follow [SECURITY.md](SECURITY.md); please don't open a public
issue for security problems.

## License

By contributing, you agree that your contributions will be licensed under the
[MIT License](LICENSE) that covers this project.
