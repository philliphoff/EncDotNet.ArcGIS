using EncDotNet.SoundCharts.Charts;
using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts.Tests.Charts;

public class RenderKeyTests
{
    private const string Revision = "C:\\CHARTS\\US5MA1BO.000|1024|638000000000000000\n";

    [Fact]
    public void AKey_IsSixteenLowerCaseHexCharacters()
    {
        var key = RenderKey.Compute(ChartRenderOptions.Default, "0.27.1", Revision);

        Assert.Equal(RenderKey.Length, key.Length);
        Assert.True(RenderKey.IsValid(key));
        Assert.Matches("^[0-9a-f]{16}$", key);
    }

    [Fact]
    public void TheSameInputs_GiveTheSameKey()
    {
        var first = RenderKey.Compute(new ChartRenderOptions(SymbolScale: 1.5, NationalLanguage: "fra"), "0.27.1", Revision);
        var second = RenderKey.Compute(new ChartRenderOptions(SymbolScale: 1.5, NationalLanguage: "fra"), "0.27.1", Revision);

        Assert.Equal(first, second);
    }

    [Fact]
    public void TheKey_IsStableAcrossMachinesAndRuns()
    {
        // Pinned: a change here changes every SoundCharts URL. Bump the schema in RenderKey on purpose, then update this.
        Assert.Equal("27a753e86d3e1f8d", RenderKey.Compute(ChartRenderOptions.Default, "0.27.1", Revision));
    }

    public static TheoryData<ChartRenderOptions> ChangedOptions() =>
    [
        new ChartRenderOptions(SymbolScale: 1.5),
        new ChartRenderOptions(TextScale: 0.8),
        new ChartRenderOptions(ShowText: false),
        new ChartRenderOptions(NationalLanguage: "fra"),
        new ChartRenderOptions(DisplayMode: "ice-sod"),
    ];

    [Theory]
    [MemberData(nameof(ChangedOptions))]
    public void AnyServerSetting_ChangesTheKey(ChartRenderOptions changed)
    {
        Assert.NotEqual(
            RenderKey.Compute(ChartRenderOptions.Default, "0.27.1", Revision),
            RenderKey.Compute(changed, "0.27.1", Revision));
    }

    [Fact]
    public void AnotherServerVersion_ChangesTheKey()
    {
        Assert.NotEqual(
            RenderKey.Compute(ChartRenderOptions.Default, "0.27.1", Revision),
            RenderKey.Compute(ChartRenderOptions.Default, "0.28.0", Revision));
    }

    [Fact]
    public void AnotherDataRevision_ChangesTheKey()
    {
        Assert.NotEqual(
            RenderKey.Compute(ChartRenderOptions.Default, "0.27.1", Revision),
            RenderKey.Compute(ChartRenderOptions.Default, "0.27.1", Revision.Replace("1024", "2048", StringComparison.Ordinal)));
    }

    [Fact]
    public void TheKey_FitsInAServerToken()
    {
        var key = RenderKey.Compute(ChartRenderOptions.Default, null, string.Empty);

        Assert.True(ServerToken.IsValid(ServerToken.Create(key)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("3f9a1c02")]
    [InlineData("3F9A1C02E47B5D18")]
    [InlineData("3f9a1c02e47b5d1g")]
    public void IsValid_RejectsAnythingElse(string? key)
    {
        Assert.False(RenderKey.IsValid(key));
    }

    [Fact]
    public void OfADatasetSet_ReadsTheFilesNow()
    {
        using var temp = new TempFolder();
        var path = temp.File("US5MA1BO.000", "edition 1");
        var set = new DatasetSet([DatasetSource.Dataset(path)]);
        var before = RenderKey.Compute(set, "0.27.1");

        File.AppendAllText(path, " and an update");

        Assert.NotEqual(before, RenderKey.Compute(set, "0.27.1"));
    }

    [Fact]
    public void OfADatasetSet_FollowsItsOptions()
    {
        using var temp = new TempFolder();
        var set = new DatasetSet([DatasetSource.Dataset(temp.File("US5MA1BO.000", "x"))]);

        Assert.NotEqual(
            RenderKey.Compute(set, "0.27.1"),
            RenderKey.Compute(set.WithOptions(new ChartRenderOptions(ShowText: false)), "0.27.1"));
    }
}
