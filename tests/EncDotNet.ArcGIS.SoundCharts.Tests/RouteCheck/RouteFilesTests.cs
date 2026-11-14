using EncDotNet.SoundCharts.RouteCheck;

namespace EncDotNet.SoundCharts.Tests.RouteCheck;

/// <summary>
/// Tests for <see cref="RouteFiles"/>: synthetic RTZ written here, and
/// EncDotNet.S100's S-421 and RTZ route fixtures when
/// <see cref="Fixtures.UpstreamDatasetsVariable"/> is set.
/// </summary>
public sealed class RouteFilesTests : IDisposable
{
    private const string Rtz = """
        <?xml version="1.0" encoding="UTF-8"?>
        <route xmlns="http://www.cirm.org/RTZ/1/0" version="1.0">
          <routeInfo routeName="Synthetic passage" />
          <waypoints>
            <waypoint id="1"><position lat="41.255" lon="-70.345" /></waypoint>
            <waypoint id="2"><position lat="41.29" lon="-70.31" />
              <leg safetyContour="10" safetyDepth="12" draughtForward="6.5" draughtAft="7.0" staticUKC="1.5" /></waypoint>
            <waypoint id="3"><position lat="41.32" lon="-70.28" />
              <leg safetyContour="20" /></waypoint>
          </waypoints>
        </route>
        """;

    private readonly TempFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void Read_Rtz_TakesTheNameLegsAndDeepestSafetyValues()
    {
        var path = Write("passage.rtz", Rtz);

        var route = RouteFiles.Read(path);

        Assert.Equal("Synthetic passage", route.Name);
        Assert.Equal(RouteSource.Rtz, route.Source);
        Assert.Equal(path, route.Path);
        Assert.Equal(2, route.Plan.Legs.Count);
        Assert.Equal(20.0, route.SafetyContour);
        Assert.Equal(12.0, route.SafetyDepth);
        Assert.Contains(route.Plan.Legs, l => l.Draft is not null);
    }

    [Fact]
    public void Read_NotARoute_SaysSoInTheViewersWords()
    {
        var path = Write("notes.gml", "<notARoute />");

        var ex = Assert.Throws<RouteFileException>(() => RouteFiles.Read(path));

        Assert.Equal("This file isn't an RTZ or S-421 route.", ex.Message);
    }

    [Fact]
    public void Read_OneWaypoint_IsTooFew()
    {
        var path = Write("one.rtz", """
            <route xmlns="http://www.cirm.org/RTZ/1/0" version="1.0">
              <routeInfo routeName="One" />
              <waypoints><waypoint id="1"><position lat="41.0" lon="-70.0" /></waypoint></waypoints>
            </route>
            """);

        var ex = Assert.Throws<RouteFileException>(() => RouteFiles.Read(path));

        Assert.Equal("The route has fewer than two waypoints.", ex.Message);
    }

    [Theory]
    [InlineData("RTZ/rtz-1.0-solent.rtz", RouteSource.Rtz, "Nab to Southampton")]
    [InlineData("RTZ/rtz-1.1-stm.rtz", RouteSource.Rtz, null)]
    [InlineData("RTZ/rtz-1.2-pas.rtz", RouteSource.Rtz, null)]
    [InlineData("S421/RTE-TEST-GBASIC.s421.gml", RouteSource.S421, null)]
    [InlineData("S421/RTE-TEST-GFULL.s421.gml", RouteSource.S421, null)]
    [InlineData("S421/RTE-TEST-GMIN.s421.gml", RouteSource.S421, null)]
    public void Read_UpstreamFixtures(string relativePath, RouteSource source, string? name)
    {
        var path = Fixtures.Upstream(relativePath);
        Assert.SkipWhen(path is null, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");

        var route = RouteFiles.Read(path!);

        Assert.Equal(source, route.Source);
        Assert.NotEmpty(route.Plan.Legs);
        Assert.All(route.Plan.Legs, l => Assert.True(l.Points.Count >= 2));
        Assert.False(string.IsNullOrWhiteSpace(route.Name));
        if (name is not null)
        {
            Assert.Equal(name, route.Name);
        }
    }

    private string Write(string name, string content)
    {
        var path = Path.Combine(_folder.Path, name);
        File.WriteAllText(path, content);
        return path;
    }
}
