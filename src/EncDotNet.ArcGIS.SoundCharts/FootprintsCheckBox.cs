using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>Footprints</b> on the SoundCharts Layer tab (<c>SoundCharts_Footprints</c>,
/// design brief §2.2): shows or hides the map's <b>Coverage</b> layer.
/// </summary>
internal sealed class FootprintsCheckBox : ArcGIS.Desktop.Framework.Contracts.CheckBox
{
    /// <summary>Follows the Coverage layer's visibility; disabled when the map has none.</summary>
    protected override void OnUpdate()
    {
        var layer = ChartLayerManager.CoverageLayerOf(MapView.Active);
        Enabled = layer is not null;
        IsChecked = layer?.IsVisible == true;
    }

    /// <summary>Toggles the Coverage layer's visibility.</summary>
    protected override async void OnClick()
    {
        try
        {
            if (ChartLayerManager.CoverageLayerOf(MapView.Active) is not { } layer)
            {
                return;
            }

            var visible = !layer.IsVisible;
            await QueuedTask.Run(() => layer.SetVisibility(visible));
            IsChecked = visible;
        }
        catch (Exception ex)
        {
            // The boundary with Pro: log rather than let the exception reach Pro.
            AddInLog.Write(LogLevel.Error, $"Showing or hiding the Coverage layer failed: {ex}");
        }
    }
}
