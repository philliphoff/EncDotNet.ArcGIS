using EncDotNet.SoundCharts.Charts;

namespace EncDotNet.SoundCharts.Settings;

/// <summary>
/// A project's palette and mariner settings, shared by every SoundCharts
/// layer in it (design brief §5.1, D23).
/// </summary>
/// <remarks>
/// <para>
/// The defaults are the SoundCharts viewer's (D25): EncDotNet.S100's
/// <c>MarinerSettings</c> and <c>EcdisDisplaySettings</c>, and the viewer's
/// symbol and text scales. Depths are in metres whatever
/// <see cref="ApplicationSettings.DepthUnit"/> shows.
/// </para>
/// <para>
/// Only some settings reach the chart server today
/// (<see cref="ChartServerSupport"/>); <see cref="ToRenderOptions"/> gives
/// those. The rest are kept, so a project keeps its values for when the
/// server takes them.
/// </para>
/// </remarks>
public sealed record MarinerSettings
{
    /// <summary>The smallest symbol or text scale, as the viewer's slider.</summary>
    public const double MinScale = 0.5;

    /// <summary>The largest symbol or text scale, as the viewer's slider.</summary>
    public const double MaxScale = 3.0;

    /// <summary>The deepest contour or depth the settings take, in metres.</summary>
    public const double MaxDepth = 12000;

    /// <summary>Gets the viewer's defaults.</summary>
    public static MarinerSettings Default { get; } = new();

    /// <summary>Gets the palette; <b>Day</b> by default.</summary>
    public ChartPalette Palette { get; init; } = ChartPalette.Day;

    /// <summary>Gets the display category; <b>Standard</b> by default.</summary>
    public DisplayCategory DisplayCategory { get; init; } = DisplayCategory.Standard;

    /// <summary>Gets the safety contour in metres; 30 by default.</summary>
    public double SafetyContour { get; init; } = 30.0;

    /// <summary>Gets the safety depth in metres; 30 by default.</summary>
    public double SafetyDepth { get; init; } = 30.0;

    /// <summary>Gets the shallow contour in metres; 2 by default.</summary>
    public double ShallowContour { get; init; } = 2.0;

    /// <summary>Gets the deep contour in metres; 30 by default.</summary>
    public double DeepContour { get; init; } = 30.0;

    /// <summary>Gets whether depth areas use four shades; off by default.</summary>
    public bool FourShades { get; init; }

    /// <summary>Gets whether isolated dangers in shallow water are emphasised; on by default.</summary>
    public bool ShallowWaterDangers { get; init; } = true;

    /// <summary>Gets whether area boundaries are plain; on by default.</summary>
    public bool PlainBoundaries { get; init; } = true;

    /// <summary>Gets whether point symbols are simplified; off by default.</summary>
    public bool SimplifiedSymbols { get; init; }

    /// <summary>Gets whether light sectors are drawn to their nominal range; off by default.</summary>
    public bool FullLightLines { get; init; }

    /// <summary>Gets whether features are drawn whatever their scale minimum; off by default.</summary>
    public bool IgnoreScaleMinimum { get; init; }

    /// <summary>Gets whether soundings are drawn; on by default.</summary>
    public bool ShowSoundings { get; init; } = true;

    /// <summary>Gets whether text is drawn; on by default.</summary>
    public bool ShowText { get; init; } = true;

    /// <summary>Gets the symbol scale factor; 1 by default.</summary>
    public double SymbolScale { get; init; } = 1.0;

    /// <summary>Gets the text scale factor; 1 by default.</summary>
    public double TextScale { get; init; } = 1.0;

    /// <summary>
    /// Gets the ISO 639-2/B code preferred for chart text, or an empty
    /// string for the catalogue's.
    /// </summary>
    public string NationalLanguage { get; init; } = string.Empty;

    /// <summary>
    /// Gets the <c>s100 tiles serve</c> start-up options these settings
    /// give: the ones the server applies (<see cref="ChartServerSupport"/>).
    /// </summary>
    /// <param name="baseline">Options the settings don't decide, such as the S-411 display mode; <see cref="ChartRenderOptions.Default"/> when <see langword="null"/>.</param>
    public ChartRenderOptions ToRenderOptions(ChartRenderOptions? baseline = null) => (baseline ?? ChartRenderOptions.Default) with
    {
        SymbolScale = SymbolScale,
        TextScale = TextScale,
        ShowText = ShowText,
        NationalLanguage = string.IsNullOrWhiteSpace(NationalLanguage) ? null : NationalLanguage.Trim().ToLowerInvariant(),
    };

    /// <summary>
    /// Gets what's wrong with these settings, or an empty list. The viewer's
    /// rules: depths from 0 to <see cref="MaxDepth"/> metres, the shallow
    /// contour no deeper than the safety contour and the deep contour no
    /// shallower, scales from <see cref="MinScale"/> to <see cref="MaxScale"/>,
    /// and a national language of three letters or none.
    /// </summary>
    public IReadOnlyList<MarinerSettingsProblem> Validate()
    {
        List<MarinerSettingsProblem> problems = [];
        if (!IsDepth(SafetyContour) || !IsDepth(SafetyDepth) || !IsDepth(ShallowContour) || !IsDepth(DeepContour))
        {
            problems.Add(MarinerSettingsProblem.DepthOutOfRange);
        }
        else if (ShallowContour > SafetyContour || DeepContour < SafetyContour)
        {
            problems.Add(MarinerSettingsProblem.ContoursOutOfOrder);
        }

        if (!IsScale(SymbolScale) || !IsScale(TextScale))
        {
            problems.Add(MarinerSettingsProblem.ScaleOutOfRange);
        }

        var language = NationalLanguage.Trim();
        if (language.Length > 0 && (language.Length != 3 || !language.All(char.IsAsciiLetter)))
        {
            problems.Add(MarinerSettingsProblem.NationalLanguageInvalid);
        }

        if (!Enum.IsDefined(Palette) || !Enum.IsDefined(DisplayCategory))
        {
            problems.Add(MarinerSettingsProblem.UnknownValue);
        }

        return problems;
    }

    private static bool IsDepth(double metres) => double.IsFinite(metres) && metres is >= 0 and <= MaxDepth;

    private static bool IsScale(double factor) => double.IsFinite(factor) && factor is >= MinScale and <= MaxScale;
}
