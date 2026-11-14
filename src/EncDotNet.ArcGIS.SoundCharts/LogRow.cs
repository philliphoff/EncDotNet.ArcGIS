using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Settings;

namespace EncDotNet.SoundCharts;

/// <summary>One line of the SoundCharts Log pane.</summary>
/// <param name="Entry">The log entry.</param>
/// <param name="TimeFormat">How the time is shown (D28).</param>
internal sealed record LogRow(LogEntry Entry, TimeFormat TimeFormat = TimeFormat.Utc)
{
    /// <summary>Gets the time with the date and seconds, as <b>Time Format</b> says.</summary>
    public string Time => TimeText.Format(Entry.Timestamp, TimeFormat, seconds: true);

    /// <summary>Gets the level label, such as <c>WARN</c>.</summary>
    public string Level => LogEntry.LevelLabel(Entry.Level);

    /// <summary>Gets the source, such as <c>add-in</c> or <c>chart 3f9a1c02</c>.</summary>
    public string Source => Entry.Source;

    /// <summary>Gets the message.</summary>
    public string Message => Entry.Message;
}
