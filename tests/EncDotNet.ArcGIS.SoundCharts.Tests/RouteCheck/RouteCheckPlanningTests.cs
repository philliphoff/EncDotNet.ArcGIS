using EncDotNet.S100.DataModel;
using EncDotNet.S100.Navigation;
using EncDotNet.S100.Quantities;
using EncDotNet.SoundCharts.RouteCheck;

namespace EncDotNet.SoundCharts.Tests.RouteCheck;

public sealed class RouteCheckPlanningTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Prepare_OptionsFillWhatTheRouteDoesntGive()
    {
        var route = RouteLines.FromLine("Line", RouteSource.Sketch, [new(41.0, -70.0), new(41.1, -70.1)]);
        var ship = new ShipParameters(Draft: 7.0, UkcMargin: 1.0, SafetyContour: 10.0, SafetyDepth: 12.0);

        var prepared = RouteCheckPlanning.Prepare(route, ship, Now);

        var leg = Assert.Single(prepared.Plan.Legs);
        Assert.Equal(Depth.FromMetres(7.0), leg.Draft);
        Assert.Equal(Depth.FromMetres(1.0), leg.RequiredUkc);
        Assert.Equal(Now, prepared.Settings.At);
        Assert.Equal(Depth.FromMetres(10.0), prepared.Settings.SafetyContour);
        Assert.Equal(Depth.FromMetres(12.0), prepared.Settings.SafetyDepth);
        Assert.Equal(
            new RouteCheckParameters(7.0, false, 1.0, false, 10.0, false, 12.0, false, HasDraft: true),
            prepared.Parameters);
    }

    [Fact]
    public void Prepare_TheRoutesOwnValuesWin()
    {
        var departure = new DateTimeOffset(2026, 3, 1, 6, 0, 0, TimeSpan.Zero);
        PassageLeg Leg(double? draft) => new()
        {
            Points = [new GeoPosition(41.0, -70.0), new GeoPosition(41.1, -70.1)],
            Draft = draft is { } d ? Depth.FromMetres(d) : null,
            RequiredUkc = Depth.FromMetres(2.0),
            Speed = Speed.FromKnots(8),
            Name = "Leg",
        };
        var route = new RouteInput("Planned", RouteSource.S421, new PassagePlan([Leg(8.5), Leg(null)], departure))
        {
            SafetyContour = 20.0,
        };

        var prepared = RouteCheckPlanning.Prepare(route, new ShipParameters(7.0, 1.0, 10.0, 12.0), Now);

        Assert.Equal([Depth.FromMetres(8.5), Depth.FromMetres(7.0)], prepared.Plan.Legs.Select(l => l.Draft));
        Assert.All(prepared.Plan.Legs, l => Assert.Equal(Depth.FromMetres(2.0), l.RequiredUkc));
        Assert.All(prepared.Plan.Legs, l => Assert.Equal(Speed.FromKnots(8), l.Speed));
        Assert.Equal(departure, prepared.Plan.Departure);
        Assert.Equal(departure, prepared.Settings.At);
        Assert.Equal(Depth.FromMetres(20.0), prepared.Settings.SafetyContour);
        Assert.Equal(Depth.FromMetres(12.0), prepared.Settings.SafetyDepth);
        Assert.True(prepared.Parameters.DraftFromRoute);
        Assert.Equal(8.5, prepared.Parameters.Draft);
        Assert.True(prepared.Parameters.UkcMarginFromRoute);
        Assert.True(prepared.Parameters.SafetyContourFromRoute);
        Assert.False(prepared.Parameters.SafetyDepthFromRoute);
    }

    [Fact]
    public void Prepare_WithoutADraft_HasNoDraft()
    {
        var route = RouteLines.FromLine("Line", RouteSource.Sketch, [new(41.0, -70.0), new(41.1, -70.1)]);

        var prepared = RouteCheckPlanning.Prepare(route, ShipParameters.Default, Now);

        Assert.False(prepared.Parameters.HasDraft);
        Assert.Null(prepared.Parameters.Draft);
        Assert.Null(prepared.Plan.Legs[0].Draft);
        Assert.Null(prepared.Plan.Legs[0].RequiredUkc);
        Assert.Equal(Depth.FromMetres(30.0), prepared.Settings.SafetyContour);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void Prepare_IgnoresADraftThatIsntPositive(double draft)
    {
        var route = RouteLines.FromLine("Line", RouteSource.Sketch, [new(41.0, -70.0), new(41.1, -70.1)]);

        var prepared = RouteCheckPlanning.Prepare(route, ShipParameters.Default with { Draft = draft }, Now);

        Assert.False(prepared.Parameters.HasDraft);
    }
}
