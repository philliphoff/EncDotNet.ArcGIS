using EncDotNet.SoundCharts.ChartLayers;
using static EncDotNet.SoundCharts.Tests.ChartLayers.ChartLayerSamples;

namespace EncDotNet.SoundCharts.Tests.ChartLayers;

public class SmallPartsTests
{
    [Theory]
    [InlineData("S-104", ChartLayerKind.WaterLevel)]
    [InlineData("S-111", ChartLayerKind.SurfaceCurrents)]
    [InlineData("S-57", ChartLayerKind.Chart)]
    [InlineData("S-101", ChartLayerKind.Chart)]
    [InlineData("S-102", ChartLayerKind.Chart)]
    [InlineData("S-124", ChartLayerKind.Chart)]
    public void Kinds_ByTimeBehaviour(string product, ChartLayerKind kind)
    {
        Assert.Equal(kind, ChartLayerKinds.ForProduct(product));
    }

    [Theory]
    [InlineData(ChartLayerKind.Chart, "Chart")]
    [InlineData(ChartLayerKind.WaterLevel, "Water level")]
    [InlineData(ChartLayerKind.SurfaceCurrents, "Surface currents")]
    public void Kinds_NamesAndIdsRoundTrip(ChartLayerKind kind, string name)
    {
        Assert.Equal(name, ChartLayerKinds.DisplayName(kind));
        Assert.Equal(kind, ChartLayerKinds.FromId(ChartLayerKinds.Id(kind)));
    }

    [Fact]
    public void Extent_Union_SkipsUnknownAndInvalid()
    {
        var union = GeoExtent.Union([Cell.Extent, null, new GeoExtent(10, 95, 11, 96), Tide.Extent]);

        Assert.Equal(new GeoExtent(-80.1, 32.6, -70.94, 42.38), union);
        Assert.Null(GeoExtent.Union([null]));
    }

    [Theory]
    [InlineData("""{"minzoom":11,"maxzoom":18}""", 11)]
    [InlineData("""{"minzoom":9.0}""", 9)]
    [InlineData("""{"maxzoom":18}""", null)]
    [InlineData("""{"minzoom":"11"}""", null)]
    [InlineData("""{"minzoom":-1}""", null)]
    [InlineData("[]", null)]
    [InlineData("not json", null)]
    [InlineData(null, null)]
    public void TileJson_MinZoom(string? json, int? expected)
    {
        Assert.Equal(expected, TileJsonZoom.TryReadMinZoom(json));
    }

    [Fact]
    public void TileJson_MinScale_IsTheZoomsScaleOrNoLimit()
    {
        Assert.Equal(ChartLayerDocument.ScaleOfZoom(11), TileJsonZoom.MinScale(11));
        Assert.Equal(0, TileJsonZoom.MinScale(0));
        Assert.Equal(0, TileJsonZoom.MinScale(null));
    }

    [Fact]
    public void Credits_ProducersInOrderOfDatasetCount()
    {
        Assert.Equal("US, AA00", ChartCredits.Producers([Cell, Tide, S101]));
        Assert.Equal("unknown producer", ChartCredits.Producers([Cell with { Producer = null }]));
    }
}
