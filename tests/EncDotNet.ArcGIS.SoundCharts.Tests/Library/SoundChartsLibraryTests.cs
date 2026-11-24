using EncDotNet.S100.Collections;
using EncDotNet.S100.Collections.Library;
using EncDotNet.SoundCharts.Library;

namespace EncDotNet.SoundCharts.Tests.Library;

public class SoundChartsLibraryTests
{
    [Fact]
    public async Task Import_Folder_AddsACollectionNamedAfterIt()
    {
        using var temp = new TempFolder();
        var charts = temp.Folder("Charts");
        using var library = Create(temp);

        var import = await library.ImportAsync([charts], cancellationToken: TestContext.Current.CancellationToken);

        var added = Assert.Single(import.Added);
        Assert.Empty(import.Problems);
        Assert.Equal(LibrarySourceKind.Folder, added.Kind);
        var collection = Assert.Single(library.Collections);
        Assert.Equal("Charts", collection.Definition.Name);
        var source = Assert.IsType<LocalFolderSource>(Assert.Single(collection.Sources).Definition);
        Assert.Equal(charts, source.Path, ignoreCase: true);
    }

    [Fact]
    public async Task Import_ReportsMissingAndRepeatedPaths()
    {
        using var temp = new TempFolder();
        var charts = temp.Folder("Charts");
        var inside = temp.Folder(Path.Combine("Charts", "Inside"));
        using var library = Create(temp);
        await library.ImportAsync([charts], cancellationToken: TestContext.Current.CancellationToken);

        var import = await library.ImportAsync(
            [charts, inside, Path.Combine(temp.Path, "Nowhere")],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(import.Added);
        Assert.Collection(
            import.Problems,
            p => Assert.Equal(LibraryImportProblemKind.AlreadyInLibrary, p.Kind),
            p => Assert.Equal(LibraryImportProblemKind.AlreadyInLibrary, p.Kind),
            p => Assert.Equal(LibraryImportProblemKind.NotFound, p.Kind));
        Assert.Single(library.Collections);
    }

    [Fact]
    public async Task Import_SeveralPaths_GoToOneCollectionNamedAfterTheirFolder()
    {
        using var temp = new TempFolder();
        var a = temp.Folder(Path.Combine("Region", "A"));
        var b = temp.Folder(Path.Combine("Region", "B"));
        using var library = Create(temp);

        var import = await library.ImportAsync([a, b], cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, import.Added.Count);
        var collection = Assert.Single(library.Collections);
        Assert.Equal("Region", collection.Definition.Name);
        Assert.Equal(2, collection.Sources.Count);
    }

    [Fact]
    public async Task Import_IntoACollection_AddsASource()
    {
        using var temp = new TempFolder();
        using var library = Create(temp);
        var first = await library.ImportAsync([temp.Folder("One")], cancellationToken: TestContext.Current.CancellationToken);

        var second = await library.ImportAsync([temp.Folder("Two")], first.CollectionId, TestContext.Current.CancellationToken);

        Assert.Equal(first.CollectionId, second.CollectionId);
        Assert.Equal(2, Assert.Single(library.Collections).Sources.Count);
    }

    [Fact]
    public async Task Import_CatalogueFile_IsAnExchangeSet()
    {
        using var temp = new TempFolder();
        var catalogue = temp.File(Path.Combine("Set", "CATALOG.031"));
        using var library = Create(temp);

        var import = await library.ImportAsync([catalogue], cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(LibrarySourceKind.ExchangeSet, Assert.Single(import.Added).Kind);
        Assert.True(library.Contains(catalogue));
    }

    [Fact]
    public async Task Import_CollectionManifest_AddsItsGroups()
    {
        using var temp = new TempFolder();
        temp.Folder("AU");
        var manifest = temp.File(
            "charts.s100collection.json",
            """{ "format": "encdotnet-s100-collection", "version": 1, "title": "My charts", "groups": [ { "id": "AU", "paths": ["AU"] } ] }""");
        using var library = Create(temp);

        var import = await library.ImportAsync([manifest], cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(LibrarySourceKind.LocalManifest, Assert.Single(import.Added).Kind);
        Assert.IsType<LocalManifestSource>(Assert.Single(Assert.Single(library.Collections).Sources).Definition);
    }

    [Fact]
    public async Task Import_ManifestThatIsNotValid_IsUnreadable()
    {
        using var temp = new TempFolder();
        var manifest = temp.File("broken.s100collection.json", "{ not json");
        using var library = Create(temp);

        var import = await library.ImportAsync([manifest], cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(import.Added);
        Assert.Equal(LibraryImportProblemKind.Unreadable, Assert.Single(import.Problems).Kind);
        Assert.Empty(library.Collections);
    }

    [Fact]
    public async Task Collections_ArePersisted()
    {
        using var temp = new TempFolder();
        var charts = temp.Folder("Charts");
        using (var library = Create(temp))
        {
            await library.ImportAsync([charts], cancellationToken: TestContext.Current.CancellationToken);
        }

        using var reopened = Create(temp);
        reopened.Initialize();

        Assert.Equal("Charts", Assert.Single(reopened.Collections).Definition.Name);
        Assert.True(reopened.Contains(charts));
    }

    [Fact]
    public async Task Remove_SourceOrCollection()
    {
        using var temp = new TempFolder();
        using var library = Create(temp);
        var import = await library.ImportAsync([temp.Folder("A")], cancellationToken: TestContext.Current.CancellationToken);
        var collection = import.CollectionId!.Value;
        await library.ImportAsync([temp.Folder("B")], collection, TestContext.Current.CancellationToken);

        Assert.True(library.Remove(collection, import.Added[0].SourceId));
        Assert.Single(Assert.Single(library.Collections).Sources);
        Assert.True(library.Remove(collection));
        Assert.Empty(library.Collections);
    }

    [Fact]
    public async Task Index_S57Folder_ListsTheCellWithoutLoadingIt()
    {
        var cell = Fixtures.Upstream(Path.Combine("S57", "US5MA1BO", "US5MA1BO.000"));
        Assert.SkipWhen(cell is null, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        using var temp = new TempFolder();
        var folder = temp.Folder("Charts");
        File.Copy(cell, Path.Combine(folder, "US5MA1BO.000"));
        using var library = Create(temp);

        await library.ImportAsync([folder], cancellationToken: TestContext.Current.CancellationToken);
        await library.WhenIdle();

        var source = Assert.Single(Assert.Single(library.Collections).Sources);
        var item = Assert.Single(source.Index!.Items);
        Assert.Equal("US5MA1BO", item.Name);
        Assert.Equal("S-57", item.ProductSpec);
        Assert.NotNull(item.Edition);
        Assert.Equal(Path.Combine(folder, "US5MA1BO.000"), LibraryItemPaths.DatasetFile(item), ignoreCase: true);
    }

    [Fact]
    public async Task Index_S101ExchangeSet_HasFootprints()
    {
        var catalogue = Fixtures.Upstream(Path.Combine("S101", "CATALOG.XML"));
        Assert.SkipWhen(catalogue is null, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        using var temp = new TempFolder();
        using var library = Create(temp);

        await library.ImportAsync([Path.GetDirectoryName(catalogue)!], cancellationToken: TestContext.Current.CancellationToken);
        await library.WhenIdle();

        var items = Assert.Single(Assert.Single(library.Collections).Sources).Index!.Items;
        Assert.NotEmpty(items);
        Assert.All(items, i => Assert.Equal("S-101", i.ProductSpec));

        // This catalogue gives no bounds, so Show Footprint reads the dataset's own extent.
        using var reader = new LibraryDatasetReader();
        var footprints = items
            .Select(i => LibraryFootprint.Of(i) ?? reader.ReadFootprint(LibraryItemPaths.DatasetFile(i)!))
            .ToList();
        Assert.Contains(footprints, f => f is not null);
    }

    [Fact]
    public async Task Index_LooseHdf5Dataset_IsReadByTheProbe()
    {
        var dataset = Fixtures.Upstream(Path.Combine("S104", "104US004SC1BO_20251217T12Z.h5"));
        Assert.SkipWhen(dataset is null, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        using var temp = new TempFolder();
        using var reader = new LibraryDatasetReader();
        using var library = new SoundChartsLibrary(temp.Folder("library"), reader.Probe);
        library.Initialize();

        await library.ImportAsync([dataset], cancellationToken: TestContext.Current.CancellationToken);
        await library.WhenIdle();

        var item = Assert.Single(Assert.Single(Assert.Single(library.Collections).Sources).Index!.Items);
        Assert.Equal("S-104", item.ProductSpec);
        Assert.NotNull(item.Bounds);
    }

    [Fact]
    public async Task Index_AdvisoryZip_IsOneTropicalCycloneDataset()
    {
        // Issue #63: an NHC advisory's forecast zip is a dataset, not an exchange set without a catalogue.
        var zip = Fixtures.Upstream(Path.Combine("TC", "al092022", "al092022_5day_020.zip"));
        Assert.SkipWhen(zip is null, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        using var temp = new TempFolder();

        var index = await IndexAsync(temp, zip);

        var item = Assert.Single(index.Items);
        Assert.Equal("TC", item.ProductSpec);
        Assert.Equal("al092022_5day_020", item.Name);
        Assert.Equal("al092022_5day_020.zip", item.Key);
        Assert.Equal(Path.GetFullPath(zip), LibraryItemPaths.AddToMapPath(item));
        Assert.Equal(Path.GetFullPath(zip), LibraryItemPaths.DatasetFile(item));
        Assert.DoesNotContain(index.Diagnostics, d => string.Equals(d.Path, Path.GetFullPath(zip), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Index_AdvisoryFolder_IsOneItemAtTheFolder()
    {
        // Issue #69: EncDotNet.S100 lists a folder of one storm's zips as one
        // item located at the folder, keyed like a GRIB2 run. The wind radii
        // and best track zips aren't items of their own.
        using var temp = new TempFolder();
        var folder = Path.Combine(temp.Path, "ian");
        Assert.SkipUnless(Fixtures.CopyUpstreamStorm(folder), $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");

        var index = await IndexAsync(temp, folder);

        var item = Assert.Single(index.Items);
        Assert.Equal("TC", item.ProductSpec);
        Assert.Equal("ian/", item.Key);
        Assert.Equal("ian", item.Name);
        Assert.NotNull(item.Bounds);
        Assert.True(item.Properties.ContainsKey("timeStart"));
        Assert.Equal(folder, LibraryItemPaths.AddToMapPath(item), ignoreCase: true);
        Assert.Equal(folder, LibraryItemPaths.DatasetFile(item), ignoreCase: true);
        Assert.True(LibraryItemPaths.Exists(LibraryItemPaths.AddToMapPath(item)));
    }

    [Fact]
    public async Task Index_FolderWithAnAdvisorySubfolder_ListsTheSubfolderOnce()
    {
        using var temp = new TempFolder();
        var root = temp.Folder("charts");
        var folder = Path.Combine(root, "ian");
        Assert.SkipUnless(Fixtures.CopyUpstreamStorm(folder), $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");

        var index = await IndexAsync(temp, root);

        var item = Assert.Single(index.Items);
        Assert.Equal("TC", item.ProductSpec);
        Assert.Equal("ian/", item.Key);
        Assert.Equal(folder, LibraryItemPaths.AddToMapPath(item), ignoreCase: true);
    }

    [Fact]
    public async Task Index_AdvisoryBesideAGrib2Run_IsTheStormsLatestForecastZip()
    {
        // A folder that is a GRIB2 run keeps the folder for the run, so the
        // advisory is its latest forecast zip, which supersedes older ones.
        var grib = Fixtures.Upstream(Path.Combine("GRIB2", "gfswave-useast.grib2"));
        var filter = Fixtures.Upstream(Path.Combine("GRIB2", "gfswave-useast-filter.grib2"));
        Assert.SkipWhen(grib is null || filter is null, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        using var temp = new TempFolder();
        var folder = temp.Folder("run");
        Assert.SkipUnless(Fixtures.CopyUpstreamStorm(folder, advisory: "019"), "No TC fixture.");
        Fixtures.CopyUpstreamStorm(folder);
        File.Copy(grib, Path.Combine(folder, "gfswave-useast.grib2"));
        File.Copy(filter, Path.Combine(folder, "gfswave-useast-filter.grib2"));

        var index = await IndexAsync(temp, folder);

        var storm = Assert.Single(index.Items, i => i.ProductSpec == "TC");
        Assert.Equal("al092022_5day_020.zip", storm.Key);
        Assert.Equal(Path.Combine(folder, "al092022_5day_020.zip"), LibraryItemPaths.AddToMapPath(storm), ignoreCase: true);

        // The add-in reads GRIB2 runs file by file, so the run is its files.
        Assert.Equal(
            ["gfswave-useast-filter.grib2", "gfswave-useast.grib2"],
            index.Items.Where(i => i.ProductSpec == "GRIB2").Select(i => i.Key).Order(StringComparer.Ordinal));
        Assert.Equal(3, index.Items.Count);
    }

    [Fact]
    public async Task Index_SeveralStormsInOneFolder_ListsEachStormsLatestForecastZip()
    {
        using var temp = new TempFolder();
        var folder = Path.Combine(temp.Path, "storms");
        Assert.SkipUnless(Fixtures.CopyUpstreamStorm(folder, advisory: "019"), $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        Fixtures.CopyUpstreamStorm(folder);
        Fixtures.CopyUpstreamStorm(folder, atcfId: "ep012022", advisory: "003");

        var index = await IndexAsync(temp, folder);

        Assert.All(index.Items, i => Assert.Equal("TC", i.ProductSpec));
        Assert.Equal(
            ["al092022_5day_020.zip", "ep012022_5day_003.zip"],
            index.Items.Select(i => i.Key).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Index_ManifestGroupWithAnAdvisoryFolder_ListsTheFolder()
    {
        // The hurricane showcase's layout: a Storm group holding one advisory's folder.
        using var temp = new TempFolder();
        var folder = Path.Combine(temp.Path, "Storm", "al092022_020");
        Assert.SkipUnless(Fixtures.CopyUpstreamStorm(folder), $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        var manifest = temp.File(
            "al092022.s100collection.json",
            """{ "format": "encdotnet-s100-collection", "version": 1, "title": "Ian", "groups": [ { "id": "storm", "name": "Storm", "paths": ["Storm"] } ] }""");

        var index = await IndexAsync(temp, manifest);

        var item = Assert.Single(index.Items);
        Assert.Equal("TC", item.ProductSpec);
        Assert.Equal(folder, LibraryItemPaths.AddToMapPath(item), ignoreCase: true);
    }

    [Fact]
    public async Task Index_WindRadiiAndBestTrackZipsWithoutAForecast_AreNotItems()
    {
        using var temp = new TempFolder();
        var folder = Path.Combine(temp.Path, "ian");
        Assert.SkipUnless(Fixtures.CopyUpstreamStorm(folder), $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        File.Delete(Path.Combine(folder, "al092022_5day_020.zip"));

        var index = await IndexAsync(temp, folder);

        Assert.Empty(index.Items);
    }

    /// <summary>Indexes <paramref name="path"/> in a new library with the add-in's probe, as Pro does.</summary>
    private static async Task<SourceIndex> IndexAsync(TempFolder temp, string path)
    {
        using var reader = new LibraryDatasetReader();
        using var library = new SoundChartsLibrary(temp.Folder("library"), reader.Probe);
        library.Initialize();

        await library.ImportAsync([path], cancellationToken: TestContext.Current.CancellationToken);
        await library.WhenIdle();

        return Assert.Single(Assert.Single(library.Collections).Sources).Index!;
    }

    private static SoundChartsLibrary Create(TempFolder temp)
    {
        var library = new SoundChartsLibrary(Path.Combine(temp.Path, "library"));
        library.Initialize();
        return library;
    }
}
