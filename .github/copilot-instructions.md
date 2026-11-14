# Copilot Instructions for EncDotNet.ArcGIS

## Project overview

EncDotNet.ArcGIS is **SoundCharts for ArcGIS**: an ArcGIS Pro 3.7+ add-in
that brings S-100 nautical chart portrayal from
[EncDotNet.S100](https://github.com/philliphoff/EncDotNet.S100) into ArcGIS
Pro. The plan and its slices are in the epic,
[philliphoff/EncDotNet.ArcGIS#1](https://github.com/philliphoff/EncDotNet.ArcGIS/issues/1).
The solution targets .NET 10 (`net10.0-windows`, WPF) with
`<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`,
and follows EncDotNet.S100's repository conventions.

## Decisions that shape every change

- **The brand is SoundCharts.** The ribbon tab, contextual tabs, panes,
  Options page, toolbox and DAML ids are all "SoundCharts", never "S-100":
  Pro's built-in ENC support already uses "S-100" in its ribbon.
- **No file extension claims.** Don't take over `.000`, `.h5`, `CATALOG.XML`
  or other S-1xx formats in Pro's catalog. Data comes in through SoundCharts
  commands.
- **The chart server runs out of process.** Pro has no API for
  custom-drawn layers, so charts come from `s100 tiles serve` (WMTS, and WMS
  where needed) in a child process the add-in manages. A server failure must
  never take Pro down. The library runs in-process only for light work:
  identify, metadata, route checks.
- **The add-in owns its layer connections**, so it can carry the palette and
  mariner settings into requests and keep Pro's tile cache from showing
  stale settings.
- **Coexist with Pro's ENC layers.** Every SoundCharts layer says where it
  came from.
- **Permits are deferred.** S-100 Part 15 / S-63 permits aren't handled.
- **Not for navigation**, like the rest of the project. Keep the notice in
  the About window and the docs.

## Repository layout

```
src/
  EncDotNet.ArcGIS.SoundCharts/        # The add-in: Config.daml, module, ribbon controls, windows
    Config.daml                        # DAML: add-in info, module, tabs, groups, controls
    Resources/Strings.resx             # Localizable UI strings (+ hand-written Strings.cs)
  EncDotNet.ArcGIS.SoundCharts.Core/   # Logic with no Pro reference, for tests and other slices
    Charts/                            # DatasetSet, DatasetSource: what one chart server portrays
    Server/                            # ChartServerPool, ChartServer (supervisor), endpoints, process launch
    Logging/                           # SoundChartsLog: the Log pane's list and the daily file
    Settings/                          # Mariner settings, application settings, the render key's style, settings JSON
  ArcGISPro.DotNetBuild.targets        # Skips Esri's packaging under `dotnet build`
tests/
  EncDotNet.ArcGIS.SoundCharts.Tests/  # xunit v3 tests for logic that doesn't need Pro
tools/
  build-server.ps1                     # Builds the pinned s100 chart server into artifacts/server/
  s100-server.json                     # The pin: EncDotNet.S100 commit and version
  deploy-addin.ps1                     # Installs the .esriAddinX for the current user
docs/                                  # Coding and docs style guides, design notes (docs/design/)
```

## Technology stack

| Concern | Library / tool |
|---|---|
| Language / platform | C#, .NET 10 (`net10.0-windows`, `win-x64`) |
| Host | ArcGIS Pro 3.7+ (x64; under emulation on Arm64 Windows) |
| Pro API | `Esri.ArcGISPro.Extensions30` NuGet package (reference assemblies and packaging targets) |
| UI | WPF, DAML (`Config.daml`), Pro's `ProWindow` and Esri styles |
| S-100 | `EncDotNet.S100` NuGet packages (one version, `EncDotNetS100Version`) |
| Testing | xunit v3, coverlet |
| Package management | Central Package Management via `Directory.Packages.props` |
| CI | GitHub Actions on `windows-latest` with Visual Studio's MSBuild (`.github/workflows/ci.yml`) |

## Building and testing

- Build the pinned chart server once with `tools/build-server.ps1`; the
  add-in packages `artifacts/server/<rid>/`. Without it the add-in builds,
  but chart servers can't start and `BundledServerTests` skips.
- Package the add-in with Visual Studio's MSBuild:
  `msbuild EncDotNet.ArcGIS.slnx /restore /p:Configuration=Release`. Esri's
  packaging targets use `CodeTaskFactory`, which the .NET MSBuild doesn't
  support.
- `dotnet build` and `dotnet test` work on Windows but skip packaging.
  On Linux (cloud sessions), `dotnet build` and `dotnet format` work
  (`EnableWindowsTargeting`), but the tests need the Windows desktop
  runtime.
- Run tests with `dotnet test EncDotNet.ArcGIS.slnx --configuration Release`.
- Install into Pro with `tools/deploy-addin.ps1`, then restart Pro. The
  module logs its versions and the chart server it found to
  `%LOCALAPPDATA%\SoundCharts\ArcGIS\logs\soundcharts-<yyyyMMdd>.log` when
  Pro starts.
- Chart servers run out of process (`s100 tiles serve`), one per dataset
  set, supervised by `ChartServer`: loopback, a random token, a long-path
  working folder, a kill-on-close job. Nothing a server does may throw into
  Pro; report it through the log and the server's state.
- You can't click in Pro from an agent session. Verify what you can with
  tests and the log, and ask the user to check UI behaviour in Pro.

## Requirements for changes

### Tests
- Every bug fix and new piece of logic comes with an xunit test in
  `tests/EncDotNet.ArcGIS.SoundCharts.Tests`.
- Tests run without Pro: the Pro assemblies are reference-only, so a test
  that reaches a Pro type fails. Keep testable logic in classes that don't
  use the Pro API; keep modules, buttons, panes and view models thin.
- Use xunit's dynamic skips (`Assert.SkipUnless(...)` / `Assert.SkipWhen(...)`)
  for tests that need optional external data.
- Never commit real ENC data files.

### Naming and style
- **The normative C# style guide is [`docs/coding-style.md`](../docs/coding-style.md); read it and make generated code comply.** It is encoded in [`.editorconfig`](../.editorconfig).
- **Code namespaces start `EncDotNet.SoundCharts`**, not
  `EncDotNet.ArcGIS.SoundCharts` (the project and assembly names). A
  namespace with an `ArcGIS` segment hides Pro's `ArcGIS.*` namespaces.
- Nullable reference types are enabled everywhere — avoid `!` suppression.
- All public APIs carry XML doc comments. Classes Pro creates from DAML are
  `internal sealed`.
- Before finishing a change, run
  `dotnet format EncDotNet.ArcGIS.slnx --diagnostics IDE0005 IDE1006 --severity warn --verify-no-changes`;
  CI's **Format check** job runs it.

### UI
- Every UI string is localizable: window and pane text in
  `Resources/Strings.resx` with a matching `Strings` property, ribbon text in
  `Config.daml`. No hard-coded user-facing strings in XAML or code.
- Every button has a tooltip; every interactive control has an
  `AutomationProperties.AutomationId` (`<View>.<Element>`, English, never
  localized) and a short, localized accessible name.
- Use Pro's styles (`Esri_Button`, `Esri_TextBlockRegular`, …) and dynamic
  resources so windows follow Pro's light, dark and high-contrast themes.

### Package dependencies
- All NuGet versions are managed centrally in `Directory.Packages.props`. Do not add `Version` attributes to individual `.csproj` files.
- Run `gh-advisory-database` security checks before introducing any new dependency.
- The add-in shares Pro's process. Check a new dependency doesn't clash with
  an assembly Pro already loads.

### S-100 semantics
- Product logic (reading, portrayal, validation, route checks) belongs in
  EncDotNet.S100. If the add-in needs something the library doesn't expose,
  file an issue there instead of reimplementing it here.

## Skills and instructions

Skills live under `.github/skills/<name>/SKILL.md`, and file-specific rules
under `.github/instructions/`. Load them by task signal:

| Signal in task or files touched | Load skill |
|---|---|
| Add-in code, `Config.daml`, ribbon controls, panes, `QueuedTask`, Pro SDK API, layers and maps, packaging and deployment | `arcgis-pro-addin` |
| Adding, rewriting or reviewing developer docs: `docs/**`, root Markdown files, project READMEs | `docs-writing` |

For S-100 product semantics, use EncDotNet.S100's per-product skills (in
that repository's `.github/skills/`), and run `s100 --skill` before choosing
`s100` commands or arguments for the chart server.
