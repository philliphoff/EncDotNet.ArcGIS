using EncDotNet.SoundCharts.Settings;

namespace EncDotNet.SoundCharts.Tests.Settings;

public class ChartStyleTests
{
    private const string Key = "3f9a1c02e47b5d18";

    [Theory]
    [InlineData(ChartPalette.Day, "day")]
    [InlineData(ChartPalette.Dusk, "dusk")]
    [InlineData(ChartPalette.Night, "night")]
    public void APalette_IsTheServersStyle(ChartPalette palette, string style)
    {
        Assert.Equal(style, ChartStyle.For(palette));
        Assert.Equal(palette, ChartStyle.PaletteOf(style));
    }

    [Fact]
    public void TodaysServer_TakesNoRenderKeyInTheStyle()
    {
        // tiles serve answers an unknown style with an OWS exception (U1), so the key goes in the token.
        Assert.False(ChartServerSupport.SettingsStyles);
        Assert.Equal("night", ChartStyle.For(ChartPalette.Night, Key));
    }

    [Fact]
    public void AServerWithSettingsStyles_GetsPaletteDashKey()
    {
        Assert.Equal($"night-{Key}", ChartStyle.For(ChartPalette.Night, Key, settingsStyles: true));
        Assert.Equal(ChartPalette.Night, ChartStyle.PaletteOf($"night-{Key}"));
    }

    [Fact]
    public void ABadKey_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => ChartStyle.For(ChartPalette.Day, "NOT-A-KEY", settingsStyles: true));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("default")]
    [InlineData("sepia-3f9a1c02e47b5d18")]
    public void OtherStyles_HaveNoPalette(string? style)
    {
        Assert.Null(ChartStyle.PaletteOf(style));
    }

    [Fact]
    public void OnlyThePaletteAndServerStartUpOptions_AreSupportedToday()
    {
        var supported = Enum.GetValues<MarinerSetting>().Where(ChartServerSupport.Supports);

        Assert.Equal(
            [MarinerSetting.Palette, MarinerSetting.Text, MarinerSetting.SymbolScale, MarinerSetting.TextScale, MarinerSetting.NationalLanguage],
            supported);
    }
}
