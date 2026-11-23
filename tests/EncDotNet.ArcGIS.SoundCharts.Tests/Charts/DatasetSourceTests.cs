using System.IO.Compression;
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
    public void FromPath_AZipWithACatalogue_IsAnExchangeSet()
    {
        using var temp = new TempFolder();
        var path = Path.Combine(temp.Path, "set.zip");
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            zip.CreateEntry("S100_ROOT/CATALOG.XML");
        }

        Assert.Equal(DatasetSourceKind.ExchangeSet, DatasetSource.FromPath(path).Kind);
        Assert.False(DatasetSource.IsZippedDataset(path));
    }

    [Fact]
    public void FromPath_AnS101ExchangeSetZip_IsAnExchangeSet()
    {
        var path = Upstream("S101.zip");

        Assert.Equal(DatasetSourceKind.ExchangeSet, DatasetSource.FromPath(path).Kind);
    }

    [Fact]
    public void FromPath_AnAdvisoryForecastZip_IsADataset()
    {
        // Issue #63: EncDotNet.S100 reads an NHC forecast zip as a TC dataset.
        var path = Upstream("TC/al092022/al092022_5day_020.zip");

        var source = DatasetSource.FromPath(path);

        Assert.Equal(DatasetSourceKind.Dataset, source.Kind);
        Assert.True(DatasetSource.IsZippedDataset(path));
    }

    [Theory]
    [InlineData("TC/al092022/al092022_fcst_020.zip")]
    [InlineData("TC/al092022/al092022_best_track.zip")]
    public void IsZippedDataset_AnAdvisorysSiblingZip_IsFalse(string relativePath)
    {
        // Read with the forecast zip beside it, never on its own.
        Assert.False(DatasetSource.IsZippedDataset(Upstream(relativePath)));
    }

    [Fact]
    public void IsZippedDataset_AMissingOrNonZipFile_IsFalse()
    {
        using var temp = new TempFolder();
        var text = temp.File("notes.txt", "text");

        Assert.False(DatasetSource.IsZippedDataset(Path.Combine(temp.Path, "missing.zip")));
        Assert.False(DatasetSource.IsZippedDataset(text));
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

    private static string Upstream(string relativePath)
    {
        var path = Fixtures.Upstream(relativePath);
        Assert.SkipWhen(path is null, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        return path!;
    }
}
