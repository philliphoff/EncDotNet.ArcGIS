using System.Globalization;
using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts;

/// <summary>One line of the SoundCharts Log pane.</summary>
/// <param name="Entry">The log entry.</param>
internal sealed record LogRow(LogEntry Entry)
{
    /// <summary>Gets the time, in UTC, with the date.</summary>
    public string Time => Entry.Timestamp.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    /// <summary>Gets the level label, such as <c>WARN</c>.</summary>
    public string Level => LogEntry.LevelLabel(Entry.Level);

    /// <summary>Gets the source, such as <c>add-in</c> or <c>chart 3f9a1c02</c>.</summary>
    public string Source => Entry.Source;

    /// <summary>Gets the message.</summary>
    public string Message => Entry.Message;
}
