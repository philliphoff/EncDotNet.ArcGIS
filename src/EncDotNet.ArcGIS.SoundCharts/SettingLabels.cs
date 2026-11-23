using EncDotNet.SoundCharts.Resources;
using EncDotNet.SoundCharts.Settings;

namespace EncDotNet.SoundCharts;

/// <summary>The localized labels of the settings' values, in the viewer's words (D25).</summary>
internal static class SettingLabels
{
    /// <summary>Gets the palettes in order, with their labels.</summary>
    public static IReadOnlyList<SettingChoice<ChartPalette>> Palettes { get; } =
    [
        new(ChartPalette.Day, Strings.Palette_Day),
        new(ChartPalette.Dusk, Strings.Palette_Dusk),
        new(ChartPalette.Night, Strings.Palette_Night),
    ];

    /// <summary>Gets the display categories in order, with their labels.</summary>
    public static IReadOnlyList<SettingChoice<DisplayCategory>> DisplayCategories { get; } =
    [
        new(DisplayCategory.DisplayBase, Strings.DisplayCategory_DisplayBase),
        new(DisplayCategory.Standard, Strings.DisplayCategory_Standard),
        new(DisplayCategory.OtherInformation, Strings.DisplayCategory_OtherInformation),
        new(DisplayCategory.All, Strings.DisplayCategory_All),
    ];

    /// <summary>Gets the time formats, with their labels.</summary>
    public static IReadOnlyList<SettingChoice<TimeFormat>> TimeFormats { get; } =
    [
        new(TimeFormat.Utc, Strings.TimeFormat_Utc),
        new(TimeFormat.Local, Strings.TimeFormat_Local),
    ];

    /// <summary>Gets the depth units, with their labels.</summary>
    public static IReadOnlyList<SettingChoice<DepthUnit>> DepthUnits { get; } =
    [
        new(DepthUnit.Metres, Strings.DepthUnit_Metres),
        new(DepthUnit.Feet, Strings.DepthUnit_Feet),
        new(DepthUnit.FathomsFeet, Strings.DepthUnit_FathomsFeet),
        new(DepthUnit.Fathoms, Strings.DepthUnit_Fathoms),
    ];

    /// <summary>Gets the message for <paramref name="problem"/>.</summary>
    /// <param name="problem">The problem.</param>
    public static string Message(MarinerSettingsProblem problem) => problem switch
    {
        MarinerSettingsProblem.DepthOutOfRange => Strings.Settings_Problem_DepthOutOfRange,
        MarinerSettingsProblem.ContoursOutOfOrder => Strings.Settings_Problem_ContoursOutOfOrder,
        MarinerSettingsProblem.ScaleOutOfRange => Strings.Settings_Problem_ScaleOutOfRange,
        MarinerSettingsProblem.NationalLanguageInvalid => Strings.Settings_Problem_NationalLanguageInvalid,
        _ => Strings.Settings_Problem_UnknownValue,
    };

    /// <summary>
    /// Gets the messages for the problems in the Options page's Display
    /// section (the scales, the national language and unknown values), one a
    /// line, or <see langword="null"/>.
    /// </summary>
    /// <param name="settings">The settings to check.</param>
    public static string? DisplayProblems(MarinerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return Join(settings.Validate().Where(p => p is MarinerSettingsProblem.ScaleOutOfRange
            or MarinerSettingsProblem.NationalLanguageInvalid
            or MarinerSettingsProblem.UnknownValue));
    }

    /// <summary>
    /// Gets the messages for the problems in the Options page's Depths
    /// section (the contours and the safety depth), one a line, or
    /// <see langword="null"/>.
    /// </summary>
    /// <param name="settings">The settings to check.</param>
    public static string? DepthProblems(MarinerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        // Validate reports a bad draft or margin as a depth out of range, so
        // leave them out: the Route check section shows its own problem.
        return Join((settings with { Draft = null, UkcMargin = null }).Validate()
            .Where(p => p is MarinerSettingsProblem.DepthOutOfRange or MarinerSettingsProblem.ContoursOutOfOrder));
    }

    /// <summary>
    /// Gets the message for a problem in the Options page's Route check
    /// section (the draft and the UKC margin), or <see langword="null"/>.
    /// </summary>
    /// <param name="settings">The settings to check.</param>
    public static string? RouteCheckProblems(MarinerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        // Checked against the defaults, which are valid, so only the draft and margin count.
        var routeCheck = MarinerSettings.Default with { Draft = settings.Draft, UkcMargin = settings.UkcMargin };
        return routeCheck.Validate().Count == 0 ? null : Strings.Settings_Problem_RouteCheckOutOfRange;
    }

    private static string? Join(IEnumerable<MarinerSettingsProblem> problems)
    {
        var messages = problems.Select(Message).ToList();
        return messages.Count == 0 ? null : string.Join(Environment.NewLine, messages);
    }
}
