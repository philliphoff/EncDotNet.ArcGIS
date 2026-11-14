using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts.Tests.Logging;

public class LogFilterTests
{
    [Theory]
    [InlineData(LogFilter.InfoAndAbove, LogLevel.Debug, false)]
    [InlineData(LogFilter.InfoAndAbove, LogLevel.Info, true)]
    [InlineData(LogFilter.InfoAndAbove, LogLevel.Error, true)]
    [InlineData(LogFilter.WarningsAndErrors, LogLevel.Info, false)]
    [InlineData(LogFilter.WarningsAndErrors, LogLevel.Warning, true)]
    [InlineData(LogFilter.Errors, LogLevel.Warning, false)]
    [InlineData(LogFilter.Errors, LogLevel.Error, true)]
    [InlineData(LogFilter.Everything, LogLevel.Debug, true)]
    public void Includes_FollowsTheViewersLevels(LogFilter filter, LogLevel level, bool expected)
    {
        Assert.Equal(expected, filter.Includes(level));
    }

    [Fact]
    public void Includes_CanKeepOneSource()
    {
        var entry = new LogEntry(DateTimeOffset.UnixEpoch, LogLevel.Info, "chart 3f9a1c02", "Running.");

        Assert.True(LogFilter.InfoAndAbove.Includes(entry));
        Assert.True(LogFilter.InfoAndAbove.Includes(entry, "chart 3f9a1c02"));
        Assert.False(LogFilter.InfoAndAbove.Includes(entry, "chart 00000000"));
    }

    [Fact]
    public void TheDefault_IsInfoAndAbove()
    {
        Assert.Equal(LogFilter.InfoAndAbove, default);
    }
}
