using EncDotNet.S100.DataModel;
using EncDotNet.SoundCharts.RouteCheck;

namespace EncDotNet.SoundCharts.Tests.RouteCheck;

public sealed class RouteLinesTests
{
    [Fact]
    public void FromLine_MakesALegPerPairOfVertices()
    {
        var route = RouteLines.FromLine(" Line 7 ", RouteSource.Feature,
            [new(41.0, -70.0), new(41.1, -70.1), new(41.2, -70.1)]);

        Assert.Equal("Line 7", route.Name);
        Assert.Equal(RouteSource.Feature, route.Source);
        Assert.Equal(["Leg 1 → 2", "Leg 2 → 3"], route.Plan.Legs.Select(l => l.Name));
        Assert.Equal(new GeoPosition(41.1, -70.1), route.Plan.Legs[1].Points[0]);
        Assert.All(route.Plan.Legs, l => Assert.Null(l.Draft));
        Assert.Null(route.Plan.Departure);
        Assert.Null(route.SafetyContour);
    }

    [Fact]
    public void FromLine_DropsRepeatedAndNonFiniteVertices()
    {
        var route = RouteLines.FromLine("Line", RouteSource.Sketch,
            [new(41.0, -70.0), new(41.0, -70.0), new(double.NaN, -70.0), new(41.1, -70.1)]);

        var leg = Assert.Single(route.Plan.Legs);
        Assert.Equal([new GeoPosition(41.0, -70.0), new GeoPosition(41.1, -70.1)], leg.Points);
    }

    [Fact]
    public void FromLine_OneDistinctVertex_IsTooFew()
    {
        var ex = Assert.Throws<RouteFileException>(() =>
            RouteLines.FromLine("Line", RouteSource.Sketch, [new(41.0, -70.0), new(41.0, -70.0)]));

        Assert.Equal("The route has fewer than two waypoints.", ex.Message);
    }
}
