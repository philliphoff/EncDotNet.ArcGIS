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
}
