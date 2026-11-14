using System.Globalization;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts.MapTime;

/// <summary>
/// The words the add-in uses for map time (design brief §6.3, §7.3, D28,
/// D31): times as <c>12 Nov 2026 14:00 UTC</c>, spans, and a layer's time
/// status.
/// </summary>
public static class TimeText
{
    /// <summary>Formats a time as UTC, the default <b>Time format</b> (D28): <c>17 Dec 2025 13:00 UTC</c>.</summary>
    /// <param name="time">The time; unspecified is taken as UTC.</param>
    public static string FormatTime(DateTime time) =>
        string.Format(CultureInfo.CurrentCulture, Strings.Status_UtcFormat, TimeSteps.Utc(time));

    /// <summary>Formats a span as minutes, hours or days, rounded: <c>45 min</c>, <c>3 h</c>, <c>2 days</c>.</summary>
    /// <param name="span">The span; its sign is ignored.</param>
    public static string FormatSpan(TimeSpan span)
    {
        var length = span.Duration();
        var (format, value) = length switch
        {
            { TotalMinutes: < 90 } => (Strings.Status_SpanMinutes, Math.Max(1, (int)Math.Round(length.TotalMinutes))),
            { TotalHours: < 48 } => (Strings.Status_SpanHours, (int)Math.Round(length.TotalHours)),
            _ => (Strings.Status_SpanDays, (int)Math.Round(length.TotalDays)),
        };
        return string.Format(CultureInfo.CurrentCulture, format, value);
    }

    /// <summary>
    /// Describes a layer's status: <c>Water level · 17 Dec 2025 13:00 UTC</c>
    /// with data, and with none the viewer's "No data at map time · last
    /// {time}, {span} earlier" (or "next {time}, {span} later").
    /// </summary>
    /// <param name="layerName">The layer's name, such as <b>Water level</b>.</param>
    /// <param name="status">The layer's status.</param>
    public static string Describe(string layerName, TimeStatus status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layerName);
        ArgumentNullException.ThrowIfNull(status);
        if (status.Step is { } step)
        {
            return Format(Strings.Status_LayerAtTime, layerName, FormatTime(step));
        }

        // Point to the nearer side, as Go to Nearest Data goes there.
        var nearest = status.Nearest;
        var noData = nearest switch
        {
            null => Strings.Status_NoData,
            { } n when n <= status.Instant => Format(Strings.Status_NoDataLast, FormatTime(n), FormatSpan(status.Instant - n)),
            { } n => Format(Strings.Status_NoDataNext, FormatTime(n), FormatSpan(n - status.Instant)),
        };
        return Format(Strings.Status_LayerNoData, layerName, noData);
    }

    /// <summary>
    /// Summarizes the time layers of a map, one line each, for the SoundCharts
    /// Layer tab; or says there are none, or that the map has no map time.
    /// </summary>
    /// <param name="statuses">Each layer's name and status, <see langword="null"/> without map time.</param>
    public static string Summarize(IReadOnlyList<(string Name, TimeStatus? Status)> statuses)
    {
        ArgumentNullException.ThrowIfNull(statuses);
        var lines = statuses
            .Select(s => s.Status is { } status ? Describe(s.Name, status) : null)
            .OfType<string>()
            .ToList();
        return statuses.Count == 0
            ? Strings.Status_NoTimeLayers
            : lines.Count == 0 ? Strings.Status_NoMapTime : string.Join(Environment.NewLine, lines);
    }

    private static string Format(string format, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, format, args);
}
