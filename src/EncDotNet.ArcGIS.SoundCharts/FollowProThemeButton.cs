using ArcGIS.Desktop.Framework.Contracts;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>Follow Pro theme</b> on the <b>Palette</b> split button
/// (<c>SoundCharts_PaletteFollowProTheme</c>, D26): an application setting,
/// checked while it's on.
/// </summary>
internal sealed class FollowProThemeButton : Button
{
    private bool _captured;
    private object? _ownImage;

    /// <inheritdoc/>
    protected override void OnClick() => SettingsHost.SetFollowProTheme(!SoundChartsSession.Settings.Application.FollowProTheme);

    /// <inheritdoc/>
    protected override void OnUpdate()
    {
        if (!_captured)
        {
            _captured = true;
            _ownImage = LargeImage;
        }

        IsChecked = SoundChartsSession.Settings.Application.FollowProTheme;

        // The split button's face shows the palette in use (PaletteFace).
        var face = PaletteFace.Active(_ownImage);
        if (!ReferenceEquals(LargeImage, face))
        {
            LargeImage = face;
        }
    }
}
