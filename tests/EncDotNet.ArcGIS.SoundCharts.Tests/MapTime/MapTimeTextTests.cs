using System.Globalization;
using EncDotNet.SoundCharts.MapTime;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts.Tests.MapTime;

public class MapTimeTextTests
{
    private static readonly DateTime Last = new(2025, 12, 24, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, "1 min")]
    [InlineData(45, "45 min")]
    [InlineData(89, "89 min")]
    [InlineData(180, "3 h")]
    [InlineData(47 * 60, "47 h")]
    [InlineData(3 * 24 * 60, "3 days")]
    public void FormatSpan_UsesMinutesHoursOrDays(int minutes, string expected)
    {
        using var culture = new CultureScope("en-US");
        Assert.Equal(expected, MapTimeText.FormatSpan(TimeSpan.FromMinutes(minutes)));
        Assert.Equal(expected, MapTimeText.FormatSpan(TimeSpan.FromMinutes(-minutes)));
    }

    [Fact]
    public void FormatTime_IsUtc()
    {
        using var culture = new CultureScope("en-US");
        Assert.Equal("24 Dec 2025 12:00 UTC", MapTimeText.FormatTime(Last));
    }

    [Fact]
    public void Describe_SaysTheStepWithData()
    {
        using var culture = new CultureScope("en-US");
        var status = new TimeStatus(Last.AddMinutes(10), Last);

        Assert.Equal("Water level · 24 Dec 2025 12:00 UTC", MapTimeText.Describe("Water level", status));
    }

    [Fact]
    public void Describe_AfterTheDataPointsBack()
    {
        using var culture = new CultureScope("en-US");
        var status = new TimeStatus(Last.AddHours(3), null, Previous: Last);

        Assert.Equal(
            "Water level · No data at map time · last 24 Dec 2025 12:00 UTC, 3 h earlier",
            MapTimeText.Describe("Water level", status));
    }

    [Fact]
    public void Describe_BeforeTheDataPointsForward()
    {
        using var culture = new CultureScope("en-US");
        var status = new TimeStatus(Last.AddDays(-2), null, Next: Last);

        Assert.Equal(
            "Surface currents · No data at map time · next 24 Dec 2025 12:00 UTC, 2 days later",
            MapTimeText.Describe("Surface currents", status));
    }

    [Theory]
    [InlineData(2022, 9, 28, 3, "Weather · est. 28 Sep 2022 03:00 UTC, between forecast points")]
    [InlineData(2022, 9, 28, 12, "Weather · 28 Sep 2022 12:00 UTC")]
    [InlineData(2022, 9, 25, 12, "Weather · 25 Sep 2022 12:00 UTC · best track")]
    [InlineData(2022, 10, 5, 0, "Weather · Beyond forecast · last forecast point 2 Oct 2022 00:00 UTC, 3 days earlier")]
    [InlineData(2022, 9, 21, 18, "Weather · Before track · track starts 22 Sep 2022 18:00 UTC, 24 h later")]
    public void Describe_AStormSaysWhereItIsOnItsTrack(int year, int month, int day, int hour, string expected)
    {
        // Issue #64: Hurricane Ian advisory 20, forecast points 28 Sep 00Z to
        // 2 Oct 00Z, past track from 22 Sep 18Z.
        using var culture = new CultureScope("en-US");
        var ian = TimeSteps.Create(
            [Utc(2022, 9, 28, 0), Utc(2022, 9, 28, 12), Utc(2022, 9, 29, 0), Utc(2022, 10, 2, 0)],
            TimeSelection.Continuous,
            (Utc(2022, 9, 22, 18), Utc(2022, 10, 2, 0)))!;

        Assert.Equal(expected, MapTimeText.Describe("Weather", ian.Resolve(Utc(year, month, day, hour))));
    }

    [Fact]
    public void Summarize_SaysWhenThereIsNothingToFollow()
    {
        Assert.Equal(Strings.Status_NoTimeLayers, MapTimeText.Summarize([]));
        Assert.Equal(Strings.Status_NoMapTime, MapTimeText.Summarize([("Water level", null)]));
    }

    [Fact]
    public void Summarize_GivesALinePerLayer()
    {
        var summary = MapTimeText.Summarize(
        [
            ("Surface currents", new TimeStatus(Last, Last)),
            ("Water level", new TimeStatus(Last.AddHours(3), null, Previous: Last)),
        ]);

        Assert.Equal(2, summary.Split(Environment.NewLine).Length);
    }

    private static DateTime Utc(int year, int month, int day, int hour) => new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

        public CultureScope(string name)
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _culture;
            CultureInfo.CurrentUICulture = _uiCulture;
        }
    }
}
