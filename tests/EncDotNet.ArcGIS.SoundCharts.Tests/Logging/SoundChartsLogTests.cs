using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts.Tests.Logging;

public class SoundChartsLogTests
{
    private static readonly DateTimeOffset Noon = new(2026, 11, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Write_AddsAnEntry_AndRaisesEntryAdded()
    {
        var log = new SoundChartsLog(null, new FixedTime(Noon));
        LogEntry? added = null;
        log.EntryAdded += (_, entry) => added = entry;

        log.Write(LogLevel.Warning, "chart 3f9a1c02", "Restarting.");

        var entry = Assert.Single(log.Snapshot());
        Assert.Equal(new LogEntry(Noon, LogLevel.Warning, "chart 3f9a1c02", "Restarting."), entry);
        Assert.Same(entry, added);
    }

    [Fact]
    public void Write_KeepsTheLatestEntriesUpToCapacity()
    {
        var log = new SoundChartsLog(null, capacity: 3);

        for (var i = 0; i < 5; i++)
        {
            log.Info($"line {i}");
        }

        Assert.Equal(["line 2", "line 3", "line 4"], log.Snapshot().Select(entry => entry.Message));
    }

    [Fact]
    public void Write_AppendsToTodaysFile()
    {
        using var temp = new TempFolder();
        var log = new SoundChartsLog(temp.Path, new FixedTime(Noon));

        log.Info("SoundCharts loaded.");
        log.Error("The chart server stopped.");

        Assert.Equal(Path.Combine(temp.Path, "soundcharts-20261113.log"), log.CurrentFilePath);
        Assert.Equal(
            [
                "2026-11-13T12:00:00.000Z INFO  [add-in] SoundCharts loaded.",
                "2026-11-13T12:00:00.000Z ERROR [add-in] The chart server stopped.",
            ],
            File.ReadAllLines(log.CurrentFilePath ?? string.Empty));
    }

    [Fact]
    public void Write_SurvivesAFailingHandler()
    {
        var log = new SoundChartsLog(null);
        log.EntryAdded += (_, _) => throw new InvalidOperationException("pane bug");

        log.Info("still logged");

        Assert.Single(log.Snapshot());
    }

    [Fact]
    public void Write_SurvivesAnUnwritableFolder()
    {
        using var temp = new TempFolder();
        var blocker = temp.File("logs");

        var log = new SoundChartsLog(blocker);
        log.Info("kept in memory");

        Assert.Single(log.Snapshot());
    }

    [Fact]
    public void Creating_DeletesFilesOlderThanTwoWeeks()
    {
        using var temp = new TempFolder();
        var old = temp.File("soundcharts-20261029.log");
        var recent = temp.File("soundcharts-20261030.log");
        var other = temp.File("notes.log");

        _ = new SoundChartsLog(temp.Path, new FixedTime(Noon));

        Assert.False(File.Exists(old));
        Assert.True(File.Exists(recent));
        Assert.True(File.Exists(other));
    }

    [Fact]
    public void ToText_RedactsMessages()
    {
        var entries = new[]
        {
            new LogEntry(Noon, LogLevel.Info, "chart 1", @"Serving C:\Users\alice\Charts\a.000"),
            new LogEntry(Noon, LogLevel.Debug, "add-in", "debug"),
        };

        var text = LogEntry.ToText(entries, message => HomePathRedactor.Redact(message, @"C:\Users\alice"));

        Assert.Equal(
            "2026-11-13T12:00:00.000Z INFO  [chart 1] Serving ~\\Charts\\a.000" + Environment.NewLine +
            "2026-11-13T12:00:00.000Z DEBUG [add-in] debug" + Environment.NewLine,
            text);
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
