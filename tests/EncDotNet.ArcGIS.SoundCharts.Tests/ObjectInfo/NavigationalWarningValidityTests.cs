using EncDotNet.S100.Datasets.S124.DataModel;
using EncDotNet.SoundCharts.ObjectInfo;

namespace EncDotNet.SoundCharts.Tests.ObjectInfo;

public class NavigationalWarningValidityTests
{
    private static readonly DateTimeOffset Noon = new(2026, 3, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsInForce_WithoutDatesIsInForce()
    {
        Assert.True(NavigationalWarningValidity.IsInForce(null, null, [], Noon));
        Assert.True(NavigationalWarningValidity.IsInForce(null, null, [[]], Noon));
    }

    [Fact]
    public void IsInForce_NotBeforeItsPublication()
    {
        Assert.False(NavigationalWarningValidity.IsInForce(Noon.AddHours(1), null, [], Noon));
        Assert.True(NavigationalWarningValidity.IsInForce(Noon, null, [], Noon));
    }

    [Fact]
    public void IsInForce_NotFromItsCancellation()
    {
        Assert.False(NavigationalWarningValidity.IsInForce(null, Noon, [], Noon));
        Assert.True(NavigationalWarningValidity.IsInForce(null, Noon.AddSeconds(1), [], Noon));
    }

    [Fact]
    public void IsInForce_FollowsThePartsDateRanges()
    {
        IReadOnlyList<S124DateRange> tomorrow = [new(Noon.AddDays(1), Noon.AddDays(2))];
        IReadOnlyList<S124DateRange> today = [new(Noon.AddHours(-1), null)];

        Assert.False(NavigationalWarningValidity.IsInForce(null, null, [tomorrow], Noon));
        Assert.True(NavigationalWarningValidity.IsInForce(null, null, [tomorrow, today], Noon));

        // A part without dates applies whenever the warning does.
        Assert.True(NavigationalWarningValidity.IsInForce(null, null, [tomorrow, []], Noon));
    }

    [Theory]
    [InlineData(2026, 9, 15, false)] // Before its publication on 1 Oct.
    [InlineData(2026, 10, 5, true)]
    [InlineData(2026, 10, 20, false)] // After its cancellation on 12 Oct.
    public void IsInForce_ReadsAnUpstreamWarning(int year, int month, int day, bool expected)
    {
        // Published 2026-10-01, cancels itself 2026-10-12, one part dated 1-30 Oct.
        var path = Fixtures.Upstream("S124/navwarn_drawing_cancelled.gml");
        Assert.SkipWhen(path is null, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");

        Assert.Equal(expected, NavigationalWarningValidity.IsInForce(path, new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Utc)));
    }
}
