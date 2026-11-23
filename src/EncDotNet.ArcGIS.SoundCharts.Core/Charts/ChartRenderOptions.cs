using System.Globalization;

namespace EncDotNet.SoundCharts.Charts;

/// <summary>
/// The <c>s100 tiles serve</c> start-up options that change a chart's tiles,
/// and so belong to its <see cref="DatasetSet"/>.
/// </summary>
/// <remarks>
/// The server takes display options only at start-up (design brief §1.3), so
/// changing one starts a new server. The palette isn't here: every palette
/// is served by the same server as a WMTS style. Slice A5 sets these from
/// the project's mariner settings.
/// </remarks>
/// <param name="SymbolScale">The symbol size factor (<c>--symbol-scale</c>); 1 is the portrayal's size.</param>
/// <param name="TextScale">The text size factor (<c>--text-scale</c>); 1 is the portrayal's size.</param>
/// <param name="ShowText">Whether text is drawn; <see langword="false"/> passes <c>--no-text</c>.</param>
/// <param name="NationalLanguage">The national language code (<c>--national-language</c>), or <see langword="null"/> for the catalogue's.</param>
/// <param name="DisplayMode">The S-411 display mode (<c>--display-mode</c>), or <see langword="null"/> for the server's default.</param>
public sealed record ChartRenderOptions(
    double SymbolScale = 1.0,
    double TextScale = 1.0,
    bool ShowText = true,
    string? NationalLanguage = null,
    string? DisplayMode = null)
{
    /// <summary>Gets the server's own defaults.</summary>
    public static ChartRenderOptions Default { get; } = new();

    /// <summary>
    /// Gets the options as <c>s100 tiles serve</c> arguments, leaving out
    /// those at the server's defaults.
    /// </summary>
    /// <exception cref="InvalidOperationException">A scale isn't a positive number.</exception>
    public IReadOnlyList<string> ToArguments()
    {
        Validate();

        List<string> arguments = [];
        if (SymbolScale != 1.0)
        {
            arguments.AddRange(["--symbol-scale", SymbolScale.ToString("R", CultureInfo.InvariantCulture)]);
        }

        if (TextScale != 1.0)
        {
            arguments.AddRange(["--text-scale", TextScale.ToString("R", CultureInfo.InvariantCulture)]);
        }

        if (!ShowText)
        {
            arguments.Add("--no-text");
        }

        if (!string.IsNullOrWhiteSpace(NationalLanguage))
        {
            arguments.AddRange(["--national-language", NationalLanguage]);
        }

        if (!string.IsNullOrWhiteSpace(DisplayMode))
        {
            arguments.AddRange(["--display-mode", DisplayMode]);
        }

        return arguments;
    }

    /// <summary>
    /// Gets the options as stable text, for <see cref="DatasetSet.Key"/>:
    /// fixed order, invariant culture, the same for equal options.
    /// </summary>
    public string ToCanonicalString() => string.Create(
        CultureInfo.InvariantCulture,
        $"symbol-scale={SymbolScale:R};text-scale={TextScale:R};text={(ShowText ? "on" : "off")};" +
        $"national-language={NationalLanguage?.Trim() ?? string.Empty};display-mode={DisplayMode?.Trim() ?? string.Empty}");

    private void Validate()
    {
        if (!double.IsFinite(SymbolScale) || SymbolScale <= 0)
        {
            throw new InvalidOperationException($"The symbol scale must be a positive number, not {SymbolScale}.");
        }

        if (!double.IsFinite(TextScale) || TextScale <= 0)
        {
            throw new InvalidOperationException($"The text scale must be a positive number, not {TextScale}.");
        }
    }
}
