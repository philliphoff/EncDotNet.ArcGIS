<!-- Thank you for contributing to EncDotNet.ArcGIS! -->

## Summary

<!-- What does this PR change and why? Link the issue it closes. -->

## Checked in ArcGIS Pro

<!-- What you did in Pro to check the change: the Pro version, the data, the
     steps, and what you saw. Write "N/A" for changes Pro can't show (build,
     CI, docs). -->

- [ ] Installed the add-in (`tools/deploy-addin.ps1`) and restarted Pro
- [ ] Checked the changed behaviour in Pro
- [ ] N/A — change is purely infrastructural (build, CI, docs, tooling)

## Add-in conventions

- [ ] Consulted the `arcgis-pro-addin` skill for add-in, DAML or Pro SDK changes
- [ ] Ribbon, pane and tool ids and captions say "SoundCharts", never "S-100"
- [ ] New UI text is in `Resources/Strings.resx` or `Config.daml`, and new
      controls have automation ids and accessible names
- [ ] Pro API calls that need the Main CIM Thread run in `QueuedTask.Run`

## Tests

- [ ] Added/updated xunit tests under `tests/`
- [ ] Tests requiring real data files skip via `Assert.SkipUnless`/`Assert.SkipWhen`
- [ ] `msbuild EncDotNet.ArcGIS.slnx /restore` and `dotnet test --configuration Release` pass locally

## Documentation

- [ ] Updated `README.md`, `CONTRIBUTING.md` or `docs/` if user-facing or
      contributor-facing behaviour changed
- [ ] New public APIs have XML doc comments

## Dependencies

- [ ] No new NuGet dependencies, OR versions added to
      `Directory.Packages.props` (not in the `.csproj`)
- [ ] `gh-advisory-database` security check run for any new dependency
- [ ] A new dependency doesn't clash with an assembly Pro loads

## Breaking changes

<!-- List any breaking changes (saved projects and layers, settings, DAML
     ids that other add-ins or configurations may refer to), or write "None". -->
