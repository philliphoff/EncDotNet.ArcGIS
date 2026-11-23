using EncDotNet.SoundCharts.ChartLayers;
using EncDotNet.SoundCharts.Charts;
using static EncDotNet.SoundCharts.Tests.ChartLayers.ChartLayerSamples;

namespace EncDotNet.SoundCharts.Tests.ChartLayers;

public class ChartLayerMarkerTests
{
    [Fact]
    public void Layer_RoundTripsThroughJson()
    {
        var marker = ChartLayerMarker.ForLayer(ChartLayerKind.Chart, [S101, Cell]);

        var read = ChartLayerMarker.TryParse(marker.ToJson());

        Assert.NotNull(read);
        Assert.Equal(marker, read);
        Assert.False(read.IsGroup);
        Assert.Equal(ChartLayerKind.Chart, read.Kind);
        Assert.Equal([S101, Cell], read.Datasets);
    }

    [Fact]
    public void Weather_RoundTripsThroughJson_AndKeepsItsDatasetsTimeVarying()
    {
        var marker = ChartLayerMarker.ForLayer(ChartLayerKind.Weather, [Storm]);

        var read = ChartLayerMarker.TryParse(marker.ToJson());

        Assert.NotNull(read);
        Assert.Equal(ChartLayerKind.Weather, read.Kind);
        var storm = Assert.Single(read.Datasets);
        Assert.True(storm.TimeVarying);
        Assert.Equal(ChartLayerKind.Weather, storm.Kind);
        Assert.Contains("\"timeVarying\":true", marker.ToJson(), StringComparison.Ordinal);
    }

    [Fact]
    public void Layer_WithoutTimeVaryingDatasets_WritesTheMarkerAsBefore()
    {
        var json = ChartLayerMarker.ForLayer(ChartLayerKind.WaterLevel, [Tide]).ToJson();

        Assert.DoesNotContain("timeVarying", json, StringComparison.Ordinal);
    }

    [Fact]
    public void MarkerFromBeforeTheWeatherLayer_LoadsAsItWas()
    {
        // A project saved before the Weather layer: no timeVarying, and a weather file in Chart.
        const string json = """
            {"version":1,"role":"layer","kind":"chart","datasets":[
            {"path":"C:\\Charts\\US5MA1BO\\US5MA1BO.000","product":"S-57","producer":"US"},
            {"path":"C:\\Charts\\gfswave-useast.grib2","product":"GRIB2","edition":"2.0.0"}]}
            """;

        var read = ChartLayerMarker.TryParse(json);

        Assert.NotNull(read);
        Assert.Equal(ChartLayerKind.Chart, read.Kind);
        Assert.All(read.Datasets, d =>
        {
            Assert.False(d.TimeVarying);
            Assert.Equal(ChartLayerKind.Chart, d.Kind);
        });
    }

    [Fact]
    public void Group_RoundTripsThroughJson()
    {
        var marker = ChartLayerMarker.ForGroup(["US5MA1BO", "S101", "us5ma1bo"]);

        var read = ChartLayerMarker.TryParse(marker.ToJson());

        Assert.NotNull(read);
        Assert.True(read.IsGroup);
        Assert.Null(read.Kind);
        Assert.Equal(["US5MA1BO", "S101"], read.Inputs);
    }

    [Fact]
    public void Layer_KeepsEachDatasetOnceInPathOrder()
    {
        var marker = ChartLayerMarker.ForLayer(ChartLayerKind.Chart, [S101, Cell, Cell with { Path = Cell.Path.ToUpperInvariant() }]);

        Assert.Equal([S101.Path, Cell.Path], marker.Datasets.Select(d => d.Path));
    }

    [Fact]
    public void Layer_NeedsADataset()
    {
        Assert.Throws<ArgumentException>(() => ChartLayerMarker.ForLayer(ChartLayerKind.Chart, []));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("""{"version":2,"role":"group"}""")]
    [InlineData("""{"version":1,"role":"layer","kind":"radar","datasets":[{"path":"C:\\a.000","product":"S-57"}]}""")]
    [InlineData("""{"version":1,"role":"layer","kind":"chart","datasets":[]}""")]
    [InlineData("""{"version":1,"role":"other"}""")]
    public void TryParse_SomethingElse_IsNull(string? json)
    {
        Assert.Null(ChartLayerMarker.TryParse(json));
    }

    [Fact]
    public void ToDatasetSet_IsTheChartServersSet()
    {
        var marker = ChartLayerMarker.ForLayer(ChartLayerKind.Chart, [S101, Cell]);

        var set = marker.ToDatasetSet();

        Assert.Equal(new DatasetSet([DatasetSource.Dataset(Cell.Path), DatasetSource.Dataset(S101.Path)]), set);
        Assert.All(set.Sources, s => Assert.Equal(DatasetSourceKind.Dataset, s.Kind));
    }

    [Fact]
    public void ToDatasetSet_OfTheGroup_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ChartLayerMarker.ForGroup(["a"]).ToDatasetSet());
    }

    [Fact]
    public void ToChartDatasets_CarriesNameAndNotForNavigation()
    {
        var datasets = ChartLayerMarker.ForLayer(ChartLayerKind.Chart, [S101, Cell]).ToChartDatasets();

        Assert.Equal(["101AA00DS0003", "US5MA1BO"], datasets.Select(d => d.Name));
        Assert.Equal([true, false], datasets.Select(d => d.NotForNavigation));
    }
}
