using System.Globalization;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Themes;

namespace EncDotNet.SoundCharts.Tests.Logging;

public class LogRowTests
{
    private static LogRow Row(LogLevel level) =>
        new(new LogEntry(DateTimeOffset.UnixEpoch, level, "chart 3f9a1c02", "Started on port 52448."));

    [Theory]
    [InlineData(LogLevel.Debug, "Debug", SoundChartsTone.Neutral)]
    [InlineData(LogLevel.Info, "Info", SoundChartsTone.Neutral)]
    [InlineData(LogLevel.Warning, "Warn", SoundChartsTone.Warning)]
    [InlineData(LogLevel.Error, "Error", SoundChartsTone.Danger)]
    public void LevelAndTone_FollowTheEntry(LogLevel level, string label, SoundChartsTone tone)
    {
        using var culture = new CultureScope(CultureInfo.GetCultureInfo("en-US"));
        var row = Row(level);

        Assert.Equal(label, row.Level);
        Assert.Equal(tone, row.Tone);
    }

    [Theory]
    [InlineData(LogLevel.Debug, true)]
    [InlineData(LogLevel.Info, false)]
    [InlineData(LogLevel.Error, false)]
    public void IsDebug_OnlyForDebug(LogLevel level, bool expected)
    {
        Assert.Equal(expected, Row(level).IsDebug);
    }

    [Fact]
    public void ToString_ReadsTheWholeRow()
    {
        using var culture = new CultureScope(CultureInfo.GetCultureInfo("en-US"));

        Assert.Equal("Warn, 1 Jan 1970 00:00:00 UTC, chart 3f9a1c02: Started on port 52448.", Row(LogLevel.Warning).ToString());
    }
}
