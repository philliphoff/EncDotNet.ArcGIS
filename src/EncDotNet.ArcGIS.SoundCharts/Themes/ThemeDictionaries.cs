namespace EncDotNet.SoundCharts.Themes;

/// <summary>
/// Where the theme's resource dictionaries are, and which colour dictionary a
/// theme uses. Free of the Pro and WPF APIs, so it is testable without Pro.
/// </summary>
public static class ThemeDictionaries
{
    /// <summary>The add-in assembly's name, which the component URIs name.</summary>
    public const string AssemblyName = "EncDotNet.ArcGIS.SoundCharts";

    /// <summary>The shared styles, sizes and glyphs: <c>Themes/SoundCharts.xaml</c>.</summary>
    public static Uri SharedUri { get; } = Component("Themes/SoundCharts.xaml");

    /// <summary>Chooses the colour dictionary for Pro's theme.</summary>
    /// <param name="proTheme">The variant that matches Pro's <c>ApplicationTheme</c>.</param>
    /// <param name="systemHighContrast">Whether Windows has a contrast theme on.</param>
    /// <returns>
    /// <see cref="ThemeVariant.HighContrast"/> when Windows has a contrast
    /// theme on, whatever Pro reports, so the system colours always win;
    /// otherwise <paramref name="proTheme"/>.
    /// </returns>
    public static ThemeVariant Choose(ThemeVariant proTheme, bool systemHighContrast) =>
        systemHighContrast ? ThemeVariant.HighContrast : proTheme;

    /// <summary>The colour dictionary for a theme variant.</summary>
    /// <param name="variant">The theme variant.</param>
    /// <returns>A relative component URI, which WPF resolves against the application.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="variant"/> isn't a defined variant.</exception>
    public static Uri ColorsUri(ThemeVariant variant) => variant switch
    {
        ThemeVariant.Light => Component("Themes/SoundCharts.Colors.Light.xaml"),
        ThemeVariant.Dark => Component("Themes/SoundCharts.Colors.Dark.xaml"),
        ThemeVariant.HighContrast => Component("Themes/SoundCharts.Colors.HighContrast.xaml"),
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, $"{variant} isn't a theme variant."),
    };

    // A relative pack URI ("/Assembly;component/Path"): a ResourceDictionary
    // resolves it against pack://application:,,,/ when it loads. A relative
    // URI also constructs where the pack scheme isn't registered (tests).
    private static Uri Component(string path) => new($"/{AssemblyName};component/{path}", UriKind.Relative);
}
