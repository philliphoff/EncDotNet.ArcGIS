using EncDotNet.SoundCharts.Export;

namespace EncDotNet.SoundCharts.Tests.Export;

public class ChartTilePlanTests
{
    // A 10 x 7.5 inch frame over Nantucket Sound at about 1:36,000 (zoom 14 on screen).
    private static ExportFrame Frame(double widthInches = 10, double heightInches = 7.5)
    {
        var (minX, minY) = ChartTilePlan.FromLonLat(-70.40, 41.24);
        var metresPerPixel = ChartTilePlan.MetresPerTilePixel(14);
        return new ExportFrame(
            minX,
            minY,
            minX + widthInches * 96 * metresPerPixel,
            minY + heightInches * 96 * metresPerPixel,
            widthInches,
            heightInches);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(14, 14)]
    [InlineData(18, 18)]
    public void ZoomFor_ATileResolution_IsThatZoom(int zoom, int expected)
    {
        Assert.Equal(expected, ChartTilePlan.ZoomFor(ChartTilePlan.MetresPerTilePixel(zoom)));
    }

    [Fact]
    public void ZoomFor_BetweenTwoZooms_IsTheNearestOnALogScale()
    {
        var z14 = ChartTilePlan.MetresPerTilePixel(14);

        Assert.Equal(14, ChartTilePlan.ZoomFor(z14 * 1.3));
        Assert.Equal(13, ChartTilePlan.ZoomFor(z14 * 1.5));
        Assert.Equal(15, ChartTilePlan.ZoomFor(z14 / 1.5));
    }

    [Fact]
    public void ZoomFor_PastTheServersZooms_IsClamped()
    {
        Assert.Equal(0, ChartTilePlan.ZoomFor(1e9));
        Assert.Equal(ChartTilePlan.MaxZoom, ChartTilePlan.ZoomFor(1e-6));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartTilePlan.ZoomFor(0));
    }

    [Theory]
    [InlineData(96)]
    [InlineData(150)]
    [InlineData(300)]
    [InlineData(600)]
    public void For_EveryResolution_UsesTheScreensZoom(int dpi)
    {
        // SCAMIN is decided per zoom: the same zoom keeps the screen's content.
        Assert.Equal(14, ChartTilePlan.For(Frame(), dpi).Zoom);
    }

    [Theory]
    [InlineData(72, 1)]
    [InlineData(96, 1)]
    [InlineData(150, 2)]
    [InlineData(300, 2)]
    [InlineData(600, 2)]
    public void For_AboveScreenResolution_RendersAtTheHighestPixelRatio(int dpi, int ratio)
    {
        Assert.Equal(ratio, ChartTilePlan.For(Frame(), dpi).PixelRatio);
    }

    [Fact]
    public void For_ARendererWithAHigherRatio_UsesTheRatioTheResolutionNeeds()
    {
        Assert.Equal(4, ChartTilePlan.For(Frame(), 300, maxPixelRatio: 4).PixelRatio);
        Assert.Equal(2, ChartTilePlan.For(Frame(), 150, maxPixelRatio: 4).PixelRatio);
    }

    [Fact]
    public void For_TheFrame_GivesItsBoundsInDegreesAndTheRenderedResolution()
    {
        var plan = ChartTilePlan.For(Frame(), 300);

        Assert.Equal(-70.40, plan.West, 6);
        Assert.Equal(41.24, plan.South, 6);
        Assert.True(plan.East > plan.West && plan.North > plan.South);
        Assert.Equal(192, plan.RenderedDpi, 3);
        Assert.StartsWith("-70.4", plan.BoundingBox, StringComparison.Ordinal);
        Assert.Equal(4, plan.BoundingBox.Split(',').Length);
    }

    [Fact]
    public void For_TheFrame_CountsTheTilesThatCoverIt()
    {
        // 960 x 720 screen pixels at zoom 14: 4 or 5 columns and 3 or 4 rows, by alignment.
        var plan = ChartTilePlan.For(Frame(), 96);

        Assert.InRange(plan.TileCount, 12, 20);
    }

    [Fact]
    public void For_AFrameTooLargeToRender_Throws()
    {
        var frame = Frame(widthInches: 1000, heightInches: 1000);

        Assert.Throws<InvalidOperationException>(() => ChartTilePlan.For(frame, 96));
    }

    [Fact]
    public void For_AFrameWithNoArea_Throws()
    {
        Assert.Throws<ArgumentException>(() => ChartTilePlan.For(new ExportFrame(0, 0, 0, 10, 1, 1), 96));
        Assert.Throws<ArgumentException>(() => ChartTilePlan.For(new ExportFrame(0, 0, 10, 10, 0, 1), 96));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartTilePlan.For(Frame(), 0));
    }

    [Fact]
    public void LonLat_RoundTrips()
    {
        var (x, y) = ChartTilePlan.FromLonLat(-70.3, 41.3);
        var (lon, lat) = ChartTilePlan.ToLonLat(x, y);

        Assert.Equal(-70.3, lon, 9);
        Assert.Equal(41.3, lat, 9);
    }
}
