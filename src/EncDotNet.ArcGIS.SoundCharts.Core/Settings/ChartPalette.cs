namespace EncDotNet.SoundCharts.Settings;

/// <summary>
/// The S-100 colour palettes the chart server draws, each a WMTS style
/// (design brief §5.2). The labels are the viewer's: <b>Day</b>,
/// <b>Dusk</b>, <b>Night</b>.
/// </summary>
public enum ChartPalette
{
    /// <summary>Day: the palette for print and daylight.</summary>
    Day,

    /// <summary>Dusk: the palette for a dark screen.</summary>
    Dusk,

    /// <summary>Night: the palette for a dark bridge, only ever chosen by hand (D26).</summary>
    Night,
}
