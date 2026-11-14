using EncDotNet.SoundCharts.Settings;

namespace EncDotNet.SoundCharts;

/// <summary>The <b>Night</b> palette (<c>SoundCharts_PaletteNight</c>), only ever chosen by hand (D26).</summary>
internal sealed class PaletteNightButton : PaletteButton
{
    /// <inheritdoc/>
    protected override ChartPalette Palette => ChartPalette.Night;
}
