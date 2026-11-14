# Third-party notices

The SoundCharts for ArcGIS add-in package (`.esriAddinX`) redistributes the
libraries listed below, unmodified, as NuGet dependencies of the add-in.
Each is used under the terms of its license, which the linked project
publishes.

---

## EncDotNet.S100 and EncDotNet (libraries) — MIT License

- **Packages:** `EncDotNet.S100` and the `EncDotNet.S100.*` packages it
  depends on; `EncDotNet.Iso8211` and `EncDotNet.S57`.
- **Copyright:** Copyright (c) 2026 Phillip Hoff.
- **Source:** https://github.com/philliphoff/EncDotNet.S100 and
  https://github.com/philliphoff/EncDotNet.
- **Notices:** EncDotNet.S100 redistributes third-party assets of its own,
  including the Open Sans and Droid Sans fonts (Apache License 2.0) and
  Natural Earth land data (public domain). Each EncDotNet.S100 package
  carries its `THIRD-PARTY-NOTICES.md` with their attributions and license
  texts; see
  [EncDotNet.S100's third-party notices](https://github.com/philliphoff/EncDotNet.S100/blob/main/THIRD-PARTY-NOTICES.md).

---

## Other libraries

| Package | License | Source |
|---|---|---|
| `SkiaSharp`, `SkiaSharp.NativeAssets.Win32` | MIT | https://github.com/mono/SkiaSharp |
| `Svg.Skia`, `Svg.Model`, `ShimSkiaSharp` | MIT | https://github.com/wieslawsoltes/Svg.Skia |
| `Svg.Custom` | MS-PL | https://github.com/wieslawsoltes/Svg.Skia |
| `ExCSS` | MIT | https://github.com/TylerBrinks/ExCSS |
| `MoonSharp` | BSD-3-Clause | https://github.com/moonsharp-devs/moonsharp |
| `NetTopologySuite` | BSD-3-Clause | https://github.com/NetTopologySuite/NetTopologySuite |
| `ProjNET` | LGPL-2.1-or-later | https://github.com/NetTopologySuite/ProjNet4GeoAPI |
| `PureHDF` | MIT | https://github.com/Apollo3zehn/PureHDF |
| `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions` | MIT | https://github.com/dotnet/runtime |

`ProjNET` is redistributed as its unmodified, separately loaded assembly
(`ProjNET.dll`), so you can replace it with another build of the library.

---

## ArcGIS Pro SDK (not redistributed)

The add-in compiles against the ArcGIS Pro API reference assemblies in the
`Esri.ArcGISPro.Extensions30` NuGet package, under Esri's license
agreement. Those assemblies aren't included in the add-in package: ArcGIS
Pro provides them at runtime.
