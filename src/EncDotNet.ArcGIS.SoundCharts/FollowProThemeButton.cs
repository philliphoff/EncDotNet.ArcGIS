using ArcGIS.Desktop.Framework.Contracts;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>Follow Pro theme</b> on the <b>Palette</b> split button
/// (<c>SoundCharts_PaletteFollowProTheme</c>, D26): an application setting,
/// checked while it's on.
/// </summary>
internal sealed class FollowProThemeButton : Button
{
    /// <inheritdoc/>
    protected override void OnClick() => SettingsHost.SetFollowProTheme(!SoundChartsSession.Settings.Application.FollowProTheme);

    /// <inheritdoc/>
    protected override void OnUpdate() => IsChecked = SoundChartsSession.Settings.Application.FollowProTheme;
}
