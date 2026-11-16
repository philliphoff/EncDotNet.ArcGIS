using System.Globalization;
using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts.Tests.Logging;

public class LogCountsTests
{
    private static LogEntry Entry(LogLevel level, string source = SoundChartsLog.AddInSource) =>
        new(DateTimeOffset.UnixEpoch, level, source, "Message.");

    [Fact]
    public void Of_CountsErrorsAndWarnings_AtEveryLevel()
    {
        var counts = LogCounts.Of(
        [
            Entry(LogLevel.Debug),
            Entry(LogLevel.Info),
            Entry(LogLevel.Warning),
            Entry(LogLevel.Error),
            Entry(LogLevel.Warning),
            Entry(LogLevel.Error),
            Entry(LogLevel.Warning),
        ]);

        Assert.Equal(new LogCounts(Errors: 2, Warnings: 3), counts);
    }

    [Fact]
    public void Of_KeepsOneSource()
    {
        LogEntry[] entries =
        [
            Entry(LogLevel.Error, "chart 3f9a1c02"),
            Entry(LogLevel.Warning, "chart 3f9a1c02"),
            Entry(LogLevel.Error, "chart 00000000"),
            Entry(LogLevel.Error),
        ];

        Assert.Equal(new LogCounts(1, 1), LogCounts.Of(entries, "chart 3f9a1c02"));
        Assert.Equal(new LogCounts(3, 1), LogCounts.Of(entries));
    }

    [Fact]
    public void Of_Nothing_IsZero_AndHidesTheChips()
    {
        var counts = LogCounts.Of([Entry(LogLevel.Info)]);

        Assert.Equal(default, counts);
        Assert.False(counts.HasErrors);
        Assert.False(counts.HasWarnings);
    }

    [Fact]
    public void HasErrorsAndHasWarnings_FollowTheCounts()
    {
        var counts = new LogCounts(Errors: 1, Warnings: 0);

        Assert.True(counts.HasErrors);
        Assert.False(counts.HasWarnings);
    }

    [Theory]
    [InlineData(0, "0 errors")]
    [InlineData(1, "1 error")]
    [InlineData(2, "2 errors")]
    [InlineData(1200, "1,200 errors")]
    public void ErrorsText_SaysTheCount(int errors, string expected)
    {
        using var culture = new CultureScope(CultureInfo.GetCultureInfo("en-US"));

        Assert.Equal(expected, new LogCounts(errors, 0).ErrorsText);
    }

    [Theory]
    [InlineData(1, "1 warning")]
    [InlineData(5, "5 warnings")]
    public void WarningsText_SaysTheCount(int warnings, string expected)
    {
        using var culture = new CultureScope(CultureInfo.GetCultureInfo("en-US"));

        Assert.Equal(expected, new LogCounts(0, warnings).WarningsText);
    }
}
