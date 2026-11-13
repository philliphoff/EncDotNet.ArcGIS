---
applyTo: "src/EncDotNet.ArcGIS.SoundCharts/**,tests/EncDotNet.ArcGIS.SoundCharts.Tests/**,src/*.targets,src/Directory.Build.*"
---

# Add-in editing rules

When modifying the add-in, load the `arcgis-pro-addin` skill for anything
beyond a small fix. In particular:

## Names and ids

- Brand everything **SoundCharts**: DAML ids (`SoundCharts_<Thing>`),
  captions, tooltips, pane titles, toolbox and tool names. Never "S-100",
  which Pro's ENC support uses.
- Code namespaces start `EncDotNet.SoundCharts`; never put an `ArcGIS`
  segment in a namespace.
- Don't rename a released DAML id: projects, customizations and other
  add-ins may refer to it.

## DAML

- Every control in `Config.daml` has a `caption`, a `tooltip` with a
  `heading`, an image (Pro's built-in images, such as
  `GenericInformation32`, or one in `Images/` with `BuildAction` Content),
  and a `keytip` when it's on a tab.
- `className` resolves against `defaultNamespace`; keep the classes Pro
  creates `internal sealed`.
- Keep the `AddInInfo` `id` unchanged: it is the install folder name, and
  `tools/deploy-addin.ps1` reads it.
- Don't register file extensions or catalog item types for S-1xx formats.

## Threading

- Pro API calls on maps, layers, geometry, the geodatabase and the CIM run
  on the Main CIM Thread inside `QueuedTask.Run`; await it. Don't block the UI
  thread with `.Result` or `.Wait()`.
- Update WPF-bound properties from the UI thread, or raise changes from
  view models that marshal to it.

## UI text and accessibility

- Window and pane text lives in `Resources/Strings.resx`, with a matching
  documented property in `Resources/Strings.cs`. XAML uses
  `{x:Static res:Strings.Key}`; code uses `Strings.Key`, with
  `string.Format(CultureInfo.CurrentCulture, …)` for parameters.
  `StringsTests` checks every property has a resource.
- Group keys with stable prefixes: `About_*`, `Button_*`, `Tooltip_*`,
  `Pane_*`, `Settings_*`, `Status_*`.
- Every interactive control gets `AutomationProperties.AutomationId`
  (`<View>.<Element>`, English, never localized) and a short localized
  accessible name; every button gets a tooltip.
- Use Pro's styles and `DynamicResource` brushes so windows follow Pro's
  themes.

## Process and failure

- The add-in runs inside Pro. Catch and log failures at the boundary (button
  handlers, module start-up, server callbacks) with `AddInLog`; never let an
  exception escape into Pro.
- Long-running or crash-prone work (rendering, the chart server) runs out
  of process.

## Tests

- Put logic that can be tested without Pro in classes that don't touch the
  Pro API, and test it in `tests/EncDotNet.ArcGIS.SoundCharts.Tests`.
- The test project builds for `win-x64` like the add-in; the Pro
  assemblies are reference-only there.
