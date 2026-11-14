using System.Globalization;
using System.Text.Json;

namespace EncDotNet.SoundCharts.MapTime;

/// <summary>
/// The time steps a time-varying SoundCharts layer can draw, and the rules
/// that turn Pro's map time into one of them (design brief §6.3, §6.4,
/// D30–D32).
/// </summary>
/// <remarks>
/// <para>
/// The steps are the chart server's own: TileJSON's <c>s100.times</c>, which
/// are also the WMTS <c>Time</c> dimension's <c>&lt;Value&gt;</c>s. The
/// time segment of a tile URL must be one of them, so reading them from the
/// server keeps the add-in's snapping and the server's in step.
/// </para>
/// <para>
/// A step is in range within half an interval of the first or last step;
/// inside the range, the nearest step is drawn unless it's more than one
/// interval away (a gap). Anything else is "no data", never a silent snap
/// to the last step (D31).
/// </para>
/// </remarks>
public sealed class TimeSteps
{
    /// <summary>The interval assumed for a layer with a single step, which has no interval of its own.</summary>
    public static readonly TimeSpan SingleStepInterval = TimeSpan.FromHours(1);

    private readonly DateTime[] _steps;

    private TimeSteps(DateTime[] steps)
    {
        _steps = steps;
        Interval = CommonInterval(steps);
    }

    /// <summary>Gets the steps, UTC, oldest first, without duplicates.</summary>
    public IReadOnlyList<DateTime> Steps => _steps;

    /// <summary>Gets the first step (UTC).</summary>
    public DateTime First => _steps[0];

    /// <summary>Gets the last step (UTC).</summary>
    public DateTime Last => _steps[^1];

    /// <summary>
    /// Gets the most common interval between steps (the shorter on a tie),
    /// or <see cref="SingleStepInterval"/> for a single step. Pro's slider
    /// steps by it (§6.1).
    /// </summary>
    public TimeSpan Interval { get; }

    /// <summary>Gets the range the layer has data in: the first and last steps, widened by half an interval.</summary>
    public (DateTime Start, DateTime End) Range => (First - (Interval / 2), Last + (Interval / 2));

    /// <summary>Creates a step list from <paramref name="steps"/>; <see langword="null"/> when there are none.</summary>
    /// <param name="steps">The steps, in any order; local times are converted to UTC, unspecified ones taken as UTC.</param>
    public static TimeSteps? Create(IEnumerable<DateTime> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        var sorted = steps.Select(Utc).Distinct().Order().ToArray();
        return sorted.Length == 0 ? null : new TimeSteps(sorted);
    }

    /// <summary>
    /// Reads the steps from a chart server's TileJSON (<c>s100.times</c>);
    /// <see langword="null"/> when it lists none or isn't valid JSON.
    /// </summary>
    /// <param name="json">The TileJSON document.</param>
    public static TimeSteps? TryReadTileJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("s100", out var s100)
                || s100.ValueKind != JsonValueKind.Object
                || !s100.TryGetProperty("times", out var times)
                || times.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var steps = new List<DateTime>();
            foreach (var item in times.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String && TryParse(item.GetString(), out var step))
                {
                    steps.Add(step);
                }
            }

            return Create(steps);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Gets the union of several layers' steps; <see langword="null"/> when none has any.</summary>
    /// <param name="layers">The layers' steps.</param>
    public static TimeSteps? Union(IEnumerable<TimeSteps?> layers)
    {
        ArgumentNullException.ThrowIfNull(layers);
        return Create(layers.OfType<TimeSteps>().SelectMany(l => l._steps));
    }

    /// <summary>
    /// Snaps <paramref name="instant"/> to the nearest step, or says there's
    /// no data there and where the nearest data is (D30, D31).
    /// </summary>
    /// <param name="instant">The map instant; local times are converted to UTC, unspecified ones taken as UTC.</param>
    public TimeStatus Resolve(DateTime instant)
    {
        var at = Utc(instant);
        var (start, end) = Range;
        if (at < start)
        {
            return new TimeStatus(at, null, Next: First);
        }

        if (at > end)
        {
            return new TimeStatus(at, null, Previous: Last);
        }

        var index = Array.BinarySearch(_steps, at);
        if (index >= 0)
        {
            return new TimeStatus(at, _steps[index]);
        }

        // ~index is the first step after the instant.
        var after = ~index;
        DateTime? previous = after > 0 ? _steps[after - 1] : null;
        DateTime? next = after < _steps.Length ? _steps[after] : null;
        var nearest = (previous, next) switch
        {
            ({ } p, { } n) => at - p <= n - at ? p : n,
            ({ } p, null) => p,
            (null, { } n) => n,
            _ => throw new InvalidOperationException("A step list is never empty."),
        };

        // In range but within a gap: the nearest step is further than one interval away.
        return (nearest - at).Duration() > Interval
            ? new TimeStatus(at, null, previous, next)
            : new TimeStatus(at, nearest);
    }

    /// <summary>
    /// Gets the map time to open at when the first time-varying layer arrives
    /// in a map without one (D32): now, snapped, when the data covers it;
    /// otherwise the nearer end of the data.
    /// </summary>
    /// <param name="now">The current time.</param>
    public DateTime DefaultTime(DateTime now)
    {
        var status = Resolve(now);
        return status.Step ?? status.Nearest ?? First;
    }

    /// <summary>
    /// Formats a step for the WMTS <c>Time</c> dimension and REST path
    /// segment, as the server advertises it: <c>2025-12-17T13:00:00Z</c>.
    /// </summary>
    /// <param name="step">The step.</param>
    public static string FormatSegment(DateTime step) =>
        Utc(step).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    /// <summary>Converts <paramref name="time"/> to UTC; an unspecified kind is taken as UTC already.</summary>
    /// <param name="time">The time.</param>
    public static DateTime Utc(DateTime time) => time.Kind switch
    {
        DateTimeKind.Local => time.ToUniversalTime(),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(time, DateTimeKind.Utc),
        _ => time,
    };

    private static bool TryParse(string? text, out DateTime step)
    {
        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var value))
        {
            step = value.UtcDateTime;
            return true;
        }

        step = default;
        return false;
    }

    private static TimeSpan CommonInterval(DateTime[] steps)
    {
        if (steps.Length < 2)
        {
            return SingleStepInterval;
        }

        return steps.Zip(steps.Skip(1), (a, b) => b - a)
            .GroupBy(d => d)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .First().Key;
    }
}
