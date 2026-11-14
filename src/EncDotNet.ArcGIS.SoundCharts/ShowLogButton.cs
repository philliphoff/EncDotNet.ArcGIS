using ArcGIS.Desktop.Framework.Contracts;
using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The <b>Log</b> button (<c>SoundCharts_LogButton</c> in Config.daml): opens
/// the SoundCharts Log pane.
/// </summary>
internal sealed class ShowLogButton : Button
{
    protected override void OnClick()
    {
        try
        {
            LogPaneViewModel.Show();
        }
        catch (Exception ex)
        {
            // The boundary with Pro: log rather than let the exception reach Pro.
            AddInLog.Write(LogLevel.Error, $"Opening the SoundCharts Log failed: {ex}");
        }
    }
}
