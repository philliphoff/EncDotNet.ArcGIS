using System.Globalization;

namespace EncDotNet.SoundCharts.Settings;

/// <summary>
/// Writes times as the <b>Time Format</b> setting says (design brief §7.3,
/// D28): UTC as <c>12 Nov 2026 14:00 UTC</c>, local time in the culture's
/// own form.
/// </summary>
public static class TimeText
{
    /// <summary>Formats <paramref name="time"/> with its date.</summary>
    /// <param name="time">The time.</param>
    /// <param name="format">UTC or local time.</param>
    /// <param name="seconds">Whether to show seconds, as the log does.</param>
    /// <param name="culture">The culture for local time; the current culture when <see langword="null"/>.</param>
    public static string Format(DateTimeOffset time, TimeFormat format, bool seconds = false, CultureInfo? culture = null)
    {
        if (format == TimeFormat.Local)
        {
            culture ??= CultureInfo.CurrentCulture;
            return time.ToLocalTime().DateTime.ToString(seconds ? "G" : "g", culture);
        }

        // Invariant month names: "UTC" text is the same in every culture.
        return time.UtcDateTime.ToString(seconds ? "d MMM yyyy HH:mm:ss" : "d MMM yyyy HH:mm", CultureInfo.InvariantCulture) + " UTC";
    }
}
