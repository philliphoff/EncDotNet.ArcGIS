# Install SoundCharts for ArcGIS

Install a released build of SoundCharts for ArcGIS into ArcGIS Pro for the
current Windows user. To build and install it from source instead, see
[CONTRIBUTING.md](https://github.com/philliphoff/EncDotNet.ArcGIS/blob/main/CONTRIBUTING.md).

## Prerequisites

- ArcGIS Pro 3.7 or later, on Windows.

## Download a release

Each release on the
[releases page](https://github.com/philliphoff/EncDotNet.ArcGIS/releases)
has two packages, and a `SoundCharts-for-ArcGIS-<version>.zip` with both:

| File | What it is |
|---|---|
| `EncDotNet.ArcGIS.SoundCharts.esriAddinX` | The add-in: the **SoundCharts** tab, its panes, SoundCharts Tools and `s100` |
| `EncDotNet.ArcGIS.SoundCharts.Features.esriPlugin` | A plugin datasource that **Add as Feature Layers** reads datasets through |

Download both packages, or the `.zip` and extract it.

## Install the packages

1. Close ArcGIS Pro. Pro reads add-ins when it starts.
2. Double-click `EncDotNet.ArcGIS.SoundCharts.esriAddinX`, and choose
   **Install Add-In** in the ArcGIS Add-In Installation Utility.
3. Double-click `EncDotNet.ArcGIS.SoundCharts.Features.esriPlugin`, and
   install it the same way.
4. Start ArcGIS Pro and open a project.

The **SoundCharts** tab is on the ribbon. To check the installed version,
choose **About SoundCharts** on the **SoundCharts** tab.

> [!NOTE]
> The packages aren't digitally signed. If Pro is set to load only add-ins
> signed by a trusted publisher, it doesn't load SoundCharts.

### Install with PowerShell

From a clone of the repository,
[`tools/deploy-addin.ps1`](https://github.com/philliphoff/EncDotNet.ArcGIS/blob/main/tools/deploy-addin.ps1)
copies both packages to Pro's per-user add-in folders, where the
installation utility puts them:

```powershell
tools/deploy-addin.ps1 -Path path\to\EncDotNet.ArcGIS.SoundCharts.esriAddinX -PluginPath path\to\EncDotNet.ArcGIS.SoundCharts.Features.esriPlugin
```

## Update or remove the add-in

To update, close Pro and install the new release's packages. They replace
the installed ones.

To remove both packages, run this from a clone of the repository, with Pro
closed:

```powershell
tools/deploy-addin.ps1 -Remove
```
