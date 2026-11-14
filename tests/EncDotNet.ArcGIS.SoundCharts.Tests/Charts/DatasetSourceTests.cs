using EncDotNet.SoundCharts.Charts;

namespace EncDotNet.SoundCharts.Tests.Charts;

public class DatasetSourceTests
{
    [Fact]
    public void FromPath_AFolderWithACatalogue_IsAnExchangeSet()
    {
        using var temp = new TempFolder();
        temp.File(@"US_ENC\CATALOG.XML", "<catalog/>");

        var source = DatasetSource.FromPath(Path.Combine(temp.Path, "US_ENC"));

        Assert.Equal(DatasetSourceKind.ExchangeSet, source.Kind);
        Assert.Equal("US_ENC", source.Name);
    }

    [Fact]
    public void FromPath_AnS57ExchangeSetFolder_IsAnExchangeSet()
    {
        using var temp = new TempFolder();
        temp.File(@"ENC_ROOT\CATALOG.031");

        Assert.Equal(DatasetSourceKind.ExchangeSet, DatasetSource.FromPath(Path.Combine(temp.Path, "ENC_ROOT")).Kind);
    }

    [Fact]
    public void FromPath_AnyOtherFolder_IsAFolderOfDatasets()
    {
        using var temp = new TempFolder();
        temp.File(@"Charts\101AA00DS0001.000");

        Assert.Equal(DatasetSourceKind.Folder, DatasetSource.FromPath(Path.Combine(temp.Path, "Charts")).Kind);
    }

    [Theory]
    [InlineData(@"C:\Charts\US_ENC\CATALOG.XML", DatasetSourceKind.ExchangeSet)]
    [InlineData(@"C:\Charts\ENC_ROOT\catalog.031", DatasetSourceKind.ExchangeSet)]
    [InlineData(@"C:\Charts\exchange-set.zip", DatasetSourceKind.ExchangeSet)]
    [InlineData(@"C:\Charts\101AA00DS0001.000", DatasetSourceKind.Dataset)]
    [InlineData(@"C:\Charts\104AA00DS0001.h5", DatasetSourceKind.Dataset)]
    public void FromPath_AFile_IsClassifiedByName(string path, DatasetSourceKind expected)
    {
        Assert.Equal(expected, DatasetSource.FromPath(path).Kind);
    }

    [Fact]
    public void Path_IsMadeFull()
    {
        var source = DatasetSource.Dataset(@"charts\a.000");

        Assert.True(Path.IsPathFullyQualified(source.Path));
        Assert.EndsWith(@"charts\a.000", source.Path, StringComparison.Ordinal);
    }

    [Fact]
    public void ARootFolder_KeepsItsSeparator()
    {
        Assert.Equal(@"D:\", DatasetSource.Folder(@"D:\").Path);
    }
}
