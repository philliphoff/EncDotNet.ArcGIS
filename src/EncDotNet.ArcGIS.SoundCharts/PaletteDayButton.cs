using EncDotNet.SoundCharts.Settings;

namespace EncDotNet.SoundCharts;

/// <summary>The <b>Day</b> palette (<c>SoundCharts_PaletteDay</c>).</summary>
internal sealed class PaletteDayButton : PaletteButton
{
    /// <inheritdoc/>
    protected override ChartPalette Palette => ChartPalette.Day;
}
