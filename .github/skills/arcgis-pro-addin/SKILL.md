---
name: arcgis-pro-addin
description: |
  Building the SoundCharts ArcGIS Pro add-in with the Pro SDK: Config.daml
  (modules, tabs, groups, buttons, panes, conditions), the add-in
  lifecycle, the Main CIM Thread and QueuedTask, Pro's WPF styles and
  ProWindow, packaging the .esriAddinX with Visual Studio's MSBuild,
  deploying to the per-user add-in folder, and checking that Pro loaded it.
  Also the project's own conventions: SoundCharts branding, namespaces,
  thin Pro-facing classes, testing without Pro.
  USE FOR: any change under src/EncDotNet.ArcGIS.SoundCharts; adding a
  ribbon control, pane, Options page or tool; calling the Pro API; build,
  packaging, CI or deploy problems. DO NOT USE FOR: S-100 product semantics
  (use EncDotNet.S100's product skills); docs prose (use docs-writing).
---

# ArcGIS Pro add-in (SoundCharts)

Read [`docs/coding-style.md`](../../../docs/coding-style.md) §11 and
[`.github/instructions/addin.instructions.md`](../../instructions/addin.instructions.md)
first; this skill adds background and procedures.

Esri's references: the
[Pro SDK wiki](https://github.com/Esri/arcgis-pro-sdk/wiki) — in particular
[ProConcepts Framework](https://github.com/Esri/arcgis-pro-sdk/wiki/ProConcepts-Framework)
(DAML, modules, plug-ins, conditions, threading) and
[ProConcepts Advanced Topics](https://github.com/Esri/arcgis-pro-sdk/wiki/ProConcepts-Advanced-Topics)
(assembly references, add-in loading) — and the
[community samples](https://github.com/Esri/arcgis-pro-sdk-community-samples).
The API reference is in the XML docs beside the reference assemblies in the
`Esri.ArcGISPro.Extensions30` package (`ref/net10.0-windows7.0/*.XML` in the
NuGet cache). Check an API there before you use it; don't guess member
names.

## How the add-in is put together

- **`Config.daml`** declares the add-in to Pro: `AddInInfo` (id, version,
  `desktopVersion` = the oldest Pro it runs on), then a module with its
  tabs, groups and controls. Pro reads DAML at start-up and builds the
  ribbon from it without loading the add-in's assembly.
- **The module** (`SoundChartsModule`, id `SoundCharts_Module`) is the
  add-in's singleton. It is `autoLoad="true"`, so Pro creates it at
  start-up: it logs its versions, and later slices start the chart server
  before a project's layers draw. Without `autoLoad`, Pro loads the
  assembly only when a control is first used.
- **Controls** (`button`, `tool`, `comboBox`, `dockPane`, …) name a
  `className` in the `defaultNamespace` (`EncDotNet.SoundCharts`). Pro
  creates them by reflection; keep them `internal sealed` and thin, calling
  into plain classes that tests can reach.
- **Ids** are `SoundCharts_<Thing>`. A released id is a contract: users'
  customizations and other add-ins may refer to it.
- **Images**: use Pro's built-in image names (`GenericInformation32`,
  `GenericInformation16`, …) or add PNGs under `Images/` as `Content`, with
  16 and 32 px sizes and dark-theme variants under `DarkImages/`.

## Threading

Pro's UI runs on the WPF dispatcher thread. Most of the mapping API (maps,
layers, CIM, geometry, geodatabase) must run on the **Main CIM Thread**:

```csharp
await QueuedTask.Run(() =>
{
    var map = MapView.Active?.Map;
    // ... Pro API calls that need the MCT ...
});
```

- Await `QueuedTask.Run`; never `.Wait()` or `.Result` on the UI thread.
- Calls documented "This method must be called on the MCT" throw
  `CalledOnWrongThreadException` elsewhere.
- Keep work on the MCT short: it is shared by every add-in and by Pro.
  Network calls, file parsing and the chart server go on the thread pool.

## UI

- Dialogs derive from `ArcGIS.Desktop.Framework.Controls.ProWindow`; set
  `Owner = FrameworkApplication.Current.MainWindow` and merge Pro's
  `Default.xaml` theme with `DesignOnlyResourceDictionary`, as
  `AboutWindow.xaml` does.
- Use Esri styles (`Esri_Button`, `Esri_TextBlockRegular`,
  `Esri_TextBlockH3`, …) and `DynamicResource` brushes so windows follow the
  light, dark and high-contrast themes.
- Text comes from `Resources/Strings.resx` (`{x:Static res:Strings.Key}`);
  automation ids and accessible names on every interactive control.
- A WPF project's implicit usings leave out `System.IO`; add it where
  needed.

## Build, package and deploy

| Task | Command |
|---|---|
| Build and package (`.esriAddinX`) | `msbuild EncDotNet.ArcGIS.slnx /restore /p:Configuration=Release` |
| Compile and test only | `dotnet test EncDotNet.ArcGIS.slnx --configuration Release` |
| Install for the current user | `tools/deploy-addin.ps1` (then restart Pro) |
| Uninstall | `tools/deploy-addin.ps1 -Remove` |

What to know:

- **Packaging needs Visual Studio's MSBuild.** Esri's targets
  (`Esri.ArcGISPro.Extensions30.targets`) zip `Config.daml`, `Content` items
  and the build output (`Install/`) with `CodeTaskFactory` inline tasks.
  Under the .NET MSBuild, `src/ArcGISPro.DotNetBuild.targets` replaces them
  so `dotnet build` succeeds without packaging.
- **`win-x64` only.** Pro is x64, under emulation on Arm64 Windows. Without
  a RID, SkiaSharp's natives for every platform are packaged and paths pass
  MAX_PATH. If a deep clone still hits the limit, build from a `subst`
  drive.
- **Dependencies are copied local** (`CopyLocalLockFileAssemblies`) and
  packaged under `Install/`. Pro's own assemblies are reference-only: never
  ship them. A dependency Pro also loads (for example
  `Microsoft.Extensions.*`) resolves to whichever version loads first, so
  prefer versions no newer than Pro's.
- **Deployment.** Esri's targets run `RegisterAddIn.exe` by name from Pro's
  `bin` folder after packaging. That fails silently when Pro isn't
  installed, and when `NoDefaultCurrentDirectoryInExePath` is set. The
  deploy script copies the package to
  `<Documents>\ArcGIS\AddIns\ArcGISPro\{AddInInfo id}\`, resolving a
  redirected Documents folder.
- **CI** (`.github/workflows/ci.yml`) builds on `windows-latest` with
  `microsoft/setup-msbuild`, runs the tests with `dotnet test --no-build`,
  and uploads the `.esriAddinX` as the **addin** artifact. Pro isn't
  installed there.

## Checking the add-in in Pro

An agent can't click in Pro. To check a change:

1. Build with Visual Studio's MSBuild, run `tools/deploy-addin.ps1`, and
   start (or restart) Pro: `& "C:\Program Files\ArcGIS\Pro\bin\ArcGISPro.exe"`.
2. Read `%LOCALAPPDATA%\SoundCharts for ArcGIS\Logs\addin.log`. A new
   `SoundCharts loaded:` line means Pro created the module; it lists the
   add-in, EncDotNet.S100, Pro and .NET versions and the architecture.
3. If there's no line: check the package is in the add-in folder, that
   `AddInInfo` `desktopVersion` isn't newer than Pro, and Pro's add-in
   manager (**Project** > **Add-In Manager**) for a load error. Start Pro
   with `/enablediagnostics` for its diagnostic log.
4. Ask the user to check anything visible: the ribbon, windows, layers.

## Pitfalls

- **Namespaces with an `ArcGIS` segment** (such as
  `EncDotNet.ArcGIS.SoundCharts`) hide Pro's `ArcGIS.*` namespaces, and WPF's
  generated code doesn't use `global::`, so XAML with Pro types stops
  compiling. Code namespaces start `EncDotNet.SoundCharts`.
- **`defaultAssembly` and `defaultNamespace`** in `Config.daml` must match
  the assembly name and `RootNamespace`, or packaging warns and Pro can't
  find the classes.
- **Exceptions escaping into Pro** from a button or the module surface as
  Pro errors, or stop the module loading. Catch at the boundary and log with
  `AddInLog`.
- **"S-100" in the UI** collides with Pro's ENC support. Say SoundCharts.
- **File extensions**: don't register S-1xx formats with Pro's catalog.
