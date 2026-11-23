using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Resources;
using EncDotNet.SoundCharts.Settings;
using EncDotNet.SoundCharts.Themes;

namespace EncDotNet.SoundCharts;

/// <summary>One line of the SoundCharts Log pane.</summary>
/// <param name="Entry">The log entry.</param>
/// <param name="TimeFormat">How the time is shown (D28).</param>
internal sealed record LogRow(LogEntry Entry, TimeFormat TimeFormat = TimeFormat.Utc)
{
    /// <summary>Gets the time with the date and seconds, as <b>Time Format</b> says.</summary>
    public string Time => TimeText.Format(Entry.Timestamp, TimeFormat, seconds: true);

    /// <summary>Gets the short level label, such as <c>Warn</c> (the viewer's logs panel D6).</summary>
    public string Level => Entry.Level switch
    {
        LogLevel.Debug => Strings.Log_Level_Debug,
        LogLevel.Warning => Strings.Log_Level_Warning,
        LogLevel.Error => Strings.Log_Level_Error,
        _ => Strings.Log_Level_Info,
    };

    /// <summary>Gets the source, such as <c>add-in</c> or <c>chart 3f9a1c02</c>.</summary>
    public string Source => Entry.Source;

    /// <summary>Gets the message.</summary>
    public string Message => Entry.Message;

    /// <summary>
    /// Gets the row's tone: <see cref="SoundChartsTone.Danger"/> for an
    /// error and <see cref="SoundChartsTone.Warning"/> for a warning, which
    /// get a glyph and a row tint; <see cref="SoundChartsTone.Neutral"/>
    /// otherwise.
    /// </summary>
    public SoundChartsTone Tone => Entry.Level switch
    {
        LogLevel.Error => SoundChartsTone.Danger,
        LogLevel.Warning => SoundChartsTone.Warning,
        _ => SoundChartsTone.Neutral,
    };

    /// <summary>Gets whether the row is Debug, which is shown at reduced emphasis.</summary>
    public bool IsDebug => Entry.Level == LogLevel.Debug;

    /// <summary>Gets the row as a screen reader reads it: level, time, source and message.</summary>
    public override string ToString() => $"{Level}, {Time}, {Source}: {Message}";
}
