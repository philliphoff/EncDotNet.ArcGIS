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

    [Fact]
    public void IsAdvisoryFolder_AStormsFolder_IsAndStaysAFolderSource()
    {
        // Issue #69: the folder is one TC dataset to EncDotNet.S100, but a
        // folder to the inspector, which may find charts beside the storm.
        using var temp = new TempFolder();
        var folder = Path.Combine(temp.Path, "ian");
        Assert.SkipUnless(Fixtures.CopyUpstreamStorm(folder), $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");

        Assert.True(DatasetSource.IsAdvisoryFolder(folder));
        Assert.Equal(DatasetSourceKind.Folder, DatasetSource.FromPath(folder).Kind);
    }

    [Fact]
    public void IsAdvisoryFolder_SeveralStormsOrNone_IsNot()
    {
        using var temp = new TempFolder();
        var storms = Path.Combine(temp.Path, "storms");
        Assert.SkipUnless(Fixtures.CopyUpstreamStorm(storms), $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        Fixtures.CopyUpstreamStorm(storms, atcfId: "ep012022");

        Assert.False(DatasetSource.IsAdvisoryFolder(storms));
        Assert.False(DatasetSource.IsAdvisoryFolder(temp.Folder("empty")));
        Assert.False(DatasetSource.IsAdvisoryFolder(Path.Combine(temp.Path, "missing")));
    }

    [Fact]
    public void IsAdvisoryFolder_AGrib2RunWithAStorm_IsNot()
    {
        // EncDotNet.S100 reads such a folder as the run, and lists the storm's forecast zip.
        var grib = Upstream("GRIB2/gfswave-useast.grib2");
        using var temp = new TempFolder();
        var folder = Path.Combine(temp.Path, "run");
        Assert.SkipUnless(Fixtures.CopyUpstreamStorm(folder), "No TC fixture.");
        File.Copy(grib, Path.Combine(folder, "a.grib2"));
        File.Copy(Upstream("GRIB2/gfswave-useast-filter.grib2"), Path.Combine(folder, "b.grib2"));

        Assert.False(DatasetSource.IsAdvisoryFolder(folder));
    }

    [Fact]
    public void IsRunFolder_AFolderOfForecastHours_IsAndStaysAFolderSource()
    {
        // Issue #73: the folder is one GRIB2 dataset to EncDotNet.S100, but a
        // folder to the inspector, which may find charts beside the run.
        using var temp = new TempFolder();
        var folder = Path.Combine(temp.Path, "gfswave");
        Assert.SkipWhen(Fixtures.WriteUpstreamRun(folder).Count == 0, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");

        Assert.True(DatasetSource.IsRunFolder(folder));
        Assert.False(DatasetSource.IsAdvisoryFolder(folder));
        Assert.Equal(DatasetSourceKind.Folder, DatasetSource.FromPath(folder).Kind);
    }

    [Fact]
    public void IsRunFolder_OneGrib2FileOrNone_IsNot()
    {
        // As in EncDotNet.S100's Library, a folder of one GRIB2 file lists the file.
        using var temp = new TempFolder();
        var folder = Path.Combine(temp.Path, "one");
        Assert.SkipWhen(Fixtures.WriteUpstreamRun(folder, [3]).Count == 0, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        temp.File(@"one/gfswave.t00z.global.0p25.f003.grib2.idx", "1:0:d=2026102500:HTSGW:surface:3 hour fcst:");
        temp.File(@"text/a.grib2", "not GRIB");
        temp.File(@"text/b.grib2", "not GRIB either");

        Assert.False(DatasetSource.IsRunFolder(folder));
        Assert.False(DatasetSource.IsRunFolder(Path.Combine(temp.Path, "text")));
        Assert.False(DatasetSource.IsRunFolder(temp.Folder("empty")));
        Assert.False(DatasetSource.IsRunFolder(Path.Combine(temp.Path, "missing")));
        Assert.False(DatasetSource.IsRunFolder(Path.Combine(folder, "gfswave.t00z.global.0p25.f003.grib2")));
    }

    [Fact]
    public void IsRunFolder_ARunWithAStorm_IsTheRun()
    {
        using var temp = new TempFolder();
        var folder = Path.Combine(temp.Path, "run");
        Assert.SkipWhen(Fixtures.WriteUpstreamRun(folder).Count == 0, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        Assert.SkipUnless(Fixtures.CopyUpstreamStorm(folder), "No TC fixture.");

        Assert.True(DatasetSource.IsRunFolder(folder));
        Assert.False(DatasetSource.IsAdvisoryFolder(folder));
    }

    private static string Upstream(string relativePath)
    {
        var path = Fixtures.Upstream(relativePath);
        Assert.SkipWhen(path is null, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        return path!;
    }
}
