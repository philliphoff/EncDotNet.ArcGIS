using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>Log</b> on the SoundCharts Layer tab (<c>SoundCharts_ChartLog</c>):
/// opens the SoundCharts Log filtered to the selected layer's chart server,
/// or unfiltered when the selection spans several servers (design brief §2.2).
/// </summary>
internal sealed class ChartLogButton : Button
{
    protected override void OnClick()
    {
        try
        {
            var servers = ChartLayerManager.Instance.SelectedServers(MapView.Active);
            LogPaneViewModel.Show(servers.Count == 1 ? servers[0].LogSource : null);
        }
        catch (Exception ex)
        {
            // The boundary with Pro: log rather than let the exception reach Pro.
            AddInLog.Write(LogLevel.Error, $"Opening the chart's log failed: {ex}");
        }
    }
}
