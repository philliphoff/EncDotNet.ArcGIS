using EncDotNet.SoundCharts.MapTime;

namespace EncDotNet.SoundCharts.Tests.MapTime;

public class TimeStepsTests
{
    private static readonly DateTime Start = Utc(2025, 12, 17, 13);

    /// <summary>Hourly steps 13:00 to 18:00 on 17 Dec 2025, like the Charleston S-104 sample's first hours.</summary>
    private static readonly TimeSteps Hourly = Steps(Enumerable.Range(0, 6).Select(h => Start.AddHours(h)));

    [Fact]
    public void TryReadTileJson_ReadsTheServersTimes()
    {
        const string json = """
            { "tilejson": "3.0.0", "minzoom": 9,
              "s100": { "palette": "day", "times": ["2025-12-17T14:00:00Z", "2025-12-17T13:00:00Z", "2025-12-17T13:00:00Z"] } }
            """;

        var steps = TimeSteps.TryReadTileJson(json);

        Assert.NotNull(steps);
        Assert.Equal([Start, Start.AddHours(1)], steps.Steps);
        Assert.All(steps.Steps, s => Assert.Equal(DateTimeKind.Utc, s.Kind));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("""{ "tilejson": "3.0.0" }""")]
    [InlineData("""{ "s100": { "palette": "day" } }""")]
    [InlineData("""{ "s100": { "times": [] } }""")]
    [InlineData("""{ "s100": { "times": ["yesterday", 3] } }""")]
    public void TryReadTileJson_IsNullForAChartWithoutTime(string? json)
    {
        Assert.Null(TimeSteps.TryReadTileJson(json));
    }

    [Fact]
    public void Interval_IsTheMostCommonStep()
    {
        var steps = Steps([Start, Start.AddMinutes(30), Start.AddMinutes(60), Start.AddMinutes(90), Start.AddHours(6)]);

        Assert.Equal(TimeSpan.FromMinutes(30), steps.Interval);
        Assert.Equal(TimeSpan.FromHours(1), Hourly.Interval);
        Assert.Equal(TimeSteps.SingleStepInterval, Steps([Start]).Interval);
    }

    [Fact]
    public void Resolve_DrawsAnExactStep()
    {
        var status = Hourly.Resolve(Start.AddHours(2));

        Assert.True(status.HasData);
        Assert.Equal(Start.AddHours(2), status.Step);
    }

    [Theory]
    [InlineData(20, 0)]
    [InlineData(29, 0)]
    [InlineData(30, 0)] // A tie goes to the earlier step.
    [InlineData(31, 1)]
    [InlineData(59, 1)]
    public void Resolve_SnapsToTheNearestStep(int minutes, int hours)
    {
        Assert.Equal(Start.AddHours(hours), Hourly.Resolve(Start.AddMinutes(minutes)).Step);
    }

    [Theory]
    [InlineData(-30)]
    [InlineData(5 * 60 + 30)]
    public void Resolve_IsInRangeWithinHalfAStepOfTheEnds(int minutes)
    {
        Assert.True(Hourly.Resolve(Start.AddMinutes(minutes)).HasData);
    }

    [Fact]
    public void Resolve_BeforeTheDataSaysNoDataAndPointsToTheFirstStep()
    {
        var status = Hourly.Resolve(Start.AddMinutes(-31));

        Assert.False(status.HasData);
        Assert.Null(status.Previous);
        Assert.Equal(Start, status.Next);
        Assert.Equal(Start, status.Nearest);
    }

    [Fact]
    public void Resolve_AfterTheDataNeverSnapsToTheLastStepSilently()
    {
        // Step 0: the server drew the last step for any later time (D31).
        var status = Hourly.Resolve(Start.AddDays(1));

        Assert.False(status.HasData);
        Assert.Null(status.Step);
        Assert.Equal(Hourly.Last, status.Previous);
        Assert.Null(status.Next);
        Assert.Equal(Hourly.Last, status.Nearest);
    }

    [Fact]
    public void Resolve_InAGapSaysNoDataWithBothSides()
    {
        // 30-minute steps, then nothing for 3 hours.
        var steps = Steps([Start, Start.AddMinutes(30), Start.AddMinutes(60), Start.AddHours(4), Start.AddHours(4.5)]);

        var gap = steps.Resolve(Start.AddMinutes(160));
        Assert.False(gap.HasData);
        Assert.Equal(Start.AddMinutes(60), gap.Previous);
        Assert.Equal(Start.AddHours(4), gap.Next);
        Assert.Equal(Start.AddHours(4), gap.Nearest);

        // Within one interval of a step is still data.
        Assert.Equal(Start.AddMinutes(60), steps.Resolve(Start.AddMinutes(90)).Step);
    }

    [Fact]
    public void Resolve_TakesLocalTimesAsTheirUtcInstant()
    {
        var local = Start.AddHours(1).ToLocalTime();

        Assert.Equal(Start.AddHours(1), Hourly.Resolve(local).Step);
        Assert.Equal(Start.AddHours(1), Hourly.Resolve(DateTime.SpecifyKind(Start.AddHours(1), DateTimeKind.Unspecified)).Step);
    }

    [Fact]
    public void DefaultTime_IsNowSnappedWhenTheDataCoversIt()
    {
        Assert.Equal(Start.AddHours(3), Hourly.DefaultTime(Start.AddHours(3).AddMinutes(10)));
    }

    [Fact]
    public void DefaultTime_IsTheNearerEndOtherwise()
    {
        // A forecast that starts tomorrow opens at its first step; last week's data at its last.
        Assert.Equal(Hourly.First, Hourly.DefaultTime(Start.AddDays(-1)));
        Assert.Equal(Hourly.Last, Hourly.DefaultTime(Start.AddDays(7)));
    }

    [Fact]
    public void Range_WidensTheStepsByHalfAnInterval()
    {
        Assert.Equal((Start.AddMinutes(-30), Start.AddHours(5.5)), Hourly.Range);
    }

    [Fact]
    public void Union_MergesLayersSteps()
    {
        var other = Steps([Start.AddMinutes(30), Start.AddHours(10)]);

        var union = TimeSteps.Union([Hourly, null, other]);

        Assert.NotNull(union);
        Assert.Equal(8, union.Steps.Count);
        Assert.Equal(Start.AddHours(10), union.Last);
        Assert.Null(TimeSteps.Union([null]));
    }

    [Fact]
    public void FormatSegment_IsTheServersIsoForm()
    {
        Assert.Equal("2025-12-17T13:00:00Z", TimeSteps.FormatSegment(Start));
        Assert.Equal("2025-12-17T13:00:00Z", TimeSteps.FormatSegment(Start.ToLocalTime()));
    }

    [Fact]
    public void Create_IsNullWithoutSteps()
    {
        Assert.Null(TimeSteps.Create([]));
    }

    [Fact]
    public void Steps_FromTheChartServerSample_SnapAndRange()
    {
        // The S-104 Charleston sample's TileJSON lists 180 hourly steps from 13:00 on 17 Dec 2025.
        var times = string.Join(",", Enumerable.Range(0, 180).Select(h => $"\"{TimeSteps.FormatSegment(Start.AddHours(h))}\""));
        var steps = TimeSteps.TryReadTileJson($$"""{ "s100": { "times": [{{times}}] } }""");

        Assert.NotNull(steps);
        Assert.Equal(Start.AddHours(179), steps.Last);
        Assert.Equal(Start.AddHours(42), steps.Resolve(Start.AddHours(42).AddMinutes(12)).Step);
        Assert.False(steps.Resolve(steps.Last.AddHours(1)).HasData);
    }

    private static TimeSteps Steps(IEnumerable<DateTime> steps) =>
        TimeSteps.Create(steps) ?? throw new InvalidOperationException("No steps.");

    private static DateTime Utc(int year, int month, int day, int hour) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);
}
