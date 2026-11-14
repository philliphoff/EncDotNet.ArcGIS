using EncDotNet.SoundCharts.Charts;
using EncDotNet.SoundCharts.Settings;

namespace EncDotNet.SoundCharts.Tests.Settings;

public class MarinerSettingsTests
{
    [Fact]
    public void TheDefaults_AreTheViewers()
    {
        // EncDotNet.S100 main: MarinerSettings, EcdisDisplaySettings and ViewerSettings.
        var settings = MarinerSettings.Default;

        Assert.Equal(ChartPalette.Day, settings.Palette);
        Assert.Equal(DisplayCategory.Standard, settings.DisplayCategory);
        Assert.Equal(30.0, settings.SafetyContour);
        Assert.Equal(30.0, settings.SafetyDepth);
        Assert.Equal(2.0, settings.ShallowContour);
        Assert.Equal(30.0, settings.DeepContour);
        Assert.False(settings.FourShades);
        Assert.True(settings.ShallowWaterDangers);
        Assert.True(settings.PlainBoundaries);
        Assert.False(settings.SimplifiedSymbols);
        Assert.False(settings.FullLightLines);
        Assert.False(settings.IgnoreScaleMinimum);
        Assert.True(settings.ShowSoundings);
        Assert.True(settings.ShowText);
        Assert.Equal(1.0, settings.SymbolScale);
        Assert.Equal(1.0, settings.TextScale);
        Assert.Equal(string.Empty, settings.NationalLanguage);
        Assert.Empty(settings.Validate());
    }

    [Fact]
    public void TheDefaults_GiveTheServersDefaults()
    {
        Assert.Equal(ChartRenderOptions.Default, MarinerSettings.Default.ToRenderOptions());
    }

    [Fact]
    public void ToRenderOptions_CarriesWhatTheServerTakes()
    {
        var settings = MarinerSettings.Default with
        {
            ShowText = false,
            SymbolScale = 1.5,
            TextScale = 0.8,
            NationalLanguage = " FRA ",
            SafetyContour = 10,
            DisplayCategory = DisplayCategory.All,
        };

        Assert.Equal(
            new ChartRenderOptions(SymbolScale: 1.5, TextScale: 0.8, ShowText: false, NationalLanguage: "fra"),
            settings.ToRenderOptions());
    }

    [Fact]
    public void ToRenderOptions_KeepsTheBaselinesOtherOptions()
    {
        var options = MarinerSettings.Default.ToRenderOptions(new ChartRenderOptions(DisplayMode: "ice-sod"));

        Assert.Equal("ice-sod", options.DisplayMode);
    }

    [Fact]
    public void SettingsTheServerDoesntTake_DontChangeTheRenderOptions()
    {
        var settings = MarinerSettings.Default with
        {
            Palette = ChartPalette.Night,
            DisplayCategory = DisplayCategory.DisplayBase,
            SafetyContour = 10,
            FourShades = true,
            ShowSoundings = false,
        };

        Assert.Equal(MarinerSettings.Default.ToRenderOptions(), settings.ToRenderOptions());
    }

    [Theory]
    [InlineData(-1, 2, 30, MarinerSettingsProblem.DepthOutOfRange)]
    [InlineData(30, 2, 20000, MarinerSettingsProblem.DepthOutOfRange)]
    [InlineData(10, 20, 30, MarinerSettingsProblem.ContoursOutOfOrder)]
    [InlineData(10, 2, 5, MarinerSettingsProblem.ContoursOutOfOrder)]
    public void Contours_FollowTheViewersRules(double safety, double shallow, double deep, MarinerSettingsProblem expected)
    {
        var settings = MarinerSettings.Default with { SafetyContour = safety, ShallowContour = shallow, DeepContour = deep };

        Assert.Equal([expected], settings.Validate());
    }

    [Theory]
    [InlineData(0.4)]
    [InlineData(3.1)]
    [InlineData(double.NaN)]
    public void Scales_StayWithinTheViewersRange(double scale)
    {
        Assert.Equal([MarinerSettingsProblem.ScaleOutOfRange], (MarinerSettings.Default with { TextScale = scale }).Validate());
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("fra1")]
    [InlineData("f-a")]
    public void ANationalLanguage_IsThreeLetters(string language)
    {
        Assert.Equal([MarinerSettingsProblem.NationalLanguageInvalid], (MarinerSettings.Default with { NationalLanguage = language }).Validate());
    }
}
