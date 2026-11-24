using EncDotNet.SoundCharts.MapTime;
using EncDotNet.SoundCharts.ObjectInfo;

namespace EncDotNet.SoundCharts.Tests.MapTime;

/// <summary>
/// S-124 warnings judged as the chart server judges them (U7, issue #75),
/// with EncDotNet.S100's fixtures: NAVAREA X 120/26 has part 1 in force
/// 10–20 Oct 2026, part 2 ended 5 Oct and part 3 from 25 Oct; NAVAREA X
/// 121/26, in force from 1 Oct, cancels itself on 12 Oct. In the bulletin
/// set, the NAVAREA X in-force bulletin 110/26 of 12 Oct lists 104 but not
/// 105, so 105 is cancelled then.
/// </summary>
public class NavigationalWarningsTests
{
    private const string Parts = "ZZ.S124.NAVAREAX.120.26.";

    private static readonly string[] Drawing = ["navwarn_drawing_parts.gml", "navwarn_drawing_cancelled.gml"];

    [Fact]
    public void Times_AreWhenTheWarningsInForceChange()
    {
        var warnings = Read(Drawing);

        // Each part's range starts and ends (a tick after a range's end), and the cancellation.
        Assert.Equal(
            [
                "2026-10-01T00:00:00Z",
                "2026-10-05T00:00:00.0000001Z",
                "2026-10-10T00:00:00Z",
                "2026-10-12T00:00:00Z",
                "2026-10-20T00:00:00.0000001Z",
                "2026-10-25T00:00:00Z",
                "2026-10-30T00:00:00.0000001Z",
            ],
            warnings.Times.Changes.Select(WarningTimes.FormatSegment));
    }

    [Fact]
    public void Times_IncludeAnInForceBulletinsPublication()
    {
        var warnings = Read(Bulletins());

        Assert.Contains(Utc(2026, 10, 12), warnings.Times.Changes);
        Assert.Equal(WarningTimes.FormatSegment(Utc(2026, 10, 12)), warnings.Times.Segment(Utc(2026, 10, 12, 18)));
    }

    [Theory]
    [InlineData(9, 15, false)] // Not yet in force: its range starts 1 Oct.
    [InlineData(10, 5, true)]
    [InlineData(10, 12, false)] // Cancelled at 00:00 on 12 Oct.
    [InlineData(10, 20, false)]
    public void IsInForce_ACancelledWarning(int month, int day, bool expected)
    {
        var path = Path("navwarn_drawing_cancelled.gml");

        Assert.Equal(expected, Read(Drawing).IsInForce(path, [], Utc(2026, month, day, 12)));
    }

    [Fact]
    public void IsInForce_JudgesThePartsPicked()
    {
        var warnings = Read(Drawing);
        var path = Path("navwarn_drawing_parts.gml");
        var oct15 = Utc(2026, 10, 15, 12);

        Assert.True(warnings.IsInForce(path, [Parts + "1"], oct15));
        Assert.False(warnings.IsInForce(path, [Parts + "2"], oct15)); // Ended 5 Oct.
        Assert.False(warnings.IsInForce(path, [Parts + "3"], oct15)); // Not yet in force: from 25 Oct.
        Assert.True(warnings.IsInForce(path, [Parts + "3"], Utc(2026, 10, 27)));

        // Not a part (the affected area): any part of the warning.
        Assert.True(warnings.IsInForce(path, [Parts + "4"], oct15));
        Assert.Null(warnings.IsInForce(@"C:\elsewhere.gml", [], oct15));
    }

    [Fact]
    public void IsInForce_ALaterInForceBulletinThatLeavesItOutCancelsIt()
    {
        var warnings = Read(Bulletins());
        var path = Path("navwarn_bulletin_navarea_x_105_26.gml");

        Assert.Equal(true, warnings.IsInForce(path, [], Utc(2026, 10, 11)));
        Assert.Equal(false, warnings.IsInForce(path, [], Utc(2026, 10, 12, 12)));

        // Alone, nothing cancels it: the set is what the server serves.
        Assert.Equal(true, Read(["navwarn_bulletin_navarea_x_105_26.gml"]).IsInForce(path, [], Utc(2026, 10, 12, 12)));
    }

    [Fact]
    public void ObjectInformation_SaysInForceExactlyWhenTheMapDrawsIt()
    {
        // D33 and issue #75: Object information and the Navigational warnings layer agree.
        var cancelled = new ChartDataset(Path("navwarn_drawing_cancelled.gml"));
        var parts = new ChartDataset(Path("navwarn_drawing_parts.gml"));
        var warnings = Read(Drawing);
        using var service = new ObjectInfoService();

        foreach (var time in new[] { Utc(2026, 9, 15), Utc(2026, 10, 3), Utc(2026, 10, 11), Utc(2026, 10, 15), Utc(2026, 10, 27) })
        {
            // NAVAREA X 121/26's point, and 120/26 part 1's point.
            var report = service.Identify([cancelled, parts], -16.45, 146.05, 200, time, TestContext.Current.CancellationToken);
            var reported = Assert.Single(report.Datasets, d => d.Dataset == cancelled);
            Assert.Equal(warnings.IsInForce(cancelled.Path, [], time), reported.InForce);

            report = service.Identify([cancelled, parts], -16.50, 146.00, 200, time, TestContext.Current.CancellationToken);
            reported = Assert.Single(report.Datasets, d => d.Dataset == parts);
            Assert.Equal(warnings.IsInForce(parts.Path, reported.Features.Select(f => f.FeatureRef), time), reported.InForce);
            Assert.Contains(reported.Features, f => f.FeatureRef == Parts + "1");
        }

        // Without map time, no validity is said: every warning draws.
        var untimed = service.Identify([cancelled, parts], -16.45, 146.05, 200, time: null, TestContext.Current.CancellationToken);
        Assert.All(untimed.Datasets, d => Assert.Null(d.InForce));
    }

    private static NavigationalWarnings Read(IEnumerable<string> names)
    {
        var warnings = NavigationalWarnings.Read(names.Select(Path), factory: null, out var failed);
        Assert.Empty(failed);
        return warnings;
    }

    private static string[] Bulletins()
    {
        Path("navwarn_bulletin_navarea_x_105_26.gml");
        var folder = System.IO.Path.GetDirectoryName(Fixtures.Upstream("S124/navwarn_bulletin_navarea_x_105_26.gml"))!;
        return [.. Directory.GetFiles(folder, "navwarn_bulletin_*.gml").Select(System.IO.Path.GetFileName).OfType<string>()];
    }

    private static string Path(string name)
    {
        var path = Fixtures.Upstream("S124/" + name);
        Assert.SkipWhen(path is null, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        return path!;
    }

    private static DateTime Utc(int year, int month, int day, int hour = 0) => new(year, month, day, hour, 0, 0, DateTimeKind.Utc);
}
