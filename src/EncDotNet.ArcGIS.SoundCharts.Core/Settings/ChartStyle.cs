using EncDotNet.SoundCharts.Charts;

namespace EncDotNet.SoundCharts.Settings;

/// <summary>
/// The WMTS style of a SoundCharts layer: the style path segment of its
/// tile URLs (design brief §5.4, D27).
/// </summary>
public static class ChartStyle
{
    /// <summary>
    /// Gets the WMTS style for <paramref name="palette"/>: the palette's
    /// name (<c>day</c>, <c>dusk</c>, <c>night</c>), and, once the server
    /// takes settings styles (U1), <c>{palette}-{renderKey}</c>.
    /// </summary>
    /// <param name="palette">The palette.</param>
    /// <param name="renderKey">The render key, or <see langword="null"/>.</param>
    /// <param name="settingsStyles">Whether the server takes <c>{palette}-{key}</c> styles; <see cref="ChartServerSupport.SettingsStyles"/> by default.</param>
    /// <exception cref="ArgumentException"><paramref name="renderKey"/> isn't a render key.</exception>
    public static string For(ChartPalette palette, string? renderKey = null, bool? settingsStyles = null)
    {
        var name = Name(palette);
        if (renderKey is null || !(settingsStyles ?? ChartServerSupport.SettingsStyles))
        {
            return name;
        }

        return RenderKey.IsValid(renderKey)
            ? $"{name}-{renderKey}"
            : throw new ArgumentException($"'{renderKey}' isn't a render key.", nameof(renderKey));
    }

    /// <summary>Gets the palette's name as the server's style: <c>day</c>, <c>dusk</c> or <c>night</c>.</summary>
    /// <param name="palette">The palette.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="palette"/> isn't a palette.</exception>
    public static string Name(ChartPalette palette) => palette switch
    {
        ChartPalette.Day => "day",
        ChartPalette.Dusk => "dusk",
        ChartPalette.Night => "night",
        _ => throw new ArgumentOutOfRangeException(nameof(palette), palette, "Unknown palette."),
    };

    /// <summary>
    /// Reads the palette of a style, <c>day</c> or <c>night-3f9a1c02e47b5d18</c>;
    /// <see langword="null"/> when it isn't a palette style.
    /// </summary>
    /// <param name="style">The style.</param>
    public static ChartPalette? PaletteOf(string? style)
    {
        if (string.IsNullOrWhiteSpace(style))
        {
            return null;
        }

        var dash = style.IndexOf('-', StringComparison.Ordinal);
        var name = dash < 0 ? style : style[..dash];
        return name.ToLowerInvariant() switch
        {
            "day" => ChartPalette.Day,
            "dusk" => ChartPalette.Dusk,
            "night" => ChartPalette.Night,
            _ => null,
        };
    }
}
