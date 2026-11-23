using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The <b>About SoundCharts</b> button (<c>SoundCharts_AboutButton</c> in
/// Config.daml).
/// </summary>
internal sealed class AboutButton : Button
{
    protected override void OnClick()
    {
        try
        {
            var window = new AboutWindow(AboutInfo.Collect())
            {
                Owner = FrameworkApplication.Current.MainWindow,
            };
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            // The boundary with Pro: log rather than let the exception reach Pro.
            AddInLog.Write($"About SoundCharts failed: {ex}");
        }
    }
}
