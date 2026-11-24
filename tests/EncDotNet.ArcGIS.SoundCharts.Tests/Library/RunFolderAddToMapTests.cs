using System.Net;
using System.Runtime.InteropServices;
using EncDotNet.S100.Collections;
using EncDotNet.SoundCharts.ChartLayers;
using EncDotNet.SoundCharts.Charts;
using EncDotNet.SoundCharts.FeatureLayers;
using EncDotNet.SoundCharts.Library;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts.Tests.Library;

/// <summary>
/// Follows a Library item for a GRIB2 model run folder (issue #73) through
/// <b>Add to Map</b> as the add-in does in Pro, without Pro: the path it
/// passes, the inspection, the layer's marker, the server's inputs, the
/// revision, the footprint and the Library details. Uses EncDotNet.S100's
/// GFS-Wave fixture split into one file per forecast hour, and the bundled
/// chart server when it's built.
/// </summary>
public sealed class RunFolderAddToMapTests : IDisposable
{
    private static readonly DateTime[] RunTimes =
    [
        new(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc),
        new(2026, 10, 25, 3, 0, 0, DateTimeKind.Utc),
        new(2026, 10, 25, 6, 0, 0, DateTimeKind.Utc),
    ];

    private readonly TempFolder _temp = new();
    private readonly string _folder;

    public RunFolderAddToMapTests()
    {
        _folder = Path.Combine(_temp.Path, "gfswave");
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task AddToMap_ARunFolderItem_IsOneRunInTheWeatherLayer()
    {
        var item = Assert.Single(await IndexAsync(withStorm: false));

        // Add to Map passes the folder, which is there.
        var path = Assert.Single(LibraryItemPaths.AddToMapPaths([item]));
        Assert.Equal(_folder, path, ignoreCase: true);
        Assert.True(LibraryItemPaths.Exists(path));

        // Add SoundCharts Data finds one GRIB2 dataset at the folder, with the run's steps.
        using var inspector = new DatasetInspector(Path.Combine(_temp.Path, "extracted"));
        var inspection = await inspector.InspectAsync(path, cancellationToken: Token);
        var dataset = Assert.Single(inspection.Datasets);
        Assert.Empty(inspection.Warnings);
        Assert.Equal("GRIB2", dataset.Product);
        Assert.Equal(ChartLayerKind.Weather, dataset.Kind);

        // The layer's marker keeps the folder, and the server is given the folder with --only GRIB2.
        var marker = ChartLayerMarker.TryParse(ChartLayerMarker.ForLayer(dataset.Kind, [dataset]).ToJson());
        Assert.NotNull(marker);
        Assert.Equal(_folder, Assert.Single(marker.Datasets).Path, ignoreCase: true);
        Assert.True(Assert.Single(marker.Datasets).Exists);
        var set = marker.ToDatasetSet();
        Assert.Equal([_folder, "--only", "GRIB2"], ServerCommandLine.Inputs(set, Path.Combine(_temp.Path, "work")));

        // The revision covers every file in the folder.
        Assert.Equal(3, DataRevision.Of(set.Sources).Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);

        // The Coverage layer's footprint spans the run.
        using var footprints = new FootprintReader();
        var footprint = footprints.Read(dataset, out var error);
        Assert.Null(error);
        Assert.NotNull(footprint);
        Assert.Equal(RunTimes[0], footprint.FirstTime);
        Assert.Equal(RunTimes[^1], footprint.LastTime);
    }

    [Fact]
    public async Task Details_ARunFolderItem_HasTheRunsTimeStepsAndFootprint()
    {
        var item = Assert.Single(await IndexAsync(withStorm: false));
        using var reader = new LibraryDatasetReader();
        var file = LibraryItemPaths.DatasetFile(item);

        Assert.Equal(_folder, file, ignoreCase: true);
        Assert.Equal(RunTimes, reader.ReadTimeSteps(file!));
        Assert.NotNull(reader.ReadFootprint(file!));
    }

    [Fact]
    public async Task AddAsFeatureLayers_ARunFolderItem_IsWeatherThatIsReportedAndSkipped()
    {
        // Add as Feature Layers inspects the same path, and reports a GRIB2 dataset as not features.
        var item = Assert.Single(await IndexAsync(withStorm: false));
        using var inspector = new DatasetInspector(Path.Combine(_temp.Path, "extracted"));

        var inspection = await inspector.InspectAsync(Assert.Single(LibraryItemPaths.AddToMapPaths([item])), cancellationToken: Token);

        Assert.DoesNotContain(Assert.Single(inspection.Datasets).Product, FeatureLayerPlan.Products);
    }

    [Fact]
    public async Task AddAllToMap_ARunThatIsAlsoAStormsFolder_IsOneWeatherLayerOfTwoDatasets()
    {
        // The folder is two items (#1687, #1694); Add All to Map puts both in the Weather layer.
        var items = await IndexAsync(withStorm: true);
        var paths = LibraryItemPaths.AddToMapPaths(items);
        Assert.Equal(
            new[] { _folder, Path.Combine(_folder, "al092022_5day_020.zip") }.Order(StringComparer.OrdinalIgnoreCase),
            paths.Order(StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

        var datasets = await InspectAllAsync(paths);

        Assert.Equal(["GRIB2", "TC"], datasets.Select(d => d.Product).Order(StringComparer.Ordinal));
        Assert.All(datasets, d => Assert.Equal(ChartLayerKind.Weather, d.Kind));

        // The server is given the storm's forecast zip and the run as one file, each as a --layer.
        var work = Path.Combine(_temp.Path, "work");
        var inputs = ServerCommandLine.Inputs(ChartLayerMarker.ForLayer(ChartLayerKind.Weather, datasets).ToDatasetSet(), work);
        Assert.Equal(["--layer", "--layer"], inputs.Where((_, i) => i % 2 == 0));
        Assert.Contains(Path.Combine(_folder, "al092022_5day_020.zip"), inputs);
        Assert.Contains(inputs, input => input.StartsWith(work, StringComparison.Ordinal) && input.EndsWith("gfswave.grib2", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TheBundledServer_ServesARunFolderItemAlone_AndBesideAStorm_Alike()
    {
        var executable = ServerExecutable.Locate(AppContext.BaseDirectory, RuntimeInformation.OSArchitecture, overridePath: null);
        Assert.SkipWhen(executable is null, "No bundled server; run tools/build-server.ps1 before building.");
        var items = await IndexAsync(withStorm: true);
        var datasets = await InspectAllAsync(LibraryItemPaths.AddToMapPaths(items));
        var run = Assert.Single(datasets, d => d.Product == "GRIB2");

        using var probe = new HttpHealthProbe();
        var host = new ChartServerHost(
            new SoundChartsLog(null),
            new SoundChartsFolders(Path.Combine(_temp.Path, "SoundCharts"), Environment.ProcessId),
            executable,
            new ProcessServerLauncher(),
            probe,
            new ChartServerOptions { HealthInterval = TimeSpan.FromMilliseconds(200), StartupTimeout = TimeSpan.FromSeconds(60) });
        using var http = new HttpClient(new SocketsHttpHandler { UseProxy = false });
        await using var pool = new ChartServerPool(host);
        try
        {
            // The run alone, from its folder.
            await using var alone = await pool.AcquireAsync(ChartLayerMarker.ForLayer(ChartLayerKind.Weather, [run]).ToDatasetSet(), Token);
            Assert.True(alone.Server.State == ChartServerState.Running, alone.Server.FailureReason);
            var aloneEndpoint = Assert.IsType<ServerEndpoint>(alone.Server.Endpoint);
            var capabilities = await http.GetStringAsync(aloneEndpoint.WmtsCapabilitiesUri, Token);
            Assert.Equal(RunTimes.Length, CountOccurrences(capabilities, "<Value>2026-10-25T"));
            Assert.DoesNotContain("<Value>2022-", capabilities, StringComparison.Ordinal);

            // The run and the storm in one Weather layer: the storm's forecast times, and the run's.
            await using var both = await pool.AcquireAsync(ChartLayerMarker.ForLayer(ChartLayerKind.Weather, datasets).ToDatasetSet(), Token);
            Assert.True(both.Server.State == ChartServerState.Running, both.Server.FailureReason);
            var bothEndpoint = Assert.IsType<ServerEndpoint>(both.Server.Endpoint);
            capabilities = await http.GetStringAsync(bothEndpoint.WmtsCapabilitiesUri, Token);
            Assert.Equal(RunTimes.Length, CountOccurrences(capabilities, "<Value>2026-10-25T"));
            Assert.Contains("<Value>2022-09-28T00:00:00Z</Value>", capabilities, StringComparison.Ordinal);

            // Off the US East Coast, each hour draws its own waves, the same in both.
            var tiles = new List<byte[]>();
            foreach (var time in RunTimes)
            {
                var query = $"6/19/25.png?t={time:yyyy-MM-ddTHH:mm:ssZ}";
                var fromAlone = await http.GetByteArrayAsync(new Uri(aloneEndpoint.BaseUri, query), Token);
                var fromBoth = await http.GetByteArrayAsync(new Uri(bothEndpoint.BaseUri, query), Token);
                Assert.NotEmpty(fromAlone);
                Assert.Equal(fromAlone, fromBoth);
                tiles.Add(fromAlone);
            }

            Assert.Equal(RunTimes.Length, tiles.Distinct(new BytesComparer()).Count());

            // Nothing draws over the North Pacific.
            using var empty = await http.GetAsync(new Uri(aloneEndpoint.BaseUri, "6/0/0.png?t=2026-10-25T03:00:00Z"), Token);
            Assert.Equal(HttpStatusCode.NoContent, empty.StatusCode);
        }
        finally
        {
            pool.KillAll();
        }
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        for (var index = text.IndexOf(value, StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    /// <summary>Writes the run (and Ian's zips, when asked) into a folder, indexes it with the add-in's probe and returns its items.</summary>
    private async Task<IReadOnlyList<CollectionItem>> IndexAsync(bool withStorm)
    {
        Assert.SkipWhen(Fixtures.WriteUpstreamRun(_folder).Count == 0, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        if (withStorm)
        {
            Assert.SkipUnless(Fixtures.CopyUpstreamStorm(_folder), "No TC fixture.");
        }

        using var reader = new LibraryDatasetReader();
        using var library = new SoundChartsLibrary(Path.Combine(_temp.Path, "library"), reader.Probe);
        library.Initialize();

        await library.ImportAsync([_folder], cancellationToken: Token);
        await library.WhenIdle();

        var items = Assert.Single(Assert.Single(library.Collections).Sources).Index!.Items;
        Assert.Contains(items, item => item.Key == "gfswave/" && item.ProductSpec == "GRIB2");
        return items;
    }

    /// <summary>
    /// Inspects each path as Add All to Map does, and returns every dataset
    /// found once, as a layer's marker keeps them. The run's folder also
    /// finds the storm's forecast zip beside it, as an advisory folder finds
    /// charts beside it (issue #69).
    /// </summary>
    private async Task<IReadOnlyList<ChartLayerDataset>> InspectAllAsync(IEnumerable<string> paths)
    {
        using var inspector = new DatasetInspector(Path.Combine(_temp.Path, "extracted"));
        var datasets = new List<ChartLayerDataset>();
        foreach (var path in paths)
        {
            var inspection = await inspector.InspectAsync(path, cancellationToken: Token);
            Assert.Empty(inspection.Warnings);
            datasets.AddRange(inspection.Datasets);
        }

        return [.. datasets.DistinctBy(d => d.Path, StringComparer.OrdinalIgnoreCase)];
    }

    private sealed class BytesComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[]? x, byte[]? y) => x is not null && y is not null && x.AsSpan().SequenceEqual(y);

        public int GetHashCode(byte[] obj) => obj.Length;
    }
}
