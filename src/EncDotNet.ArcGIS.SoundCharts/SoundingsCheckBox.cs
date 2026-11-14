using ArcGIS.Desktop.Framework.Contracts;
using EncDotNet.SoundCharts.Settings;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The <b>Soundings</b> quick toggle (<c>SoundCharts_SoundingsCheckBox</c>,
/// D5). The chart server can't hide soundings yet (U1), so it shows the
/// project's setting but stays disabled until the server supports it.
/// </summary>
internal sealed class SoundingsCheckBox : CheckBox
{
    /// <inheritdoc/>
    protected override void OnClick()
    {
        if (ChartServerSupport.Supports(MarinerSetting.Soundings))
        {
            SettingsHost.UpdateProject(settings => settings with { ShowSoundings = !settings.ShowSoundings });
        }
    }

    /// <inheritdoc/>
    protected override void OnUpdate()
    {
        Enabled = ChartServerSupport.Supports(MarinerSetting.Soundings);
        IsChecked = SoundChartsSession.Settings.Project.ShowSoundings;
    }
}
