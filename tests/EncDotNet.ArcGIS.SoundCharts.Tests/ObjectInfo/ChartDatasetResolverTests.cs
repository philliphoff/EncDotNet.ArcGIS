using EncDotNet.SoundCharts.ObjectInfo;

namespace EncDotNet.SoundCharts.Tests.ObjectInfo;

public class ChartDatasetResolverTests
{
    private static readonly ChartLayerReference Layer = new("CIMPATH=map/chart.json", "Chart", "CIMPATH=map/map.json");

    [Fact]
    public void FixedResolver_ReturnsItsDatasetsForAnyMap()
    {
        var dataset = new ChartDataset(@"C:\data\a.000");
        var resolver = new FixedChartDatasetResolver([dataset]);

        Assert.Equal([dataset], resolver.Resolve([]));
        Assert.Equal([dataset], resolver.Resolve([Layer]));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ; ;")]
    public void FromPathList_NamesNoDataset_ReturnsNull(string? paths)
    {
        Assert.Null(FixedChartDatasetResolver.FromPathList(paths));
    }

    [Fact]
    public void FromPathList_SplitsAndTrimsFullPaths()
    {
        var resolver = FixedChartDatasetResolver.FromPathList(@" C:\data\a.000 ;C:\data\b.h5");

        Assert.NotNull(resolver);
        Assert.Equal([@"C:\data\a.000", @"C:\data\b.h5"], resolver.Resolve([]).Select(d => d.Path));
    }

    [Fact]
    public void ChartDataset_NameFallsBackToTheFileName()
    {
        Assert.Equal("a.000", new ChartDataset(Path.Combine("C:", "data", "a.000")).Name);
        Assert.Equal("Cell A", new ChartDataset(@"C:\data\a.000", "Cell A").Name);
    }

    [Fact]
    public void ObjectInfoHost_ReleaseBuildsUseTheChartLayerRegistry()
    {
#if DEBUG
        Assert.Skip("Debug builds may read the stop-gap environment variable.");
#else
        Assert.IsType<EncDotNet.SoundCharts.ChartLayers.RegistryChartDatasetResolver>(ObjectInfoHost.Resolver);
#endif
    }
}
