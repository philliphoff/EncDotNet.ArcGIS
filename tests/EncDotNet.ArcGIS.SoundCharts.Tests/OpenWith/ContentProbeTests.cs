using System.IO.Compression;
using EncDotNet.SoundCharts.OpenWith;

namespace EncDotNet.SoundCharts.Tests.OpenWith;

/// <summary>
/// Open with SoundCharts (issue #6) shows only for Catalog items that hold
/// chart data, judged by their content. Most tests use a fake product
/// detector that reads a marker from the file, so they don't need real
/// datasets; the last ones use EncDotNet.S100's test datasets when
/// <see cref="Fixtures.UpstreamDatasetsVariable"/> is set.
/// </summary>
public sealed class ContentProbeTests : IDisposable
{
    private const string S100Catalogue =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<S100XC:S100_ExchangeCatalogue xmlns:S100XC=\"http://www.iho.int/s100/xc/5.0\" />";

    private readonly TempFolder _temp = new();
    private readonly List<string> _detected = [];

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void File_IsADataset_WhenItsContentNamesAProduct_WhateverItsName()
    {
        var path = _temp.File("chart.bin", "product:S-102");

        var result = Probe().Probe(path);

        Assert.Equal(ContentProbeKind.Dataset, result.Kind);
        Assert.Equal("S-102", result.Product);
        Assert.True(result.IsChartData);
    }

    [Fact]
    public void File_IsNothing_WhenItsContentNamesNoProduct_EvenWithADatasetExtension()
    {
        var path = _temp.File("US5MA1BO.000", "not a cell");

        var result = Probe().Probe(path);

        Assert.Equal(ContentProbeKind.None, result.Kind);
        Assert.False(result.IsChartData);
    }

    [Fact]
    public void UpdateFile_IsNeverADatasetOnItsOwn()
    {
        var path = _temp.File("US5MA1BO.001", "product:S-57");

        Assert.Equal(ContentProbeKind.None, Probe().Probe(path).Kind);
        Assert.Empty(_detected);
    }

    [Fact]
    public void DetectorThatThrows_MeansNothing()
    {
        var path = _temp.File("broken.h5", "product:S-104");
        var probe = new ContentProbe(_ => throw new InvalidOperationException("malformed"));

        Assert.Equal(ContentProbeKind.None, probe.Probe(path).Kind);
    }

    [Fact]
    public void MissingPath_IsNothing()
    {
        var result = Probe().Probe(Path.Combine(_temp.Path, "gone.h5"));

        Assert.Equal(ContentProbeKind.None, result.Kind);
    }

    [Fact]
    public void S100Catalogue_IsAnExchangeSet_ByItsRootElement()
    {
        var path = _temp.File("set/CATALOG.XML", S100Catalogue);

        Assert.Equal(ContentProbeKind.ExchangeSet, Probe().Probe(path).Kind);
    }

    [Fact]
    public void CatalogueNamedXml_ThatIsntAnS100Catalogue_IsNothing()
    {
        var path = _temp.File("other/CATALOG.XML", "<catalog><book /></catalog>");

        Assert.Equal(ContentProbeKind.None, Probe().Probe(path).Kind);
    }

    [Fact]
    public void S57Catalogue_IsAnExchangeSet_ByItsIso8211Leader()
    {
        var path = _temp.File("ENC_ROOT/CATALOG.031", "002013LE1 0900073   6604" + new string(' ', 40));
        var notIso = _temp.File("other/CATALOG.031", "hello, this isn't ISO 8211 at all");

        Assert.Equal(ContentProbeKind.ExchangeSet, Probe().Probe(path).Kind);
        Assert.Equal(ContentProbeKind.None, Probe().Probe(notIso).Kind);
    }

    [Fact]
    public void Zip_IsAnExchangeSet_WhenItHoldsACatalogue()
    {
        var withCatalogue = Path.Combine(_temp.Path, "set.zip");
        using (var zip = ZipFile.Open(withCatalogue, ZipArchiveMode.Create))
        {
            zip.CreateEntry("S100_ROOT/CATALOG.XML");
        }

        var without = Path.Combine(_temp.Path, "photos.zip");
        using (var zip = ZipFile.Open(without, ZipArchiveMode.Create))
        {
            zip.CreateEntry("photo.jpg");
        }

        Assert.Equal(ContentProbeKind.ExchangeSet, Probe().Probe(withCatalogue).Kind);
        Assert.Equal(ContentProbeKind.None, Probe().Probe(without).Kind);
    }

    [Fact]
    public void Folder_WithACatalogue_IsAnExchangeSet()
    {
        _temp.File("set/CATALOG.XML", S100Catalogue);
        _temp.File("set/S100_ROOT/S-101/DATASET_FILES/101AA00DS0003.000", "product:S-101");

        var result = Probe().Probe(Path.Combine(_temp.Path, "set"));

        Assert.Equal(ContentProbeKind.ExchangeSet, result.Kind);
        Assert.Empty(_detected);
    }

    [Fact]
    public void Folder_HoldingAnExchangeSetBelowIt_IsAFolder()
    {
        _temp.File("charts/ENC_ROOT/CATALOG.031", "002013LE1 0900073   6604" + new string(' ', 40));

        var result = Probe().Probe(Path.Combine(_temp.Path, "charts"));

        Assert.Equal(ContentProbeKind.Folder, result.Kind);
    }

    [Fact]
    public void Folder_WithADatasetBelowIt_IsAFolder_WithThatProduct()
    {
        _temp.File("data/readme.txt", "notes");
        _temp.File("data/2026/11/111US00_X.h5", "product:S-111");

        var result = Probe().Probe(Path.Combine(_temp.Path, "data"));

        Assert.Equal(ContentProbeKind.Folder, result.Kind);
        Assert.Equal("S-111", result.Product);
        Assert.Equal(Path.Combine(_temp.Path, "data"), result.Path);
    }

    [Fact]
    public void Folder_WithoutChartData_IsNothing()
    {
        _temp.File("pictures/a.jpg", "jpeg");
        _temp.File("pictures/b/c.png", "png");

        Assert.Equal(ContentProbeKind.None, Probe().Probe(Path.Combine(_temp.Path, "pictures")).Kind);
    }

    [Fact]
    public void Folder_WithDataDeeperThanTheProbeLooks_IsNothing()
    {
        _temp.File("deep/a/b/c/d/cell.000", "product:S-57");

        var result = Probe(maxDepth: 3).Probe(Path.Combine(_temp.Path, "deep"));

        Assert.Equal(ContentProbeKind.None, result.Kind);
    }

    [Fact]
    public void Folder_StopsAfterMaxFiles()
    {
        for (var i = 0; i < 5; i++)
        {
            _temp.File($"many/file{i}.txt", "text");
        }

        _temp.File("many/sub/cell.000", "product:S-57");

        Assert.Equal(ContentProbeKind.None, Probe(maxFiles: 4).Probe(Path.Combine(_temp.Path, "many")).Kind);
        Assert.Equal(4, _detected.Count);
        Assert.Equal(ContentProbeKind.Folder, Probe(maxFiles: 10).Probe(Path.Combine(_temp.Path, "many")).Kind);
    }

    [Fact]
    public void Folder_StopsWhenItsTimeBudgetIsSpent()
    {
        for (var i = 0; i < 5; i++)
        {
            _temp.File($"slow/file{i}.txt", "text");
        }

        _temp.File("slow/z/cell.000", "product:S-57");
        var clock = new SteppingTimeProvider(TimeSpan.FromMilliseconds(100));

        var result = new ContentProbe(Detect, budget: TimeSpan.FromMilliseconds(250), timeProvider: clock)
            .Probe(Path.Combine(_temp.Path, "slow"));

        Assert.Equal(ContentProbeKind.None, result.Kind);
        Assert.True(_detected.Count < 5);
    }

    [Fact]
    public void Probe_IsCached_UntilThePathChanges()
    {
        var path = _temp.File("cell.h5", "product:S-102");
        var probe = Probe();

        probe.Probe(path);
        probe.Probe(path);
        Assert.Single(_detected);

        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1));
        probe.Probe(path);
        Assert.Equal(2, _detected.Count);
    }

    [Fact]
    public void ChartData_KeepsOnlyItemsWithChartData_OnceEach_InOrder()
    {
        var cell = _temp.File("b.000", "product:S-101");
        var text = _temp.File("notes.txt", "text");
        var folder = _temp.Folder("set");
        _temp.File("set/CATALOG.XML", S100Catalogue);

        var targets = Probe().ChartData([cell, text, folder, cell.ToUpperInvariant(), " "]);

        Assert.Equal([cell, folder], targets.Select(t => t.Path));
        Assert.Equal([ContentProbeKind.Dataset, ContentProbeKind.ExchangeSet], targets.Select(t => t.Kind));
    }

    [Fact]
    public void OpenWithTargets_HoldsWhatTheMenuLastOffered()
    {
        var cell = _temp.File("c.000", "product:S-57");
        OpenWithTargets.Set(Probe().ChartData([cell]));
        Assert.Equal([cell], OpenWithTargets.Paths);

        OpenWithTargets.Set([]);
        Assert.Empty(OpenWithTargets.Paths);
    }

    [Theory]
    [InlineData("S57/US5MA1BO/US5MA1BO.000", "S-57")]
    [InlineData("S101/S-101/DATASET_FILES/101AA00DS0003.000", "S-101")]
    [InlineData("S102/102US004MI1CI262227.h5", "S-102")]
    [InlineData("S104/104US004SC1BO_20251217T12Z.h5", "S-104")]
    [InlineData("S124/navwarn_ccg_ed2.gml", "S-124")]
    public void RealDataset_IsDetectedByEncDotNetS100(string relativePath, string product)
    {
        var path = Upstream(relativePath);

        var result = new ContentProbe().Probe(path);

        Assert.Equal(ContentProbeKind.Dataset, result.Kind);
        Assert.Equal(product, result.Product);
    }

    [Fact]
    public void RealExchangeSets_AreExchangeSets()
    {
        var s100 = Path.GetDirectoryName(Upstream("S101/CATALOG.XML"))!;
        var probe = new ContentProbe();

        Assert.Equal(ContentProbeKind.ExchangeSet, probe.Probe(s100).Kind);
        Assert.Equal(ContentProbeKind.ExchangeSet, probe.Probe(Path.Combine(s100, "CATALOG.XML")).Kind);
    }

    private static string Upstream(string relativePath)
    {
        var path = Fixtures.Upstream(relativePath);
        Assert.SkipWhen(path is null, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        return path!;
    }

    private ContentProbe Probe(int maxFiles = ContentProbe.DefaultMaxFiles, int maxDepth = ContentProbe.DefaultMaxDepth) =>
        new(Detect, maxFiles, maxDepth, budget: TimeSpan.FromHours(1));

    /// <summary>A fake detector: a file is a dataset when it starts <c>product:</c>.</summary>
    private string? Detect(string path)
    {
        _detected.Add(path);
        var text = File.ReadAllText(path);
        return text.StartsWith("product:", StringComparison.Ordinal) ? text["product:".Length..] : null;
    }

    /// <summary>A clock that moves on by <c>step</c> each time it's read.</summary>
    private sealed class SteppingTimeProvider(TimeSpan step) : TimeProvider
    {
        private long _ticks;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _ticks += step.Ticks;
    }
}
