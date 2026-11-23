using System.Collections;
using System.Resources;
using EncDotNet.SoundCharts.RouteCheck;
using EncDotNet.SoundCharts.Themes;

namespace EncDotNet.SoundCharts.Tests.Themes;

public class ThemeTests
{
    [Theory]
    [InlineData(ThemeVariant.Light, false, ThemeVariant.Light)]
    [InlineData(ThemeVariant.Dark, false, ThemeVariant.Dark)]
    [InlineData(ThemeVariant.HighContrast, false, ThemeVariant.HighContrast)]
    [InlineData(ThemeVariant.Light, true, ThemeVariant.HighContrast)]
    [InlineData(ThemeVariant.Dark, true, ThemeVariant.HighContrast)]
    public void Choose_PrefersWindowsHighContrast(ThemeVariant proTheme, bool systemHighContrast, ThemeVariant expected)
    {
        Assert.Equal(expected, ThemeDictionaries.Choose(proTheme, systemHighContrast));
    }

    [Theory]
    [InlineData(ThemeVariant.Light, "/EncDotNet.ArcGIS.SoundCharts;component/Themes/SoundCharts.Colors.Light.xaml")]
    [InlineData(ThemeVariant.Dark, "/EncDotNet.ArcGIS.SoundCharts;component/Themes/SoundCharts.Colors.Dark.xaml")]
    [InlineData(ThemeVariant.HighContrast, "/EncDotNet.ArcGIS.SoundCharts;component/Themes/SoundCharts.Colors.HighContrast.xaml")]
    public void ColorsUri_NamesTheVariantsDictionary(ThemeVariant variant, string expected)
    {
        var uri = ThemeDictionaries.ColorsUri(variant);

        Assert.False(uri.IsAbsoluteUri);
        Assert.Equal(expected, uri.OriginalString);
    }

    [Fact]
    public void ColorsUri_RejectsAnUndefinedVariant()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ThemeDictionaries.ColorsUri((ThemeVariant)42));
    }

    [Fact]
    public void SharedUri_NamesTheSharedDictionary()
    {
        Assert.Equal("/EncDotNet.ArcGIS.SoundCharts;component/Themes/SoundCharts.xaml", ThemeDictionaries.SharedUri.OriginalString);
    }

    [Fact]
    public void AssemblyName_IsTheAddInAssembly()
    {
        Assert.Equal(ThemeDictionaries.AssemblyName, typeof(ThemeDictionaries).Assembly.GetName().Name);
    }

    [Theory]
    [MemberData(nameof(DictionaryFiles))]
    public void ThemeDictionaries_AreInTheAssembly(string uri)
    {
        // The pack URI's path, lower-cased, is the BAML resource name.
        var path = uri[(uri.IndexOf("component/", StringComparison.Ordinal) + "component/".Length)..];
        var baml = path.Replace(".xaml", ".baml", StringComparison.Ordinal).ToLowerInvariant();
        var assembly = typeof(ThemeDictionaries).Assembly;
        using var stream = assembly.GetManifestResourceStream($"{ThemeDictionaries.AssemblyName}.g.resources");
        Assert.NotNull(stream);
        using var reader = new ResourceReader(stream);
        var names = reader.Cast<DictionaryEntry>().Select(e => (string)e.Key).ToList();

        Assert.Contains(baml, names);
    }

    [Theory]
    [InlineData("images/aboutsoundcharts32.scale-200.png")]
    [InlineData("darkimages/aboutsoundcharts32.scale-200.png")]
    public void AppIcons_AreInTheAssembly(string resource)
    {
        // SC_AppIcon's pack URIs in the colour dictionaries reach these.
        var assembly = typeof(ThemeDictionaries).Assembly;
        using var stream = assembly.GetManifestResourceStream($"{ThemeDictionaries.AssemblyName}.g.resources");
        Assert.NotNull(stream);
        using var reader = new ResourceReader(stream);

        Assert.Contains(resource, reader.Cast<DictionaryEntry>().Select(e => (string)e.Key));
    }

    public static TheoryData<string> DictionaryFiles() =>
    [
        ThemeDictionaries.SharedUri.OriginalString,
        ThemeDictionaries.ColorsUri(ThemeVariant.Light).OriginalString,
        ThemeDictionaries.ColorsUri(ThemeVariant.Dark).OriginalString,
        ThemeDictionaries.ColorsUri(ThemeVariant.HighContrast).OriginalString,
    ];

    [Theory]
    [InlineData(RouteCheckTone.Neutral, SoundChartsTone.Neutral)]
    [InlineData(RouteCheckTone.Success, SoundChartsTone.Success)]
    [InlineData(RouteCheckTone.Warning, SoundChartsTone.Warning)]
    [InlineData(RouteCheckTone.Destructive, SoundChartsTone.Danger)]
    public void From_MapsRouteCheckTones(RouteCheckTone tone, SoundChartsTone expected)
    {
        Assert.Equal(expected, Tones.From(tone));
    }

    [Theory]
    [InlineData("Info", SoundChartsTone.Info)]
    [InlineData("warning", SoundChartsTone.Warning)]
    [InlineData("Destructive", SoundChartsTone.Danger)]
    [InlineData("error", SoundChartsTone.Danger)]
    [InlineData("Danger", SoundChartsTone.Danger)]
    [InlineData("Nonsense", SoundChartsTone.Neutral)]
    [InlineData("", SoundChartsTone.Neutral)]
    public void From_MapsToneNames(string name, SoundChartsTone expected)
    {
        Assert.Equal(expected, Tones.From(name));
    }

    [Fact]
    public void From_KeepsASoundChartsToneAndDefaultsToNeutral()
    {
        Assert.Equal(SoundChartsTone.Success, Tones.From(SoundChartsTone.Success));
        Assert.Equal(SoundChartsTone.Neutral, Tones.From(null));
        Assert.Equal(SoundChartsTone.Neutral, Tones.From(42));
    }
}
