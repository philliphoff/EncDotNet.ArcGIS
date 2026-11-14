using System.Globalization;
using System.Text;

namespace EncDotNet.SoundCharts.Logging;

/// <summary>One line of the SoundCharts log.</summary>
/// <param name="Timestamp">When it happened, in UTC.</param>
/// <param name="Level">Its severity.</param>
/// <param name="Source">
/// What it came from: <see cref="SoundChartsLog.AddInSource"/> for the
/// add-in's own events, or <c>chart {id}</c> for a chart server's output.
/// </param>
/// <param name="Message">The text, one line or several.</param>
public sealed record LogEntry(DateTimeOffset Timestamp, LogLevel Level, string Source, string Message)
{
    /// <summary>Gets the short level label used in text, such as <c>INFO</c> or <c>WARN</c>.</summary>
    /// <param name="level">The level.</param>
    public static string LevelLabel(LogLevel level) => level switch
    {
        LogLevel.Debug => "DEBUG",
        LogLevel.Info => "INFO",
        LogLevel.Warning => "WARN",
        LogLevel.Error => "ERROR",
        _ => level.ToString().ToUpperInvariant(),
    };

    /// <summary>
    /// Formats the entry as one line of the log file and of <b>Copy all</b>:
    /// UTC ISO 8601 time, level, source and message, such as
    /// <c>2026-11-13T22:47:20.123Z INFO  [chart 3f9a1c02] Started on port 52448.</c>
    /// </summary>
    /// <param name="redact">Applied to the message, such as <see cref="HomePathRedactor.Redact(string)"/>.</param>
    public string ToText(Func<string, string>? redact = null)
    {
        var builder = new StringBuilder();
        AppendText(builder, redact);
        return builder.ToString();
    }

    /// <summary>Formats <paramref name="entries"/> one per line (see <see cref="ToText(Func{string, string}?)"/>).</summary>
    /// <param name="entries">The entries.</param>
    /// <param name="redact">Applied to each message.</param>
    public static string ToText(IEnumerable<LogEntry> entries, Func<string, string>? redact = null)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var builder = new StringBuilder();
        foreach (var entry in entries)
        {
            entry.AppendText(builder, redact);
            builder.Append(Environment.NewLine);
        }

        return builder.ToString();
    }

    private void AppendText(StringBuilder builder, Func<string, string>? redact)
    {
        builder.Append(Timestamp.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture))
            .Append(' ').Append(LevelLabel(Level).PadRight(5))
            .Append(" [").Append(Source).Append("] ")
            .Append(redact is null ? Message : redact(Message));
    }
}
