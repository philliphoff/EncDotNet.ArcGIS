using EncDotNet.SoundCharts.Charts;

namespace EncDotNet.SoundCharts.Tests.Charts;

public class DatasetSetTests
{
    private static readonly DatasetSource Cell = DatasetSource.Dataset(@"C:\Charts\101AA00DS0001.000");
    private static readonly DatasetSource Bathymetry = DatasetSource.Dataset(@"C:\Charts\102AA00DS0001.h5");
    private static readonly DatasetSource ExchangeSet = DatasetSource.ExchangeSet(@"C:\Charts\US_ENC");

    [Fact]
    public void Sets_WithTheSameSourcesInAnyOrder_AreEqual()
    {
        var a = new DatasetSet([Cell, Bathymetry]);
        var b = new DatasetSet([Bathymetry, Cell]);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.Equal(a.Key, b.Key);
        Assert.Equal(a.Id, b.Id);
    }

    [Fact]
    public void Sets_IgnoreCaseAndTrailingSeparators_AsWindowsPathsDo()
    {
        var a = new DatasetSet([DatasetSource.ExchangeSet(@"C:\Charts\US_ENC\")]);
        var b = new DatasetSet([DatasetSource.ExchangeSet(@"c:\charts\us_enc")]);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Sets_DropDuplicates_AndKeepACanonicalOrder()
    {
        var set = new DatasetSet([Cell, Bathymetry, Cell]);

        Assert.Equal([Cell, Bathymetry], set.Sources);
    }

    [Fact]
    public void Sets_WithOtherOptions_Differ()
    {
        var plain = new DatasetSet([Cell]);
        var noText = new DatasetSet([Cell], new ChartRenderOptions(ShowText: false));

        Assert.NotEqual(plain, noText);
        Assert.NotEqual(plain.Id, noText.Id);
    }

    [Fact]
    public void Sets_OfADatasetAndAnExchangeSetAtOnePath_Differ()
    {
        Assert.NotEqual(
            new DatasetSet([DatasetSource.Dataset(@"C:\Charts\x")]),
            new DatasetSet([DatasetSource.Folder(@"C:\Charts\x")]));
    }

    [Fact]
    public void Id_IsEightLowerCaseHexCharacters_AndStable()
    {
        var set = new DatasetSet([Cell]);

        Assert.Matches("^[0-9a-f]{8}$", set.Id);
        Assert.Equal(set.Id, new DatasetSet([Cell]).Id);
    }

    [Fact]
    public void AnEmptySet_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => new DatasetSet([]));
    }

    [Fact]
    public void With_AddsASource()
    {
        var set = new DatasetSet([Cell]).With(Bathymetry);

        Assert.Equal([Cell, Bathymetry], set.Sources);
    }

    [Fact]
    public void With_AnExistingSource_ReturnsTheSameSet()
    {
        var set = new DatasetSet([Cell]);

        Assert.Same(set, set.With(DatasetSource.Dataset(@"c:\charts\101aa00ds0001.000")));
    }

    [Fact]
    public void Without_RemovesASource_KeepingTheOptions()
    {
        var options = new ChartRenderOptions(SymbolScale: 2);
        var set = new DatasetSet([Cell, Bathymetry], options).Without(Cell);

        Assert.NotNull(set);
        Assert.Equal([Bathymetry], set.Sources);
        Assert.Equal(options, set.Options);
    }

    [Fact]
    public void Without_TheLastSource_GivesNull()
    {
        Assert.Null(new DatasetSet([ExchangeSet]).Without(ExchangeSet));
    }

    [Fact]
    public void WithOptions_KeepsTheSources()
    {
        var set = new DatasetSet([Cell]);
        var night = set.WithOptions(new ChartRenderOptions(TextScale: 1.25));

        Assert.Equal(set.Sources, night.Sources);
        Assert.NotEqual(set, night);
        Assert.Same(set, set.WithOptions(ChartRenderOptions.Default));
    }
}
