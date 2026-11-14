using ArcGIS.Desktop.Framework.Contracts;
using EncDotNet.SoundCharts.Settings;

namespace EncDotNet.SoundCharts;

/// <summary>
/// A palette on the <b>Palette</b> split button (design brief §2.1, D5):
/// checked while the layers show it. Choosing it sets the project's palette
/// by hand, which turns Follow Pro theme off (D26). A palette change is a
/// WMTS style change, never a server restart (D27).
/// </summary>
internal abstract class PaletteButton : Button
{
    /// <summary>Gets the palette this button chooses.</summary>
    protected abstract ChartPalette Palette { get; }

    /// <inheritdoc/>
    protected override void OnClick() => SettingsHost.ChoosePalette(Palette);

    /// <inheritdoc/>
    protected override void OnUpdate() => IsChecked = SoundChartsSession.Settings.Effective.Palette == Palette;
}
