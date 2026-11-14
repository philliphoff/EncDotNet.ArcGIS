namespace EncDotNet.SoundCharts.Settings;

/// <summary>
/// Which mariner settings the bundled chart server applies (design brief
/// §1.3, §5.2).
/// </summary>
/// <remarks>
/// <c>s100 tiles serve</c> takes the palette as the WMTS style, and text,
/// symbol and text scale and national language as start-up options. It has
/// no display category, contours, depth shades, soundings or S-101 portrayal
/// switches (upstream U1), so the add-in keeps those in its settings and
/// its UI, marked as needing a newer chart server, and doesn't hash them
/// into the render key: they don't change a tile.
/// </remarks>
public static class ChartServerSupport
{
    private static readonly HashSet<MarinerSetting> Supported =
    [
        MarinerSetting.Palette,
        MarinerSetting.Text,
        MarinerSetting.SymbolScale,
        MarinerSetting.TextScale,
        MarinerSetting.NationalLanguage,
    ];

    /// <summary>
    /// Gets whether the server takes the render key as a WMTS style suffix,
    /// <c>{palette}-{key}</c> (U1). It doesn't yet: it answers an unknown
    /// style with an error, so the key goes in the server token instead (D27).
    /// </summary>
    public static bool SettingsStyles => false;

    /// <summary>Gets whether the chart server applies <paramref name="setting"/>.</summary>
    /// <param name="setting">The setting.</param>
    public static bool Supports(MarinerSetting setting) => Supported.Contains(setting);
}
