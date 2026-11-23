using ArcGIS.Desktop.Framework.Contracts;
using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The <b>Library</b> button (<c>SoundCharts_LibraryButton</c> in
/// Config.daml, design brief §2.1): opens the SoundCharts Library pane.
/// </summary>
internal sealed class LibraryButton : Button
{
    protected override void OnClick()
    {
        try
        {
            LibraryPaneViewModel.Show();
        }
        catch (Exception ex)
        {
            // The boundary with Pro: log rather than let the exception reach Pro.
            AddInLog.Write(LogLevel.Error, $"Opening the SoundCharts Library failed: {ex}");
        }
    }
}
