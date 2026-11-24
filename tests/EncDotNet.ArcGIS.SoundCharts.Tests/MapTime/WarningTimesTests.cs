using EncDotNet.SoundCharts.MapTime;

namespace EncDotNet.SoundCharts.Tests.MapTime;

/// <summary>The Navigational warnings layer's time segment (U7, issue #75).</summary>
public class WarningTimesTests
{
    private static readonly DateTime Oct1 = Utc(2026, 10, 1);
    private static readonly DateTime Oct12 = Utc(2026, 10, 12);

    private static readonly Uri Capabilities = new("http://127.0.0.1:52100/abc-123/wmts/1.0.0/WMTSCapabilities.xml");

    [Fact]
    public void Segment_IsTheLatestChangeAtOrBeforeMapTime()
    {
        var times = new WarningTimes([Oct12, Oct1, Oct12]);

        Assert.Equal([Oct1, Oct12], times.Changes);
        Assert.Equal("2026-10-01T00:00:00Z", times.Segment(Oct1));
        Assert.Equal("2026-10-01T00:00:00Z", times.Segment(Oct12.AddTicks(-1)));
        Assert.Equal("2026-10-12T00:00:00Z", times.Segment(Oct12));
        Assert.Equal("2026-10-12T00:00:00Z", times.Segment(Utc(2027, 1, 1)));
    }

    [Fact]
    public void Segment_IsSharedBetweenChanges_SoTilesAreCached()
    {
        var times = new WarningTimes([Oct1, Oct12]);

        var segments = Enumerable.Range(0, 11 * 24).Select(h => times.Segment(Oct1.AddHours(h))).Distinct();

        Assert.Single(segments);
    }

    [Fact]
    public void Segment_BeforeTheFirstChange_IsOneEarlyTime()
    {
        var times = new WarningTimes([Oct1]);

        Assert.Equal("1900-01-01T00:00:00Z", times.Segment(Utc(2026, 9, 1)));
        Assert.Equal(times.Segment(Utc(2020, 1, 1)), times.Segment(Oct1.AddSeconds(-1)));
        Assert.Equal("1900-01-01T00:00:00Z", WarningTimes.None.Segment(Oct1));
    }

    [Fact]
    public void Segment_KeepsATickAfterARangeEnds()
    {
        // A range ending at 20 Oct 00:00 is in force then and ended a tick later.
        var ended = Utc(2026, 10, 20).AddTicks(1);
        var times = new WarningTimes([ended]);

        Assert.Equal("2026-10-20T00:00:00.0000001Z", times.Segment(Utc(2026, 10, 20, 0, 0, 1)));
        Assert.Equal("1900-01-01T00:00:00Z", times.Segment(Utc(2026, 10, 20)));
    }

    [Fact]
    public void Segment_TakesMapTimeInAnyKind()
    {
        var times = new WarningTimes([Oct12]);
        var unspecified = DateTime.SpecifyKind(Oct12, DateTimeKind.Unspecified);

        Assert.Equal("2026-10-12T00:00:00Z", times.Segment(unspecified));
        Assert.Equal("2026-10-12T00:00:00Z", times.Segment(Oct12.ToLocalTime()));
    }

    [Fact]
    public void TemplateUrl_WritesTheTimeAfterTheStyle()
    {
        Assert.Equal(
            "http://127.0.0.1:52100/abc-123/wmts/1.0.0/s100/{Style}/2026-10-12T00:00:00Z/{TileMatrixSet}/{TileMatrix}/{TileRow}/{TileCol}.png",
            WarningTimes.TemplateUrl(Capabilities, "2026-10-12T00:00:00Z"));

        // Without a time, the capabilities' own template (s100 tiles serve, S-124 alone).
        Assert.Equal(
            "http://127.0.0.1:52100/abc-123/wmts/1.0.0/s100/{Style}/{TileMatrixSet}/{TileMatrix}/{TileRow}/{TileCol}.png",
            WarningTimes.TemplateUrl(Capabilities, null));
    }

    [Theory]
    [InlineData("2026-10-12T00:00:00Z")]
    [InlineData("2026-10-20T00:00:00.0000001Z")]
    [InlineData(null)]
    public void TryReadTemplate_ReadsBackTheTime(string? segment)
    {
        Assert.True(WarningTimes.TryReadTemplate(WarningTimes.TemplateUrl(Capabilities, segment), out var read));
        Assert.Equal(segment, read);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("http://127.0.0.1:52100/abc-123/wmts/1.0.0/s100/{Style}/{Time}/{TileMatrixSet}/{TileMatrix}/{TileRow}/{TileCol}.png")]
    [InlineData("https://example.com/tiles/{TileMatrix}/{TileRow}/{TileCol}.png")]
    public void TryReadTemplate_LeavesOtherTemplatesAlone(string? template)
    {
        // A {Time} placeholder is Pro's to fill from the Time dimension, not a time to carry.
        Assert.False(WarningTimes.TryReadTemplate(template, out _));
    }

    private static DateTime Utc(int year, int month, int day, int hour = 0, int minute = 0, int second = 0) =>
        new(year, month, day, hour, minute, second, DateTimeKind.Utc);
}
