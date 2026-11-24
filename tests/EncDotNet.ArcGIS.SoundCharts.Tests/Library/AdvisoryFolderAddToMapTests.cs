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
/// Follows a Library item for an NHC advisory folder (issue #69) through
/// <b>Add to Map</b> as the add-in does in Pro, without Pro: the path it
/// passes, the inspection, the layer's marker, the server's inputs, the
/// revision, the footprint and the Library details. Uses EncDotNet.S100's
/// Hurricane Ian fixture, and the bundled chart server when it's built.
/// </summary>
public sealed class AdvisoryFolderAddToMapTests : IDisposable
{
    private readonly TempFolder _temp = new();
    private readonly string _folder;
    private readonly string _forecast;

    public AdvisoryFolderAddToMapTests()
    {
        _folder = Path.Combine(_temp.Path, "ian");
        _forecast = Path.Combine(_folder, "al092022_5day_020.zip");
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task AddToMap_AnAdvisoryFolderItem_IsOneStormInTheWeatherLayer()
    {
        var item = await IndexStormAsync();

        // Add to Map passes the folder, which is there.
        var path = Assert.Single(LibraryItemPaths.AddToMapPaths([item]));
        Assert.Equal(_folder, path, ignoreCase: true);
        Assert.True(LibraryItemPaths.Exists(path));

        // Add SoundCharts Data finds one TC dataset at the folder.
        using var inspector = new DatasetInspector(Path.Combine(_temp.Path, "extracted"));
        var inspection = await inspector.InspectAsync(path, cancellationToken: Token);
        var dataset = Assert.Single(inspection.Datasets);
        Assert.Empty(inspection.Warnings);
        Assert.Equal("TC", dataset.Product);
        Assert.Equal(ChartLayerKind.Weather, dataset.Kind);

        // The layer's marker keeps the folder, and the server is given the forecast zip.
        var marker = ChartLayerMarker.TryParse(ChartLayerMarker.ForLayer(dataset.Kind, [dataset]).ToJson());
        Assert.NotNull(marker);
        Assert.Equal(_folder, Assert.Single(marker.Datasets).Path, ignoreCase: true);
        Assert.True(Assert.Single(marker.Datasets).Exists);
        var set = marker.ToDatasetSet();
        Assert.Equal([_forecast], ServerCommandLine.Inputs(set));

        // The revision covers every file in the folder.
        var revision = DataRevision.Of(set.Sources);
        Assert.Equal(3, revision.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.Contains("AL092022_BEST_TRACK.ZIP|", revision, StringComparison.Ordinal);

        // The Coverage layer's footprint runs from the past track to the last forecast point.
        using var footprints = new FootprintReader();
        var footprint = footprints.Read(dataset, out var error);
        Assert.Null(error);
        Assert.NotNull(footprint);
        Assert.Equal(new DateTime(2022, 9, 22, 18, 0, 0, DateTimeKind.Utc), footprint.FirstTime);
        Assert.Equal(new DateTime(2022, 10, 2, 0, 0, 0, DateTimeKind.Utc), footprint.LastTime);
    }

    [Fact]
    public async Task Details_AnAdvisoryFolderItem_HasItsTimeStepsAndFootprint()
    {
        var item = await IndexStormAsync();
        using var reader = new LibraryDatasetReader();
        var file = LibraryItemPaths.DatasetFile(item);

        Assert.Equal(_folder, file, ignoreCase: true);
        Assert.Equal(8, reader.ReadTimeSteps(file!).Count);
        Assert.NotNull(reader.ReadFootprint(file!));
    }

    [Fact]
    public async Task AddAsFeatureLayers_AnAdvisoryFolderItem_IsAStormThatIsReportedAndSkipped()
    {
        // Add as Feature Layers inspects the same path, and reports a TC dataset as not features.
        var item = await IndexStormAsync();
        using var inspector = new DatasetInspector(Path.Combine(_temp.Path, "extracted"));

        var inspection = await inspector.InspectAsync(Assert.Single(LibraryItemPaths.AddToMapPaths([item])), cancellationToken: Token);

        Assert.DoesNotContain(Assert.Single(inspection.Datasets).Product, FeatureLayerPlan.Products);
    }

    [Fact]
    public async Task TheBundledServer_ServesAndDrawsAnAdvisoryFolderItem()
    {
        var executable = ServerExecutable.Locate(AppContext.BaseDirectory, RuntimeInformation.OSArchitecture, overridePath: null);
        Assert.SkipWhen(executable is null, "No bundled server; run tools/build-server.ps1 before building.");
        var item = await IndexStormAsync();
        using var inspector = new DatasetInspector(Path.Combine(_temp.Path, "extracted"));
        var inspection = await inspector.InspectAsync(Assert.Single(LibraryItemPaths.AddToMapPaths([item])), cancellationToken: Token);
        var marker = ChartLayerMarker.ForLayer(ChartLayerKind.Weather, inspection.Datasets);

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
            await using var lease = await pool.AcquireAsync(marker.ToDatasetSet(), Token);
            Assert.True(lease.Server.State == ChartServerState.Running, lease.Server.FailureReason);
            var endpoint = Assert.IsType<ServerEndpoint>(lease.Server.Endpoint);

            // Ian's eight forecast times, as from the forecast zip.
            var capabilities = await http.GetStringAsync(endpoint.WmtsCapabilitiesUri, Token);
            Assert.Contains("<Value>2022-09-28T00:00:00Z</Value>", capabilities, StringComparison.Ordinal);
            Assert.Contains("<Value>2022-10-02T00:00:00Z</Value>", capabilities, StringComparison.Ordinal);

            // The storm draws over Florida on the 28th, and nothing draws over the North Pacific.
            using var storm = await http.GetAsync(new Uri(endpoint.BaseUri, "5/8/13.png?t=2022-09-28T00:00:00Z"), Token);
            Assert.Equal(HttpStatusCode.OK, storm.StatusCode);
            Assert.NotEmpty(await storm.Content.ReadAsByteArrayAsync(Token));
            using var empty = await http.GetAsync(new Uri(endpoint.BaseUri, "5/0/0.png?t=2022-09-28T00:00:00Z"), Token);
            Assert.Equal(HttpStatusCode.NoContent, empty.StatusCode);
        }
        finally
        {
            pool.KillAll();
        }
    }

    /// <summary>Copies Ian's zips into a folder, indexes it with the add-in's probe and returns its one item.</summary>
    private async Task<CollectionItem> IndexStormAsync()
    {
        Assert.SkipUnless(Fixtures.CopyUpstreamStorm(_folder), $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        using var reader = new LibraryDatasetReader();
        using var library = new SoundChartsLibrary(Path.Combine(_temp.Path, "library"), reader.Probe);
        library.Initialize();

        await library.ImportAsync([_folder], cancellationToken: Token);
        await library.WhenIdle();

        var item = Assert.Single(Assert.Single(Assert.Single(library.Collections).Sources).Index!.Items);
        Assert.Equal("ian/", item.Key);
        return item;
    }
}
