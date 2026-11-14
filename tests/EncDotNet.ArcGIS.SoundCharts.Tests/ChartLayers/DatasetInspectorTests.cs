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
    [InlineData("S124/navwarn_ccg_ed2.gml", "S-124", ChartLayerKind.Chart, double.NaN, double.NaN)]
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
