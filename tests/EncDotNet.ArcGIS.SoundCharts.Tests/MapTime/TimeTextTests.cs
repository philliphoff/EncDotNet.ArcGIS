using System.Globalization;
using EncDotNet.SoundCharts.MapTime;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts.Tests.MapTime;

public class TimeTextTests
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
        Assert.Equal(expected, TimeText.FormatSpan(TimeSpan.FromMinutes(minutes)));
        Assert.Equal(expected, TimeText.FormatSpan(TimeSpan.FromMinutes(-minutes)));
    }

    [Fact]
    public void FormatTime_IsUtc()
    {
        using var culture = new CultureScope("en-US");
        Assert.Equal("24 Dec 2025 12:00 UTC", TimeText.FormatTime(Last));
    }

    [Fact]
    public void Describe_SaysTheStepWithData()
    {
        using var culture = new CultureScope("en-US");
        var status = new TimeStatus(Last.AddMinutes(10), Last);

        Assert.Equal("Water level · 24 Dec 2025 12:00 UTC", TimeText.Describe("Water level", status));
    }

    [Fact]
    public void Describe_AfterTheDataPointsBack()
    {
        using var culture = new CultureScope("en-US");
        var status = new TimeStatus(Last.AddHours(3), null, Previous: Last);

        Assert.Equal(
            "Water level · No data at map time · last 24 Dec 2025 12:00 UTC, 3 h earlier",
            TimeText.Describe("Water level", status));
    }

    [Fact]
    public void Describe_BeforeTheDataPointsForward()
    {
        using var culture = new CultureScope("en-US");
        var status = new TimeStatus(Last.AddDays(-2), null, Next: Last);

        Assert.Equal(
            "Surface currents · No data at map time · next 24 Dec 2025 12:00 UTC, 2 days later",
            TimeText.Describe("Surface currents", status));
    }

    [Fact]
    public void Summarize_SaysWhenThereIsNothingToFollow()
    {
        Assert.Equal(Strings.Status_NoTimeLayers, TimeText.Summarize([]));
        Assert.Equal(Strings.Status_NoMapTime, TimeText.Summarize([("Water level", null)]));
    }

    [Fact]
    public void Summarize_GivesALinePerLayer()
    {
        var summary = TimeText.Summarize(
        [
            ("Surface currents", new TimeStatus(Last, Last)),
            ("Water level", new TimeStatus(Last.AddHours(3), null, Previous: Last)),
        ]);

        Assert.Equal(2, summary.Split(Environment.NewLine).Length);
    }

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
