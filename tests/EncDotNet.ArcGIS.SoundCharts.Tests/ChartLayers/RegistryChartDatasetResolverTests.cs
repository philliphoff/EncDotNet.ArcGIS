using EncDotNet.SoundCharts.ChartLayers;
using EncDotNet.SoundCharts.ObjectInfo;
using static EncDotNet.SoundCharts.Tests.ChartLayers.ChartLayerSamples;

namespace EncDotNet.SoundCharts.Tests.ChartLayers;

/// <summary>
/// The real Object information resolver (A3): map, then SoundCharts layers by
/// provenance, then the dataset set, then datasets.
/// </summary>
public class RegistryChartDatasetResolverTests
{
    private const string MapUri = "CIMPATH=map/map.json";

    private static readonly ChartLayerReference Group = new("CIMPATH=map/soundcharts.json", "SoundCharts · 3 datasets", MapUri);
    private static readonly ChartLayerReference Currents_ = new("CIMPATH=map/surface-currents.json", "Surface currents", MapUri);
    private static readonly ChartLayerReference Chart = new("CIMPATH=map/chart.json", "Chart", MapUri);
    private static readonly ChartLayerReference EsriEnc = new("CIMPATH=map/us5ma1bo.json", "US5MA1BO", MapUri);
    private static readonly ChartLayerReference Basemap = new("CIMPATH=map/topographic.json", "Topographic", MapUri);

    private readonly ChartLayerRegistry _registry = new();
    private readonly RegistryChartDatasetResolver _resolver;

    public RegistryChartDatasetResolverTests()
    {
        _resolver = new RegistryChartDatasetResolver(_registry);
        _registry.Set(Group.LayerUri, MapUri, ChartLayerMarker.ForGroup(["a", "b"]));
        _registry.Set(Currents_.LayerUri, MapUri, ChartLayerMarker.ForLayer(ChartLayerKind.SurfaceCurrents, [Currents]));
        _registry.Set(Chart.LayerUri, MapUri, ChartLayerMarker.ForLayer(ChartLayerKind.Chart, [S101, Cell]));
    }

    [Fact]
    public void Resolve_ReturnsTheDatasetsOfTheSoundChartsLayersTopFirst()
    {
        var datasets = _resolver.Resolve([Group, Currents_, EsriEnc, Chart, Basemap]);

        // Within a layer, the dataset set's canonical order (by path).
        Assert.Equal([Currents.Path, S101.Path, Cell.Path], datasets.Select(d => d.Path));
    }

    [Fact]
    public void Resolve_IgnoresLayersWithoutAMarker()
    {
        Assert.Empty(_resolver.Resolve([EsriEnc, Basemap]));
    }

    [Fact]
    public void Resolve_TheGroupAlone_HasNoDatasetsOfItsOwn()
    {
        Assert.Empty(_resolver.Resolve([Group]));
    }

    [Fact]
    public void Resolve_InvisibleLayers_AreLeftOut()
    {
        // The tool passes only the layers visible in the view.
        var datasets = _resolver.Resolve([Chart]);

        Assert.DoesNotContain(datasets, d => d.Path == Currents.Path);
    }

    [Fact]
    public void Resolve_ADatasetInTwoLayers_IsListedOnce()
    {
        var copy = new ChartLayerReference("CIMPATH=map/chart-copy.json", "Chart", MapUri);
        _registry.Set(copy.LayerUri, MapUri, ChartLayerMarker.ForLayer(ChartLayerKind.Chart, [Cell]));

        var datasets = _resolver.Resolve([Chart, copy]);

        Assert.Single(datasets, d => d.Path == Cell.Path);
    }

    [Fact]
    public void Resolve_CarriesNamesAndNotForNavigation()
    {
        var datasets = _resolver.Resolve([Chart]);

        var s101 = Assert.Single(datasets, d => d.Path == S101.Path);
        Assert.Equal("101AA00DS0003", s101.Name);
        Assert.True(s101.NotForNavigation);
    }

    [Fact]
    public void Resolve_ARemovedLayer_IsForgotten()
    {
        _registry.Remove(Chart.LayerUri);

        Assert.Equal([Currents.Path], _resolver.Resolve([Currents_, Chart]).Select(d => d.Path));
    }

    [Fact]
    public void Registry_LayersOf_ListsChartLayersNotGroups()
    {
        _registry.Set("CIMPATH=other/chart.json", "CIMPATH=other/map.json", ChartLayerMarker.ForLayer(ChartLayerKind.Chart, [Tide]));

        Assert.Equal(
            [Chart.LayerUri, Currents_.LayerUri],
            _registry.LayersOf(MapUri).Select(pair => pair.Key).Order(StringComparer.Ordinal));
    }
}
