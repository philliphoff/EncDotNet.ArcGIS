using EncDotNet.SoundCharts.ChartLayers;
using EncDotNet.SoundCharts.ObjectInfo;

namespace EncDotNet.SoundCharts.Tests.ChartLayers;

/// <summary>
/// Object information on layers Add SoundCharts Data made: a real cell is
/// inspected, becomes a layer marker in the registry, and a pick finds its
/// features through the real resolver, as the tool does in Pro.
/// </summary>
public sealed class ChartLayerPickTests : IDisposable
{
    private const string MapUri = "CIMPATH=map/map.json";

    private readonly TempFolder _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task Pick_OnAnAddedCell_ResolvesThroughTheRegistry()
    {
        var path = Fixtures.Upstream("S57/US5MA1BO/US5MA1BO.000");
        Assert.SkipWhen(path is null, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");

        using var inspector = new DatasetInspector(_temp.Path);
        var inspection = await inspector.InspectAsync(path, cancellationToken: TestContext.Current.CancellationToken);
        var composition = ChartComposition.Empty.With(inspection.Input, inspection.Datasets);

        var registry = new ChartLayerRegistry();
        registry.Set("CIMPATH=map/soundcharts.json", MapUri, composition.Group);
        registry.Set("CIMPATH=map/chart.json", MapUri, composition.Layers[ChartLayerKind.Chart]);
        var resolver = new RegistryChartDatasetResolver(registry);

        var visible = new[]
        {
            new ChartLayerReference("CIMPATH=map/soundcharts.json", composition.GroupName, MapUri),
            new ChartLayerReference("CIMPATH=map/chart.json", "Chart", MapUri),
        };
        var datasets = resolver.Resolve(visible);
        Assert.Equal("US5MA1BO", Assert.Single(datasets).Name);

        // The middle of the cell's own footprint: a cell this size has features there.
        var extent = inspection.Datasets[0].Extent!.Value;
        using var service = new ObjectInfoService();
        var report = service.Identify(
            datasets, (extent.South + extent.North) / 2, (extent.West + extent.East) / 2, 200, time: null, TestContext.Current.CancellationToken);

        Assert.Empty(report.Problems);
        var result = Assert.Single(report.Datasets);
        Assert.Equal("US5MA1BO", result.Dataset.Name);
        Assert.NotEmpty(result.Features);
    }
}
