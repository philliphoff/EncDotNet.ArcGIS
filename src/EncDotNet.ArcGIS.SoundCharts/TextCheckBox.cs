using ArcGIS.Desktop.Framework.Contracts;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The <b>Text</b> quick toggle (<c>SoundCharts_TextCheckBox</c>, D5): the
/// project's Text setting. The chart server takes it as <c>--no-text</c>,
/// so a change starts new servers and repoints the layers.
/// </summary>
internal sealed class TextCheckBox : CheckBox
{
    /// <inheritdoc/>
    protected override void OnClick() =>
        SettingsHost.UpdateProject(settings => settings with { ShowText = !settings.ShowText });

    /// <inheritdoc/>
    protected override void OnUpdate() => IsChecked = SoundChartsSession.Settings.Project.ShowText;
}
