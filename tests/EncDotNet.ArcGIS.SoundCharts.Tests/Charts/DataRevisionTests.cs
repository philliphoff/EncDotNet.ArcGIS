using EncDotNet.SoundCharts.Charts;

namespace EncDotNet.SoundCharts.Tests.Charts;

public class DataRevisionTests
{
    [Fact]
    public void ADataset_IsItsPathSizeAndWriteTime()
    {
        using var temp = new TempFolder();
        var path = temp.File("US5MA1BO.000", "12345");
        var info = new FileInfo(path);

        var revision = DataRevision.Of([DatasetSource.Dataset(path)]);

        Assert.Equal($"{path.ToUpperInvariant()}|5|{info.LastWriteTimeUtc.Ticks}\n", revision);
    }

    [Fact]
    public void AnS57Cell_BringsItsUpdateFiles()
    {
        using var temp = new TempFolder();
        var cell = temp.File("US5MA1BO.000", "base");
        var update = temp.File("US5MA1BO.001", "update");
        temp.File("US5MA1BO.txt", "not an update");
        temp.File("OTHER.001", "another cell's update");

        var revision = DataRevision.Of([DatasetSource.Dataset(cell)]);

        Assert.Contains(update.ToUpperInvariant(), revision, StringComparison.Ordinal);
        Assert.DoesNotContain("US5MA1BO.TXT", revision, StringComparison.Ordinal);
        Assert.DoesNotContain("OTHER.001", revision, StringComparison.Ordinal);
    }

    [Fact]
    public void ANewUpdateFile_ChangesTheRevision()
    {
        using var temp = new TempFolder();
        var cell = temp.File("US5MA1BO.000", "base");
        var before = DataRevision.Of([DatasetSource.Dataset(cell)]);

        temp.File("US5MA1BO.001", "update");

        Assert.NotEqual(before, DataRevision.Of([DatasetSource.Dataset(cell)]));
    }

    [Fact]
    public void AFolder_BringsEveryFileUnderIt()
    {
        using var temp = new TempFolder();
        var folder = temp.Folder("ENC_ROOT");
        temp.File(Path.Combine("ENC_ROOT", "CATALOG.031"), "c");
        temp.File(Path.Combine("ENC_ROOT", "US5MA1BO", "US5MA1BO.000"), "x");

        var revision = DataRevision.Of([DatasetSource.Folder(folder)]);

        Assert.Equal(2, revision.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public void AnExchangeSetCatalogue_BringsItsFolder()
    {
        using var temp = new TempFolder();
        var catalogue = temp.File(Path.Combine("set", "CATALOG.XML"), "c");
        temp.File(Path.Combine("set", "S101", "101AA00DS0001.000"), "x");

        var revision = DataRevision.Of([DatasetSource.ExchangeSet(catalogue)]);

        Assert.Contains("101AA00DS0001.000", revision, StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingFile_IsMarkedMissing()
    {
        using var temp = new TempFolder();
        var path = Path.Combine(temp.Path, "gone.h5");

        Assert.EndsWith("|missing\n", DataRevision.Of([DatasetSource.Dataset(path)]), StringComparison.Ordinal);
    }

    [Fact]
    public void TheOrderOfSources_DoesntMatter()
    {
        using var temp = new TempFolder();
        var a = DatasetSource.Dataset(temp.File("a.h5", "a"));
        var b = DatasetSource.Dataset(temp.File("b.h5", "b"));

        Assert.Equal(DataRevision.Of([a, b]), DataRevision.Of([b, a]));
    }
}
