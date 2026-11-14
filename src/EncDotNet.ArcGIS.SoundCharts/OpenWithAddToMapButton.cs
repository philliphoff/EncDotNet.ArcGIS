using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.OpenWith;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>Open with SoundCharts</b> &gt; <b>Add to Map</b>
/// (<c>SoundCharts_OpenWithAddToMap</c> in Config.daml): adds the
/// right-clicked Catalog items to the active map's SoundCharts chart,
/// through the same path as <b>Add SoundCharts Data</b> (D11).
/// </summary>
internal sealed class OpenWithAddToMapButton : Button
{
    protected override async void OnClick()
    {
        try
        {
            var paths = OpenWithTargets.Paths;
            if (paths.Count == 0 || MapView.Active is not { } view)
            {
                return;
            }

            AddInLog.Write($"Open with SoundCharts: adding {string.Join(", ", paths)} to the map.");
            await AddSoundChartsData.RunAsync(view, paths);
        }
        catch (Exception ex)
        {
            // The boundary with Pro: log rather than let the exception reach Pro.
            AddInLog.Write(LogLevel.Error, $"Open with SoundCharts > Add to Map failed: {ex}");
        }
    }
}
