using EncDotNet.S100;
using EncDotNet.S100.Datasets.Pipelines;
using EncDotNet.SoundCharts.ChartLayers;

namespace EncDotNet.SoundCharts.Tests.ChartLayers;

/// <summary>
/// Content detection (issue #5): the product, edition and footprint come from
/// each dataset's content, through EncDotNet.S100, not from its extension.
/// Uses EncDotNet.S100's test datasets when <see cref="Fixtures.UpstreamDatasetsVariable"/>
/// is set, and skips otherwise.
/// </summary>
public sealed class DatasetInspectorTests : IDisposable
{
    private readonly TempFolder _temp = new();
    private readonly DatasetInspector _inspector;

    public DatasetInspectorTests()
    {
        _inspector = new DatasetInspector(Path.Combine(_temp.Path, "extracted"));
    }

    public void Dispose()
    {
        _inspector.Dispose();
        _temp.Dispose();
    }

    [Theory]
    [InlineData("S57/US5MA1BO/US5MA1BO.000", "S-57", ChartLayerKind.Chart, 42.0, -71.0)]
    [InlineData("S102/102US004MI1CI262227.h5", "S-102", ChartLayerKind.Chart, double.NaN, double.NaN)]
    [InlineData("S104/104US004SC1BO_20251217T12Z.h5", "S-104", ChartLayerKind.WaterLevel, 32.0, -80.0)]
    [InlineData("S111/111US00_DBOFS_20260320T18Z_US4DE1BB.h5", "S-111", ChartLayerKind.SurfaceCurrents, double.NaN, double.NaN)]
    [InlineData("S124/navwarn_ccg_ed2.gml", "S-124", ChartLayerKind.NavigationalWarnings, double.NaN, double.NaN)]
    public async Task Dataset_ProductAndLayerComeFromTheContent(string relativePath, string product, ChartLayerKind kind, double latitude, double longitude)
    {
        var path = Upstream(relativePath);

        var inspection = await _inspector.InspectAsync(path, cancellationToken: TestContext.Current.CancellationToken);

        var dataset = Assert.Single(inspection.Datasets);
        Assert.Equal(product, dataset.Product);
        Assert.Equal(kind, dataset.Kind);
        Assert.Equal(Path.GetFullPath(path), dataset.Path);
        Assert.Equal(Path.GetFileNameWithoutExtension(path), inspection.Input);
        Assert.NotNull(dataset.Extent);
        Assert.True(dataset.Extent!.Value.IsValid);
        if (!double.IsNaN(latitude))
        {
            // Within a degree or so: the footprint is the dataset's own, not a union.
            Assert.InRange(latitude, dataset.Extent.Value.South - 1, dataset.Extent.Value.North + 1);
            Assert.InRange(longitude, dataset.Extent.Value.West - 1, dataset.Extent.Value.East + 1);
        }
    }

    [Theory]
    [InlineData("GRIB2/gfswave-useast.grib2", "GRIB2", ChartLayerKind.Weather)]
    [InlineData("GRIB2/gfs-1p00-ugrd10m-f003.grib2", "GRIB2", ChartLayerKind.Chart)]
    [InlineData("CAP/nws-gale-warning-pzz560.xml", "CAP", ChartLayerKind.Weather)]
    [InlineData("S412/synthetic_all_warnings.gml", "S-412", ChartLayerKind.Weather)]
    [InlineData("S412/4122C00_TESTDS1.GML", "S-412", ChartLayerKind.Chart)]
    [InlineData("S411/iho_4112C00TDS001.gml", "S-411", ChartLayerKind.Chart)]
    [InlineData("S104/104US004SC1BO_20251217T12Z.h5", "S-104", ChartLayerKind.WaterLevel)]
    [InlineData("S102/102US004MI1CI262227.h5", "S-102", ChartLayerKind.Chart)]
    [InlineData("S124/navwarn_ccg_ed2.gml", "S-124", ChartLayerKind.NavigationalWarnings)]
    public async Task Dataset_WithMoreThanOneTimeStep_GoesInATimeLayer(string relativePath, string product, ChartLayerKind kind)
    {
        // Issue #62: the layer follows the dataset's time steps, not its product. One step stays in Chart.
        var inspection = await _inspector.InspectAsync(Upstream(relativePath), cancellationToken: TestContext.Current.CancellationToken);

        var dataset = Assert.Single(inspection.Datasets);
        Assert.Equal(product, dataset.Product);
        Assert.Equal(kind, dataset.Kind);
        Assert.Equal(ChartLayerKinds.IsTimeLayer(kind), dataset.TimeVarying);
    }

    [Fact]
    public void IsTimeVarying_ATropicalCycloneAdvisory_IsTimeVarying()
    {
        // Hurricane Ian, advisory 20: eight forecast points. Read through the processor,
        // as Inspect does, since adding the .zip itself is issue #63's.
        using var factory = BundledFactory();
        var processor = ((IDatasetProcessorFactory)factory).CreateProcessor(Upstream("TC/al092022/al092022_5day_020.zip"));
        try
        {
            Assert.True(DatasetInspector.IsTimeVarying(processor));
        }
        finally
        {
            (processor as IDisposable)?.Dispose();
        }
    }

    [Fact]
    public void IsTimeVarying_WithoutTimeSteps_IsFalse()
    {
        using var factory = BundledFactory();
        var processor = ((IDatasetProcessorFactory)factory).CreateProcessor(Upstream("S411/cis_seaice_synthetic.gml"));
        try
        {
            Assert.False(DatasetInspector.IsTimeVarying(processor));
        }
        finally
        {
            (processor as IDisposable)?.Dispose();
        }
    }

    [Fact]
    public async Task Dataset_ZippedAdvisory_IsOneTropicalCycloneDataset_NotAnExchangeSet()
    {
        // Issue #63: an NHC advisory's forecast zip is a TC dataset, read in place.
        var path = Upstream("TC/al092022/al092022_5day_020.zip");

        var inspection = await _inspector.InspectAsync(path, cancellationToken: TestContext.Current.CancellationToken);

        var dataset = Assert.Single(inspection.Datasets);
        Assert.Equal("TC", dataset.Product);
        Assert.Equal(Path.GetFullPath(path), dataset.Path);
        Assert.Equal("al092022_5day_020", inspection.Input);
        Assert.Empty(inspection.Warnings);
        Assert.False(Directory.Exists(_inspector.ExtractionFolder) && Directory.EnumerateFileSystemEntries(_inspector.ExtractionFolder).Any());
    }

    [Fact]
    public async Task Folder_AdvisoryFolder_IsOneTropicalCycloneDatasetAtTheFolder()
    {
        // Issue #69: the Library's item for a storm's folder is the folder,
        // so Add to Map passes it. Its zips are read together, as one
        // dataset, and aren't counted as files that aren't datasets.
        var folder = Path.GetDirectoryName(Upstream("TC/al092022/al092022_5day_020.zip"))!;

        var inspection = await _inspector.InspectAsync(folder, cancellationToken: TestContext.Current.CancellationToken);

        var dataset = Assert.Single(inspection.Datasets);
        Assert.Equal("TC", dataset.Product);
        Assert.Equal(Path.GetFullPath(folder), dataset.Path);
        Assert.Equal("al092022", dataset.Name);
        Assert.True(dataset.TimeVarying);
        Assert.Equal(ChartLayerKind.Weather, dataset.Kind);
        Assert.True(dataset.Exists);
        Assert.NotNull(dataset.Extent);
        Assert.Equal("al092022", inspection.Input);
        Assert.Empty(inspection.Warnings);
    }

    [Fact]
    public async Task Folder_AdvisoryFolderWithAChart_FindsTheStormAndTheChart()
    {
        var cell = Upstream("S57/US5MA1BO/US5MA1BO.000");
        var folder = Path.Combine(_temp.Path, "mixed");
        Assert.True(Fixtures.CopyUpstreamStorm(folder));
        File.Copy(cell, Path.Combine(folder, "US5MA1BO.000"));

        var inspection = await _inspector.InspectAsync(folder, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(
            [("S-57", Path.Combine(folder, "US5MA1BO.000")), ("TC", folder)],
            inspection.Datasets.Select(d => (d.Product, d.Path)).OrderBy(d => d.Product, StringComparer.Ordinal));
        Assert.Empty(inspection.Warnings);
    }

    [Fact]
    public async Task Folder_WithAnAdvisorySubfolder_FindsTheSubfolder()
    {
        Upstream("TC/al092022/al092022_5day_020.zip");
        var root = Path.Combine(_temp.Path, "charts");
        var storm = Path.Combine(root, "ian");
        Assert.True(Fixtures.CopyUpstreamStorm(storm));

        var inspection = await _inspector.InspectAsync(root, cancellationToken: TestContext.Current.CancellationToken);

        var dataset = Assert.Single(inspection.Datasets);
        Assert.Equal("TC", dataset.Product);
        Assert.Equal(storm, dataset.Path);
        Assert.Empty(inspection.Warnings);
    }

    [Fact]
    public async Task Folder_WithSeveralStorms_FindsEachForecastZip()
    {
        // Not an advisory folder: each storm's forecast zip is a dataset, as before.
        Upstream("TC/al092022/al092022_5day_020.zip");
        var folder = Path.Combine(_temp.Path, "storms");
        Assert.True(Fixtures.CopyUpstreamStorm(folder));
        Assert.True(Fixtures.CopyUpstreamStorm(folder, atcfId: "ep012022", advisory: "003"));

        var inspection = await _inspector.InspectAsync(folder, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(
            ["al092022_5day_020.zip", "ep012022_5day_003.zip"],
            inspection.Datasets.Select(d => Path.GetFileName(d.Path)).Order(StringComparer.Ordinal));
        Assert.All(inspection.Datasets, d => Assert.Equal("TC", d.Product));
    }

    [Fact]
    public async Task Folder_RunFolder_IsOneGrib2DatasetAtTheFolder()
    {
        // Issue #73: the Library's item for a GRIB2 run is the folder, so Add
        // to Map passes it. Its files are read together, as one dataset with
        // the run's time steps, and its .idx inventories aren't counted as
        // files that aren't datasets.
        var folder = Path.Combine(_temp.Path, "gfswave");
        Assert.SkipWhen(Fixtures.WriteUpstreamRun(folder).Count == 0, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        File.WriteAllText(Path.Combine(folder, "gfswave.t00z.global.0p25.f000.grib2.idx"), "1:0:d=2026102500:HTSGW:surface:anl:");

        var inspection = await _inspector.InspectAsync(folder, cancellationToken: TestContext.Current.CancellationToken);

        var dataset = Assert.Single(inspection.Datasets);
        Assert.Equal("GRIB2", dataset.Product);
        Assert.Equal(Path.GetFullPath(folder), dataset.Path);
        Assert.Equal("gfswave", dataset.Name);
        Assert.True(dataset.TimeVarying);
        Assert.Equal(ChartLayerKind.Weather, dataset.Kind);
        Assert.True(dataset.Exists);
        Assert.NotNull(dataset.Extent);
        Assert.Equal("gfswave", inspection.Input);
        Assert.Empty(inspection.Warnings);
    }

    [Fact]
    public async Task Folder_RunFolderWithAStormAndAChart_IsTheRunTheStormsLatestForecastZipAndTheChart()
    {
        // As in EncDotNet.S100's Library and CLI (#1687, #1694): the run keeps
        // the folder, and the storm is its latest forecast zip. The storm's
        // other zips aren't counted as files that aren't datasets.
        var cell = Upstream("S57/US5MA1BO/US5MA1BO.000");
        var folder = Path.Combine(_temp.Path, "run");
        Assert.SkipWhen(Fixtures.WriteUpstreamRun(folder).Count == 0, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        Assert.True(Fixtures.CopyUpstreamStorm(folder, advisory: "019"));
        Assert.True(Fixtures.CopyUpstreamStorm(folder));
        File.Copy(cell, Path.Combine(folder, "US5MA1BO.000"));

        var inspection = await _inspector.InspectAsync(folder, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(
            [("GRIB2", folder), ("S-57", Path.Combine(folder, "US5MA1BO.000")), ("TC", Path.Combine(folder, "al092022_5day_020.zip"))],
            inspection.Datasets.Select(d => (d.Product, d.Path)).OrderBy(d => d.Product, StringComparer.Ordinal));
        Assert.Empty(inspection.Warnings);
    }

    [Fact]
    public async Task Folder_WithARunSubfolderAndOneGrib2File_FindsTheRunAndTheFile()
    {
        var root = Path.Combine(_temp.Path, "weather");
        var run = Path.Combine(root, "gfswave");
        Assert.SkipWhen(Fixtures.WriteUpstreamRun(run).Count == 0, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        var single = Assert.Single(Fixtures.WriteUpstreamRun(Path.Combine(root, "one"), [6]));

        var inspection = await _inspector.InspectAsync(root, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(
            [(run, true), (single, false)],
            inspection.Datasets.Select(d => (d.Path, d.TimeVarying)).OrderBy(d => d.Path, StringComparer.Ordinal));
        Assert.All(inspection.Datasets, d => Assert.Equal("GRIB2", d.Product));
        Assert.Empty(inspection.Warnings);
    }

    [Fact]
    public async Task Dataset_AdvisoryWindRadiiZipAlone_IsNoDataset()
    {
        var inspection = await _inspector.InspectAsync(Upstream("TC/al092022/al092022_fcst_020.zip"), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(inspection.Datasets);
        Assert.NotEmpty(inspection.Warnings);
    }

    [Fact]
    public async Task Dataset_NamedLikeAnotherProduct_IsWhatItsContentSays()
    {
        // An S-111 file named as if it were S-104: the HDF5 productSpecification decides.
        var copy = Path.Combine(_temp.Path, "104US004SC1BO_20251217T12Z.h5");
        File.Copy(Upstream("S111/111US00_DBOFS_20260320T18Z_US4DE1BB.h5"), copy);

        var inspection = await _inspector.InspectAsync(copy, cancellationToken: TestContext.Current.CancellationToken);

        var dataset = Assert.Single(inspection.Datasets);
        Assert.Equal("S-111", dataset.Product);
        Assert.Equal(ChartLayerKind.SurfaceCurrents, dataset.Kind);
    }

    [Fact]
    public async Task Dataset_Iso8211_TellsS57FromS101ByContent()
    {
        var s57 = await _inspector.InspectAsync(Upstream("S57/US5MA1BO/US5MA1BO.000"), cancellationToken: TestContext.Current.CancellationToken);
        var s101 = await _inspector.InspectAsync(Upstream("S101/S-101/DATASET_FILES/101AA00DS0003.000"), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("S-57", Assert.Single(s57.Datasets).Product);
        Assert.Equal("S-101", Assert.Single(s101.Datasets).Product);
    }

    [Fact]
    public async Task Dataset_S100_ReadsItsEdition()
    {
        var inspection = await _inspector.InspectAsync(Upstream("S104/104US004SC1BO_20251217T12Z.h5"), cancellationToken: TestContext.Current.CancellationToken);

        var dataset = Assert.Single(inspection.Datasets);
        Assert.NotNull(dataset.Edition);
        Assert.StartsWith("2.", dataset.Edition, StringComparison.Ordinal);
    }

    [Fact]
    public async Task File_ThatIsntADataset_IsAWarningNotADataset()
    {
        var inspection = await _inspector.InspectAsync(Upstream("S57/US5MA1BO/US347BOA.TXT"), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(inspection.Datasets);
        Assert.Contains(inspection.Warnings, w => w.Contains("US347BOA.TXT", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Hdf5ThatIsntAProduct_ByExtensionAlone_IsNotADataset()
    {
        var fake = _temp.File("fake.h5", "not HDF5 at all");

        var inspection = await _inspector.InspectAsync(fake, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(inspection.Datasets);
        Assert.Single(inspection.Warnings);
    }

    [Fact]
    public async Task ExchangeSet_Folder_ReadsTheCatalogue()
    {
        var root = Path.GetDirectoryName(Upstream("S101/CATALOG.XML"))!;

        var inspection = await _inspector.InspectAsync(root, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("S101", inspection.Input);
        Assert.True(inspection.Datasets.Count >= 10, $"Found {inspection.Datasets.Count} datasets.");
        Assert.All(inspection.Datasets, d =>
        {
            Assert.Equal("S-101", d.Product);
            Assert.Equal(ChartLayerKind.Chart, d.Kind);
            Assert.NotNull(d.Extent);

            // The catalogue names the producer and marks the test data not for navigation.
            Assert.Equal("AA00", d.Producer);
            Assert.True(d.NotForNavigation);
        });
    }

    [Fact]
    public async Task ExchangeSet_CatalogueFile_IsItsFolder()
    {
        var catalogue = Upstream("S101/CATALOG.XML");

        var byFile = await _inspector.InspectAsync(catalogue, cancellationToken: TestContext.Current.CancellationToken);
        var byFolder = await _inspector.InspectAsync(Path.GetDirectoryName(catalogue)!, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(byFolder.Datasets.Select(d => d.Path), byFile.Datasets.Select(d => d.Path));
    }

    [Fact]
    public async Task ExchangeSet_Zip_IsExtractedOnceAndRead()
    {
        var zip = Upstream("S101.zip");

        var first = await _inspector.InspectAsync(zip, cancellationToken: TestContext.Current.CancellationToken);
        var second = await _inspector.InspectAsync(zip, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("S101", first.Input);
        Assert.NotEmpty(first.Datasets);
        Assert.All(first.Datasets, d => Assert.StartsWith(_inspector.ExtractionFolder, d.Path, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(first.Datasets.Select(d => d.Path), second.Datasets.Select(d => d.Path));
        Assert.Single(Directory.GetDirectories(_inspector.ExtractionFolder));
    }

    [Fact]
    public async Task ExchangeSet_CatalogueSaysAnotherProduct_Warns()
    {
        var root = CopyS101ExchangeSet(catalogue => ReplaceFirst(
            catalogue,
            "<S100XC:productIdentifier>S-101</S100XC:productIdentifier>",
            "<S100XC:productIdentifier>S-102</S100XC:productIdentifier>"));

        var inspection = await _inspector.InspectAsync(root, cancellationToken: TestContext.Current.CancellationToken);

        // Drawn as what the content says, with a warning naming both.
        Assert.All(inspection.Datasets, d => Assert.Equal("S-101", d.Product));
        Assert.Contains(inspection.Warnings, w => w.Contains("S-102", StringComparison.Ordinal) && w.Contains("S-101", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExchangeSet_CatalogueSaysAnotherEdition_Warns()
    {
        var root = CopyS101ExchangeSet(catalogue => ReplaceFirst(
            catalogue,
            "<S100XC:productIdentifier>S-101</S100XC:productIdentifier>",
            "<S100XC:name>S-101</S100XC:name><S100XC:version>9.9.0</S100XC:version><S100XC:productIdentifier>S-101</S100XC:productIdentifier>"));

        var inspection = await _inspector.InspectAsync(root, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(inspection.Warnings, w => w.Contains("S-101 9.9.0", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExchangeSet_MissingDataset_Warns()
    {
        var root = CopyS101ExchangeSet(catalogue => catalogue);
        var first = Directory.GetFiles(root, "*.000", SearchOption.AllDirectories).Order(StringComparer.Ordinal).First();
        File.Delete(first);

        var inspection = await _inspector.InspectAsync(root, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(inspection.Warnings, w => w.Contains(Path.GetFileName(first), StringComparison.Ordinal));
        Assert.DoesNotContain(inspection.Datasets, d => d.Path.Equals(first, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Folder_FindsDatasetsByContentAndCountsTheRest()
    {
        var folder = _temp.Folder("mixed");
        File.Copy(Upstream("S57/US5MA1BO/US5MA1BO.000"), Path.Combine(folder, "US5MA1BO.000"));
        File.Copy(Upstream("S104/104US004SC1BO_20251217T12Z.h5"), Path.Combine(folder, "tide.h5"));
        File.WriteAllText(Path.Combine(folder, "readme.txt"), "not a dataset");
        File.WriteAllText(Path.Combine(folder, "US5MA1BO.001"), "an update file, applied with its base cell");

        var inspection = await _inspector.InspectAsync(folder, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("mixed", inspection.Input);
        Assert.Equal(["S-104", "S-57"], inspection.Datasets.Select(d => d.Product).Order(StringComparer.Ordinal));
        Assert.Contains(inspection.Warnings, w => w.Contains('1', StringComparison.Ordinal) && w.Contains("mixed", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MissingPath_Throws()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _inspector.InspectAsync(Path.Combine(_temp.Path, "nothing-here.000"), cancellationToken: TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(@"C:\a\US5MA1BO.000", false)]
    [InlineData(@"C:\a\US5MA1BO.001", true)]
    [InlineData(@"C:\a\US5MA1BO.123", true)]
    [InlineData(@"C:\a\tide.h5", false)]
    [InlineData(@"C:\a\CATALOG.031", true)]
    public void IsUpdateFile(string path, bool expected)
    {
        // CATALOG.031 looks like an update by its name; the inspector reads catalogues before it scans.
        Assert.Equal(expected, DatasetInspector.IsUpdateFile(path));
    }

    [Theory]
    [InlineData(@"C:\a\US5MA1BO.000", "US")]
    [InlineData(@"C:\a\101AA00DS0003.000", "AA")]
    [InlineData(@"C:\a\104US004SC1BO_20251217T12Z.h5", "US")]
    [InlineData(@"C:\a\navwarn_ccg_ed2.gml", null)]
    [InlineData(@"C:\a\tide.h5", null)]
    public void ProducerCodeFromName(string path, string? expected)
    {
        Assert.Equal(expected, DatasetInspector.ProducerCodeFromName(path));
    }

    private static IDisposable BundledFactory() => (IDisposable)BundledDatasetProcessorFactory.Create();

    private static string Upstream(string relativePath)
    {
        var path = Fixtures.Upstream(relativePath);
        Assert.SkipWhen(path is null, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        return path!;
    }

    private string CopyS101ExchangeSet(Func<string, string> editCatalogue)
    {
        var source = Path.GetDirectoryName(Upstream("S101/CATALOG.XML"))!;
        var target = _temp.Folder("S101-copy");
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var copy = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
            File.Copy(file, copy);
        }

        var catalogue = Path.Combine(target, "CATALOG.XML");
        File.WriteAllText(catalogue, editCatalogue(File.ReadAllText(catalogue)));
        return target;
    }

    private static string ReplaceFirst(string text, string oldValue, string newValue)
    {
        var index = text.IndexOf(oldValue, StringComparison.Ordinal);
        Assert.True(index >= 0, $"'{oldValue}' isn't in the catalogue.");
        return string.Concat(text.AsSpan(0, index), newValue, text.AsSpan(index + oldValue.Length));
    }
}
