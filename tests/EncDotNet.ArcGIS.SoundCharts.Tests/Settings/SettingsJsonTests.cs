using System.Globalization;
using EncDotNet.SoundCharts.Settings;

namespace EncDotNet.SoundCharts.Tests.Settings;

public class SettingsJsonTests
{
    private static readonly MarinerSettings Changed = new()
    {
        Palette = ChartPalette.Night,
        DisplayCategory = DisplayCategory.All,
        SafetyContour = 10,
        SafetyDepth = 12.5,
        ShallowContour = 5,
        DeepContour = 20,
        FourShades = true,
        ShallowWaterDangers = false,
        PlainBoundaries = false,
        SimplifiedSymbols = true,
        FullLightLines = true,
        IgnoreScaleMinimum = true,
        ShowSoundings = false,
        ShowText = false,
        SymbolScale = 1.5,
        TextScale = 0.8,
        NationalLanguage = "fra",
        Draft = 7.5,
        UkcMargin = 1.2,
    };

    [Fact]
    public void MarinerSettings_RoundTrip()
    {
        Assert.Equal(Changed, SettingsJson.ReadMariner(SettingsJson.ToJson(Changed)));
    }

    [Fact]
    public void RouteCheckValues_UnsetByNull_DefaultWhenMissingOrInvalid()
    {
        var defaults = MarinerSettings.Default with { Draft = 6.0, UkcMargin = 1.0 };

        Assert.Equal((6.0, 1.0), Ship(SettingsJson.ReadMariner("{}", defaults)));
        Assert.Equal(((double?)null, (double?)null), Ship(SettingsJson.ReadMariner("""{"draft":null,"ukcMargin":null}""", defaults)));
        Assert.Equal((6.0, 1.0), Ship(SettingsJson.ReadMariner("""{"draft":0,"ukcMargin":-1}""", defaults)));

        // A bad draft doesn't take the depths with it.
        var read = SettingsJson.ReadMariner("""{"draft":500,"safetyContour":10,"deepContour":40}""", defaults);
        Assert.Equal(6.0, read?.Draft);
        Assert.Equal(10, read?.SafetyContour);

        static (double?, double?) Ship(MarinerSettings? settings) => (settings?.Draft, settings?.UkcMargin);
    }

    [Fact]
    public void TheDefaults_RoundTrip()
    {
        Assert.Equal(MarinerSettings.Default, SettingsJson.ReadMariner(SettingsJson.ToJson(MarinerSettings.Default)));
    }

    [Fact]
    public void ApplicationSettings_RoundTrip()
    {
        var settings = new ApplicationSettings
        {
            ProjectDefaults = Changed,
            FollowProTheme = true,
            TimeFormat = TimeFormat.Local,
            DepthUnit = DepthUnit.FathomsFeet,
            ServerPath = @"C:\Tools\s100.exe",
            TileCacheMegabytes = 4096,
        };

        Assert.Equal(settings, SettingsJson.ReadApplication(SettingsJson.ToJson(settings)));
    }

    [Fact]
    public void TheJson_CarriesTheSchemaVersion_AndInvariantNumbers()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            var json = SettingsJson.ToJson(Changed);

            Assert.StartsWith("{\"version\":1,", json, StringComparison.Ordinal);
            Assert.Contains("\"safetyDepth\":12.5", json, StringComparison.Ordinal);
            Assert.Contains("\"palette\":\"Night\"", json, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void MissingValues_TakeTheDefaults()
    {
        var defaults = MarinerSettings.Default with { SafetyContour = 15, DeepContour = 40 };

        var read = SettingsJson.ReadMariner("{\"version\":1,\"palette\":\"Dusk\"}", defaults);

        Assert.Equal(defaults with { Palette = ChartPalette.Dusk }, read);
    }

    [Fact]
    public void UnknownKeysAndANewerVersion_AreIgnored()
    {
        var read = SettingsJson.ReadMariner("{\"version\":7,\"text\":false,\"radarOverlay\":true}");

        Assert.Equal(MarinerSettings.Default with { ShowText = false }, read);
    }

    [Theory]
    [InlineData("{\"palette\":\"Sepia\"}")]
    [InlineData("{\"palette\":\"7\"}")]
    [InlineData("{\"palette\":3}")]
    [InlineData("{\"text\":\"yes\"}")]
    [InlineData("{\"symbolScale\":\"big\"}")]
    [InlineData("{\"symbolScale\":12}")]
    [InlineData("{\"nationalLanguage\":\"french\"}")]
    [InlineData("{\"safetyContour\":-5}")]
    public void InvalidValues_TakeTheDefaults(string json)
    {
        Assert.Equal(MarinerSettings.Default, SettingsJson.ReadMariner(json));
    }

    [Fact]
    public void ContoursOutOfOrder_FallBackTogether()
    {
        var read = SettingsJson.ReadMariner("{\"safetyContour\":10,\"shallowContour\":20,\"deepContour\":30,\"fourShades\":true}");

        Assert.NotNull(read);
        Assert.Equal(MarinerSettings.Default.SafetyContour, read.SafetyContour);
        Assert.Equal(MarinerSettings.Default.ShallowContour, read.ShallowContour);
        Assert.True(read.FourShades);
        Assert.Empty(read.Validate());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    [InlineData("\"day\"")]
    public void WhatIsntAJsonObject_ReadsAsNull(string? json)
    {
        Assert.Null(SettingsJson.ReadMariner(json));
        Assert.Null(SettingsJson.ReadApplication(json));
    }

    [Fact]
    public void AnOutOfRangeTileCache_TakesTheDefault()
    {
        var read = SettingsJson.ReadApplication("{\"tileCacheMegabytes\":1}");

        Assert.Equal(ApplicationSettings.Default.TileCacheMegabytes, read?.TileCacheMegabytes);
    }
}
