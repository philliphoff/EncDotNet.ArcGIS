using System.Windows;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Resources;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>Zoom to Data</b> on the SoundCharts Layer tab
/// (<c>SoundCharts_ZoomToData</c>): zooms to the footprints of the selected
/// layers' own datasets (design brief §2.2), not the bounds the chart
/// server publishes, which are the union of all its datasets.
/// </summary>
internal sealed class ZoomToDataButton : Button
{
    protected override async void OnClick()
    {
        try
        {
            if (MapView.Active is not { } view)
            {
                return;
            }

            var layers = ChartLayerManager.Instance.SelectedChartLayers(view);
            if (!await ChartLayerManager.ZoomToDataAsync(view, layers))
            {
                MessageBox.Show(Strings.ZoomToData_NoExtent, Strings.Notification_Title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            // The boundary with Pro: log rather than let the exception reach Pro.
            AddInLog.Write(LogLevel.Error, $"Zoom to Data failed: {ex}");
        }
    }
}
