using System.Globalization;
using EncDotNet.SoundCharts.MapTime;

namespace EncDotNet.SoundCharts.Tests.MapTime;

/// <summary>
/// Issue #64: each time layer picks what it draws by its datasets' time
/// selection, as the chart server reports it.
/// </summary>
public class TimeSelectionTests
{
    /// <summary>
    /// What the pinned chart server's <c>/datasets</c> says about Hurricane
    /// Ian advisory 20 (EncDotNet.S100's <c>tests/datasets/TC/al092022</c>),
    /// cut to the time members: its forecast points, and a range that starts
    /// with the past track.
    /// </summary>
    private const string IanDatasets = """
        {
          "notForNavigation": true,
          "datasets": [
            {
              "id": "al092022", "file": "al092022", "product": "TC",
              "times": [
                "2022-09-28T00:00:00Z", "2022-09-28T12:00:00Z", "2022-09-29T00:00:00Z", "2022-09-29T12:00:00Z",
                "2022-09-30T00:00:00Z", "2022-09-30T12:00:00Z", "2022-10-01T00:00:00Z", "2022-10-02T00:00:00Z"
              ],
              "issueTime": "2022-09-28T03:00:00Z",
              "timeSelection": "continuous",
              "timeRange": ["2022-09-22T18:00:00Z", "2022-10-02T00:00:00Z"]
            }
          ]
        }
        """;

    /// <summary>The same server's TileJSON <c>s100</c> member, cut to the time members.</summary>
    private const string IanTileJson = """
        {
          "tilejson": "3.0.0",
          "s100": {
            "times": [
              "2022-09-28T00:00:00Z", "2022-09-28T12:00:00Z", "2022-09-29T00:00:00Z", "2022-09-29T12:00:00Z",
              "2022-09-30T00:00:00Z", "2022-09-30T12:00:00Z", "2022-10-01T00:00:00Z", "2022-10-02T00:00:00Z"
            ],
            "datasets": [{ "name": "al092022", "spec": "TC", "timeSelection": "continuous" }]
          }
        }
        """;

    private static readonly DateTime S104Start = Utc(2025, 12, 17, 13);

    /// <summary>Hourly at-or-before steps, 13:00 to 18:00, like S-104.</summary>
    private static readonly TimeSteps WaterLevel =
        TimeSteps.Create(Enumerable.Range(0, 6).Select(h => S104Start.AddHours(h)), TimeSelection.AtOrBefore)!;

    private static TimeSteps Ian => TimeSteps.TryReadTileJson(IanTileJson)!.With(DatasetTimes.TryRead(IanDatasets));

    [Theory]
    [InlineData("nearest", TimeSelection.Nearest)]
    [InlineData("atOrBefore", TimeSelection.AtOrBefore)]
    [InlineData("continuous", TimeSelection.Continuous)]
    public void TryParse_ReadsTheServersNames(string name, TimeSelection expected)
    {
        Assert.Equal(expected, TimeSelections.TryParse(name));
        Assert.Equal(name, TimeSelections.Name(expected));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Continuous")]
    [InlineData("validity")]
    public void TryParse_IsNullForAnyOtherName(string? name)
    {
        Assert.Null(TimeSelections.TryParse(name));
    }

    [Theory]
    [InlineData(new TimeSelection[0], TimeSelection.Nearest)]
    [InlineData(new[] { TimeSelection.AtOrBefore, TimeSelection.AtOrBefore }, TimeSelection.AtOrBefore)]
    [InlineData(new[] { TimeSelection.Nearest, TimeSelection.Continuous }, TimeSelection.Continuous)]
    [InlineData(new[] { TimeSelection.AtOrBefore, TimeSelection.Nearest, TimeSelection.Continuous }, TimeSelection.Continuous)]
    [InlineData(new[] { TimeSelection.Nearest, TimeSelection.AtOrBefore }, TimeSelection.AtOrBefore)]
    public void Combine_PrefersTheStormThenNeverALaterStep(TimeSelection[] selections, TimeSelection expected)
    {
        Assert.Equal(expected, TimeSelections.Combine(selections));
    }

    [Fact]
    public void DatasetTimes_ReadsSelectionsAndTheUnionOfRanges()
    {
        const string json = """
            { "datasets": [
                { "id": "chart" },
                { "id": "ian", "timeSelection": "continuous", "timeRange": ["2022-09-22T18:00:00Z", "2022-10-02T00:00:00Z"] },
                { "id": "gfs", "timeSelection": "nearest", "timeRange": ["2022-09-27T00:00:00Z", "2022-10-04T00:00:00Z"] },
                { "id": "odd", "timeSelection": "atOrBefore", "timeRange": ["2022-09-30T00:00:00Z"] }
            ] }
            """;

        var times = DatasetTimes.TryRead(json);

        Assert.NotNull(times);
        Assert.Equal([TimeSelection.Continuous, TimeSelection.Nearest, TimeSelection.AtOrBefore], times.Selections);
        Assert.Equal((Utc(2022, 9, 22, 18), Utc(2022, 10, 4, 0)), times.Span);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not json")]
    [InlineData("""{ "datasets": [] }""")]
    [InlineData("""{ "datasets": [{ "id": "an older server", "times": ["2022-09-28T00:00:00Z"] }] }""")]
    public void DatasetTimes_IsNullWithoutTimeSelections(string? json)
    {
        Assert.Null(DatasetTimes.TryRead(json));
    }

    [Fact]
    public void TryReadTileJson_ReadsTheSelection()
    {
        Assert.Equal(TimeSelection.Continuous, TimeSteps.TryReadTileJson(IanTileJson)!.Selection);
    }

    [Fact]
    public void TryReadTileJson_WithoutSelectionsIsNearest_AsBeforeTheServerSaid()
    {
        var steps = TimeSteps.TryReadTileJson("""{ "s100": { "times": ["2025-12-17T13:00:00Z"] } }""");

        Assert.Equal(TimeSelection.Nearest, steps!.Selection);
    }

    [Fact]
    public void With_NothingFromTheServerKeepsTheSteps()
    {
        Assert.Same(WaterLevel, WaterLevel.With(null));
    }

    [Fact]
    public void Continuous_RangeAndExtentAreTheServersTimeRange_WithThePastTrack()
    {
        var ian = Ian;

        Assert.Equal((Utc(2022, 9, 22, 18), Utc(2022, 10, 2, 0)), ian.Range);
        Assert.Equal(ian.Range, ian.Extent);
        Assert.Equal(Utc(2022, 9, 28, 0), ian.First);
    }

    [Fact]
    public void Continuous_WithoutTheServersRangeIsTheSteps()
    {
        Assert.Equal((Utc(2022, 9, 28, 0), Utc(2022, 10, 2, 0)), TimeSteps.TryReadTileJson(IanTileJson)!.Range);
    }

    [Theory]
    [InlineData("2022-09-28T03:00:00Z", "2022-09-28T03:00:00Z")]
    [InlineData("2022-09-28T06:00:00Z", "2022-09-28T06:00:00Z")]
    [InlineData("2022-09-28T06:14:59Z", "2022-09-28T06:00:00Z")]
    [InlineData("2022-09-28T06:15:00Z", "2022-09-28T06:15:00Z")]
    [InlineData("2022-09-25T12:00:00Z", "2022-09-25T12:00:00Z")]
    public void Continuous_DrawsMapTimeRoundedDown(string mapTime, string drawn)
    {
        var status = Ian.Resolve(Parse(mapTime));

        Assert.True(status.HasData);
        Assert.Equal(Parse(drawn), status.Step);
        Assert.Equal(drawn, LayerTime.For(Ian, status).TimeSegment);
    }

    [Fact]
    public void Continuous_BetweenForecastPointsIsEstimated()
    {
        var status = Ian.Resolve(Utc(2022, 9, 28, 3));

        Assert.True(status.IsBetweenSteps);
        Assert.False(status.IsBeforeSteps);
        Assert.Equal(Utc(2022, 9, 28, 0), status.Previous);
        Assert.Equal(Utc(2022, 9, 28, 12), status.Next);
    }

    [Fact]
    public void Continuous_OnAForecastPointIsNotEstimated()
    {
        var status = Ian.Resolve(Utc(2022, 9, 28, 12));

        Assert.False(status.IsBetweenSteps);
        Assert.False(status.IsBeforeSteps);
        Assert.Equal(Utc(2022, 9, 28, 12), status.Previous);
    }

    [Fact]
    public void Continuous_BeforeTheFirstForecastPointIsThePastTrack()
    {
        var status = Ian.Resolve(Utc(2022, 9, 25, 12));

        Assert.True(status.IsBeforeSteps);
        Assert.False(status.IsBetweenSteps);
    }

    [Fact]
    public void Continuous_OutsideItsRangeStillDrawsAndIsNeverHidden()
    {
        // The server draws the cone and track without the storm then.
        var after = Ian.Resolve(Utc(2022, 10, 5, 0));
        var before = Ian.Resolve(Utc(2022, 9, 20, 0));

        Assert.False(after.HasData);
        Assert.True(after.Draws);
        Assert.Equal(Utc(2022, 10, 2, 0), after.Nearest);
        Assert.Equal(Utc(2022, 9, 22, 18), before.Nearest);

        var time = LayerTime.For(Ian, after);
        Assert.Equal("2022-10-05T00:00:00Z", time.TimeSegment);
        Assert.False(time.UsesVisibilityExtent);
    }

    [Fact]
    public void Continuous_TimesInTheSameQuarterHourShareTheirTiles()
    {
        Assert.Equal(
            LayerTime.For(Ian, Ian.Resolve(Utc(2022, 9, 28, 3).AddMinutes(1))),
            LayerTime.For(Ian, Ian.Resolve(Utc(2022, 9, 28, 3).AddMinutes(14))));
        Assert.NotEqual(
            LayerTime.For(Ian, Ian.Resolve(Utc(2022, 9, 28, 3))),
            LayerTime.For(Ian, Ian.Resolve(Utc(2022, 9, 28, 6))));
    }

    [Fact]
    public void Continuous_PreviousAndNextDataStillStepBetweenForecastPoints()
    {
        Assert.Equal(Utc(2022, 9, 28, 12), Ian.After(Utc(2022, 9, 28, 3)));
        Assert.Equal(Utc(2022, 9, 28, 0), Ian.Before(Utc(2022, 9, 28, 3)));
    }

    [Fact]
    public void Continuous_DefaultTimeIsNowRoundedDownInRange()
    {
        Assert.Equal(Utc(2022, 9, 28, 3), Ian.DefaultTime(Utc(2022, 9, 28, 3).AddMinutes(7)));
        Assert.Equal(Utc(2022, 10, 2, 0), Ian.DefaultTime(Utc(2026, 11, 23, 9)));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(59, 0)]
    [InlineData(60, 1)]
    [InlineData(119, 1)]
    [InlineData(5 * 60 + 59, 5)]
    public void AtOrBefore_NeverDrawsALaterStep(int minutes, int hours)
    {
        var status = WaterLevel.Resolve(S104Start.AddMinutes(minutes));

        Assert.Equal(S104Start.AddHours(hours), status.Step);
        Assert.Equal(TimeSteps.FormatSegment(S104Start.AddHours(hours)), LayerTime.For(WaterLevel, status).TimeSegment);
    }

    [Fact]
    public void AtOrBefore_BeforeTheFirstStepHasNoData()
    {
        var status = WaterLevel.Resolve(S104Start.AddMinutes(-1));

        Assert.False(status.HasData);
        Assert.False(status.Draws);
        Assert.Equal(S104Start, status.Nearest);
        Assert.Equal(LayerTime.NeverVisible, LayerTime.For(WaterLevel, status).VisibleFrom);
    }

    [Fact]
    public void AtOrBefore_HoldsTheLastStepForOneInterval()
    {
        var last = S104Start.AddHours(5);

        Assert.Equal(last, WaterLevel.Resolve(last.AddHours(1)).Step);
        Assert.Null(WaterLevel.Resolve(last.AddHours(1).AddMinutes(1)).Step);
        Assert.Equal((S104Start, last.AddHours(1)), WaterLevel.Range);
        Assert.Equal((S104Start, last), WaterLevel.Extent);
    }

    [Fact]
    public void AtOrBefore_InAGapHasNoData()
    {
        var steps = TimeSteps.Create([S104Start, S104Start.AddHours(1), S104Start.AddHours(2), S104Start.AddHours(8)], TimeSelection.AtOrBefore)!;

        var status = steps.Resolve(S104Start.AddHours(5));

        Assert.False(status.HasData);
        Assert.Equal(S104Start.AddHours(2), status.Previous);
        Assert.Equal(S104Start.AddHours(8), status.Next);
    }

    [Fact]
    public void Union_WithAStormIsContinuousOverEveryLayersRange()
    {
        var union = TimeSteps.Union([WaterLevel, Ian]);

        Assert.NotNull(union);
        Assert.Equal(TimeSelection.Continuous, union.Selection);
        Assert.Equal((Utc(2022, 9, 22, 18), S104Start.AddHours(6)), union.Range);
    }

    private static DateTime Utc(int year, int month, int day, int hour) => new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    private static DateTime Parse(string text) =>
        DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
}
