using EncDotNet.SoundCharts.Charts;

namespace EncDotNet.SoundCharts.Tests.Charts;

public class ChartRenderOptionsTests
{
    [Fact]
    public void TheDefaults_AddNoArguments()
    {
        Assert.Empty(ChartRenderOptions.Default.ToArguments());
    }

    [Fact]
    public void EveryOption_BecomesItsArgument()
    {
        var options = new ChartRenderOptions(SymbolScale: 1.5, TextScale: 0.8, ShowText: false, NationalLanguage: "fra", DisplayMode: "base");

        Assert.Equal(
            ["--symbol-scale", "1.5", "--text-scale", "0.8", "--no-text", "--national-language", "fra", "--display-mode", "base"],
            options.ToArguments());
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void AScaleThatIsntPositive_IsRejected(double scale)
    {
        Assert.Throws<InvalidOperationException>(() => new ChartRenderOptions(SymbolScale: scale).ToArguments());
    }

    [Fact]
    public void TheCanonicalString_IsTheSameForEqualOptions()
    {
        Assert.Equal(
            new ChartRenderOptions(TextScale: 1.25).ToCanonicalString(),
            new ChartRenderOptions(TextScale: 1.25).ToCanonicalString());
        Assert.NotEqual(
            new ChartRenderOptions(TextScale: 1.25).ToCanonicalString(),
            ChartRenderOptions.Default.ToCanonicalString());
    }
}
