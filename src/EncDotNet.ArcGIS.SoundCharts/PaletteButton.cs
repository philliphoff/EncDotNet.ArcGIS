using ArcGIS.Desktop.Framework.Contracts;
using EncDotNet.SoundCharts.Settings;

namespace EncDotNet.SoundCharts;

/// <summary>
/// A palette on the <b>Palette</b> split button (design brief §2.1, D5):
/// checked while the layers show it. Choosing it sets the project's palette
/// by hand, which turns Follow Pro theme off (D26). A palette change is a
/// WMTS style change, never a server restart (D27). Its large image is the
/// active palette's, so the split button's face shows it (<see cref="PaletteFace"/>).
/// </summary>
internal abstract class PaletteButton : Button
{
    private bool _registered;
    private object? _ownImage;

    /// <summary>Gets the palette this button chooses.</summary>
    protected abstract ChartPalette Palette { get; }

    /// <inheritdoc/>
    protected override void OnClick() => SettingsHost.ChoosePalette(Palette);

    /// <inheritdoc/>
    protected override void OnUpdate()
    {
        if (!_registered)
        {
            _registered = true;
            _ownImage = LargeImage;
            PaletteFace.Register(Palette, _ownImage);
        }

        IsChecked = SoundChartsSession.Settings.Effective.Palette == Palette;
        var face = PaletteFace.Active(_ownImage);
        if (!ReferenceEquals(LargeImage, face))
        {
            LargeImage = face;
        }
    }
}
