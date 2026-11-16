using EncDotNet.SoundCharts.RouteCheck;

namespace EncDotNet.SoundCharts.Themes;

/// <summary>
/// Maps the view models' tone values onto <see cref="SoundChartsTone"/>.
/// Free of the WPF API, so it is testable; <see cref="ToneConverter"/> uses it
/// in bindings.
/// </summary>
public static class Tones
{
    /// <summary>Maps a tone value onto a <see cref="SoundChartsTone"/>.</summary>
    /// <param name="value">
    /// A <see cref="SoundChartsTone"/>, a <see cref="RouteCheckTone"/>
    /// (<see cref="RouteCheckTone.Destructive"/> is <see cref="SoundChartsTone.Danger"/>),
    /// or a tone's name (case-insensitive; "Destructive" and "Error" mean
    /// Danger).
    /// </param>
    /// <returns>The tone, or <see cref="SoundChartsTone.Neutral"/> for null or an unknown value.</returns>
    public static SoundChartsTone From(object? value) => value switch
    {
        SoundChartsTone tone => tone,
        RouteCheckTone routeCheck => routeCheck switch
        {
            RouteCheckTone.Success => SoundChartsTone.Success,
            RouteCheckTone.Warning => SoundChartsTone.Warning,
            RouteCheckTone.Destructive => SoundChartsTone.Danger,
            _ => SoundChartsTone.Neutral,
        },
        string name => FromName(name),
        _ => SoundChartsTone.Neutral,
    };

    private static SoundChartsTone FromName(string name)
    {
        if (string.Equals(name, "Destructive", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Error", StringComparison.OrdinalIgnoreCase))
        {
            return SoundChartsTone.Danger;
        }

        return Enum.TryParse<SoundChartsTone>(name, ignoreCase: true, out var tone) && Enum.IsDefined(tone)
            ? tone
            : SoundChartsTone.Neutral;
    }
}
