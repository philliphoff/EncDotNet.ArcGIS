using EncDotNet.SoundCharts.MapTime;

namespace EncDotNet.SoundCharts.Tests.MapTime;

public class LayerTimeTests
{
    private static readonly DateTime Start = new(2025, 12, 17, 13, 0, 0, DateTimeKind.Utc);

    private static readonly TimeSteps Hourly =
        TimeSteps.Create(Enumerable.Range(0, 4).Select(h => Start.AddHours(h))) ?? throw new InvalidOperationException();

    [Fact]
    public void For_WithDataWritesTheStepAndShowsTheDataRange()
    {
        var time = LayerTime.For(Hourly, Hourly.Resolve(Start.AddMinutes(70)));

        Assert.Equal("2025-12-17T14:00:00Z", time.TimeSegment);
        Assert.Equal(Start.AddMinutes(-30), time.VisibleFrom);
        Assert.Equal(Start.AddHours(3.5), time.VisibleTo);
    }

    [Fact]
    public void For_WithoutDataHidesTheLayerAndLeavesTheTimeAlone()
    {
        var time = LayerTime.For(Hourly, Hourly.Resolve(Start.AddDays(2)));

        Assert.Null(time.TimeSegment);
        Assert.Equal(LayerTime.NeverVisible, time.VisibleFrom);
        Assert.Equal(LayerTime.NeverVisible, time.VisibleTo);
        Assert.True(LayerTime.NeverVisible < Hourly.First);
    }

    [Fact]
    public void For_IsTheSameForTimesThatSnapToTheSameStep()
    {
        // The add-in rewrites the layer only when this changes.
        Assert.Equal(
            LayerTime.For(Hourly, Hourly.Resolve(Start.AddMinutes(50))),
            LayerTime.For(Hourly, Hourly.Resolve(Start.AddMinutes(80))));
    }
}
