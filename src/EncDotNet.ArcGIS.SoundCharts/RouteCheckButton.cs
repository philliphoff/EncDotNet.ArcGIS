using ArcGIS.Desktop.Framework.Contracts;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>Route check</b> (<c>SoundCharts_RouteCheckButton</c> in Config.daml):
/// opens the <b>SoundCharts Route check</b> pane (design brief §2.1).
/// </summary>
internal sealed class RouteCheckButton : Button
{
    protected override void OnClick()
    {
        try
        {
            RouteCheckPaneViewModel.Open();
        }
        catch (Exception ex)
        {
            // The boundary with Pro: log rather than let the exception reach Pro.
            AddInLog.Write($"Opening the SoundCharts Route check pane failed: {ex}");
        }
    }
}
