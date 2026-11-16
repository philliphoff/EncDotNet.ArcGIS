using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts.Tests.Logging;

/// <summary>
/// The Log pane's level filter choices and the count chips' clicks
/// (<see cref="LogPaneViewModel.ShowLevelCommand"/>), without Pro.
/// </summary>
public sealed class LogFilterOptionTests : IDisposable
{
    private readonly CultureScope _culture = new();

    public void Dispose() => _culture.Dispose();

    [Fact]
    public void All_HasTheViewersChoices_InfoAndAboveFirst()
    {
        var options = LogFilterOption.All();

        Assert.Equal(
            [LogFilter.InfoAndAbove, LogFilter.WarningsAndErrors, LogFilter.Errors, LogFilter.Everything],
            options.Select(o => o.Filter));
        Assert.Equal("Info and above", options[0].Label);
    }

    [Theory]
    [InlineData(LogLevel.Error, LogFilter.Errors, "Errors")]
    [InlineData(LogLevel.Warning, LogFilter.WarningsAndErrors, "Warnings and errors")]
    public void ForCount_PicksTheChipsLevel(LogLevel level, LogFilter expected, string label)
    {
        var options = LogFilterOption.All();

        var option = LogFilterOption.ForCount(options, level);

        Assert.NotNull(option);
        Assert.Equal(expected, option.Filter);
        Assert.Equal(label, option.Label);
        Assert.Contains(option, options);
    }

    [Theory]
    [InlineData(LogLevel.Info)]
    [InlineData(LogLevel.Debug)]
    public void ForCount_HasNoChoiceForOtherLevels(LogLevel level)
    {
        Assert.Null(LogFilterOption.ForCount(LogFilterOption.All(), level));
    }
}
