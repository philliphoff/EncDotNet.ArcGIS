using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.OpenWith;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>Open with SoundCharts</b> &gt; <b>Add as Feature Layers</b>
/// (<c>SoundCharts_OpenWithAddFeatureLayers</c> in Config.daml): adds the
/// right-clicked Catalog items' features to the active map as feature
/// layers, as <b>Add as Feature Layers</b> does (issue #15).
/// </summary>
internal sealed class OpenWithAddFeatureLayersButton : Button
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

            AddInLog.Write($"Open with SoundCharts: adding {string.Join(", ", paths)} as feature layers.");
            await AddSoundChartsFeatures.RunAsync(view, paths);
        }
        catch (Exception ex)
        {
            // The boundary with Pro: log rather than let the exception reach Pro.
            AddInLog.Write(LogLevel.Error, $"Open with SoundCharts > Add as Feature Layers failed: {ex}");
        }
    }
}
