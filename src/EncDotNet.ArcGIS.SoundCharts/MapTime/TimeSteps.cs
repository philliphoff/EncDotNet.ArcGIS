using System.Globalization;
using System.Text.Json;
using EncDotNet.S100.Datasets.Pipelines;

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
/// How map time becomes a time segment follows the layer's
/// <see cref="Selection"/>, the chart server's <c>timeSelection</c> (issue
/// #64):
/// </para>
/// <list type="bullet">
/// <item><see cref="TimeSelection.Nearest"/>: a step is in range within half
/// an interval of the first or last step; inside the range, the nearest step
/// is drawn unless it's more than one interval away (a gap).</item>
/// <item><see cref="TimeSelection.AtOrBefore"/>: the latest step at or before
/// map time, never a later one, up to one interval after it.</item>
/// <item><see cref="TimeSelection.Continuous"/>: map time itself, rounded
/// down to <see cref="ContinuousRounding"/>, within the server's
/// <c>timeRange</c>.</item>
/// </list>
/// <para>
/// Anything else is "no data", never a silent snap to the last step (D31).
/// </para>
/// <para>
/// Which step is drawn, and the range of an at-or-before layer, come from
/// EncDotNet.S100's <see cref="TimeStepSelector"/>, the rule the chart
/// server picks steps with (issue #72), with <see cref="Interval"/> as the
/// tolerance. What's the add-in's own: <see cref="Interval"/> is the most
/// common interval, where the selector's typical step is the median (the
/// same for regular steps); a nearest layer's range is half an interval
/// either side, not a whole one; a continuous layer is rounded to
/// <see cref="ContinuousRounding"/>, not to the minute, and its range is the
/// server's <c>timeRange</c>; and a layer whose datasets select differently
/// has one combined <see cref="Selection"/>.
/// </para>
/// </remarks>
public sealed class TimeSteps
{
    /// <summary>The interval assumed for a layer with a single step, which has no interval of its own.</summary>
    public static readonly TimeSpan SingleStepInterval = TimeSpan.FromHours(1);

    /// <summary>
    /// What map time is rounded down to for a <see cref="TimeSelection.Continuous"/>
    /// layer: 15 minutes.
    /// </summary>
    /// <remarks>
    /// Each segment has its own tile URLs, so a coarser unit means more
    /// cache hits in Pro and fewer tiles drawn by the server, which draws to
    /// the minute. A hurricane moves 10 to 40 km an hour, so in 15 minutes
    /// its marker moves at most about 10 km: under 2 pixels at the
    /// 1:2,000,000 a storm and its wind radii are seen at. Forecast points
    /// and best-track points are on the hour, so a segment never falls
    /// between a point and the time it's drawn at.
    /// </remarks>
    public static readonly TimeSpan ContinuousRounding = TimeSpan.FromMinutes(15);

    private readonly DateTime[] _steps;
    private readonly (DateTime Start, DateTime End)? _span;

    private TimeSteps(DateTime[] steps, TimeSelection selection, (DateTime Start, DateTime End)? span)
    {
        _steps = steps;
        _span = span;
        Selection = selection;
        Interval = CommonInterval(steps);
    }

    /// <summary>Gets how the layer picks what it draws at map time.</summary>
    public TimeSelection Selection { get; }

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

    /// <summary>
    /// Gets the range the layer has data in: for <see cref="TimeSelection.Nearest"/>,
    /// the first and last steps widened by half an interval; for
    /// <see cref="TimeSelection.AtOrBefore"/>, the first step to one interval
    /// after the last; for <see cref="TimeSelection.Continuous"/>, the
    /// server's <c>timeRange</c>, which starts with a storm's past track
    /// (the first and last steps when the server didn't say).
    /// </summary>
    public (DateTime Start, DateTime End) Range => Selection switch
    {
        TimeSelection.Continuous when _span is { } span => (Min(span.Start, First), Max(span.End, Last)),
        TimeSelection.Continuous => (First, Last),
        TimeSelection.AtOrBefore => AtOrBeforeRange(),

        // Half an interval, where the selector's range has a whole one either side (D31).
        _ => (First - (Interval / 2), Last + (Interval / 2)),
    };

    /// <summary>
    /// Gets the times the data covers, for the time slider: the first and
    /// last steps, or the <see cref="Range"/> of a
    /// <see cref="TimeSelection.Continuous"/> layer, which draws between them
    /// and on a storm's past track.
    /// </summary>
    public (DateTime Start, DateTime End) Extent => Selection == TimeSelection.Continuous ? Range : (First, Last);

    /// <summary>Creates a step list from <paramref name="steps"/>; <see langword="null"/> when there are none.</summary>
    /// <param name="steps">The steps, in any order; local times are converted to UTC, unspecified ones taken as UTC.</param>
    /// <param name="selection">How the layer picks what it draws.</param>
    /// <param name="span">
    /// For a <see cref="TimeSelection.Continuous"/> layer, the span it draws
    /// in (the server's <c>timeRange</c>), or <see langword="null"/>.
    /// </param>
    public static TimeSteps? Create(
        IEnumerable<DateTime> steps, TimeSelection selection = TimeSelection.Nearest, (DateTime Start, DateTime End)? span = null)
    {
        ArgumentNullException.ThrowIfNull(steps);
        var sorted = steps.Select(Utc).Distinct().Order().ToArray();
        return sorted.Length == 0 ? null : new TimeSteps(sorted, selection, span is { } s ? (Utc(s.Start), Utc(s.End)) : null);
    }

    /// <summary>
    /// Gets these steps with the selection and span the chart server's
    /// <c>/datasets</c> gives (<see cref="DatasetTimes"/>), or these steps
    /// unchanged when it gives none.
    /// </summary>
    /// <param name="datasets">The time-varying datasets the server describes, or <see langword="null"/>.</param>
    public TimeSteps With(DatasetTimes? datasets) =>
        datasets is null || datasets.Selections.Count == 0
            ? this
            : new TimeSteps(_steps, TimeSelections.Combine(datasets.Selections), datasets.Span);

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

            // Each time-varying dataset's selection (issue #64); a server
            // before it gives none, and snaps to the nearest step.
            var selections = new List<TimeSelection>();
            if (s100.TryGetProperty("datasets", out var datasets) && datasets.ValueKind == JsonValueKind.Array)
            {
                foreach (var dataset in datasets.EnumerateArray())
                {
                    if (DatasetTimes.ReadSelection(dataset) is { } selection)
                    {
                        selections.Add(selection);
                    }
                }
            }

            return Create(steps, TimeSelections.Combine(selections));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the union of several layers' steps, with their combined
    /// selection and, when that's continuous, the union of their ranges;
    /// <see langword="null"/> when none has any.
    /// </summary>
    /// <param name="layers">The layers' steps.</param>
    public static TimeSteps? Union(IEnumerable<TimeSteps?> layers)
    {
        ArgumentNullException.ThrowIfNull(layers);
        var all = layers.OfType<TimeSteps>().ToList();
        var selection = TimeSelections.Combine(all.Select(l => l.Selection));
        (DateTime, DateTime)? span = selection == TimeSelection.Continuous && all.Count > 0
            ? (all.Min(l => l.Range.Start), all.Max(l => l.Range.End))
            : null;
        return Create(all.SelectMany(l => l._steps), selection, span);
    }

    /// <summary>
    /// Says what the layer draws at <paramref name="instant"/> by its
    /// <see cref="Selection"/>: the step (or, for a continuous layer, the
    /// rounded time), or that there's no data there and where the nearest
    /// data is (D30, D31).
    /// </summary>
    /// <param name="instant">The map instant; local times are converted to UTC, unspecified ones taken as UTC.</param>
    public TimeStatus Resolve(DateTime instant)
    {
        var at = Utc(instant);
        return Selection switch
        {
            TimeSelection.Continuous => ResolveContinuous(at),
            TimeSelection.AtOrBefore => ResolveAtOrBefore(at),
            _ => ResolveNearest(at),
        };
    }

    /// <summary>
    /// Rounds <paramref name="instant"/> down to <see cref="ContinuousRounding"/>,
    /// the time a continuous layer is drawn at.
    /// </summary>
    /// <param name="instant">The instant; local times are converted to UTC, unspecified ones taken as UTC.</param>
    public static DateTime RoundDown(DateTime instant)
    {
        var at = Utc(instant);
        return new DateTime(at.Ticks - (at.Ticks % ContinuousRounding.Ticks), DateTimeKind.Utc);
    }

    private TimeStatus ResolveContinuous(DateTime at)
    {
        var (start, end) = Range;
        if (at < start)
        {
            // Go to Nearest Data goes where the track starts, not to the first forecast point.
            return new TimeStatus(at, null, Next: start, Selection: TimeSelection.Continuous);
        }

        // The track's end is the last forecast point, a step; a time past it
        // that rounds down onto it is still beyond the forecast.
        if (at > end)
        {
            return new TimeStatus(at, null, Previous: end, Selection: TimeSelection.Continuous);
        }

        // The selector draws to the minute; Pro's tile cache wants coarser.
        // The steps at or before and after the drawn time say whether it's
        // on a step, between two or before the first.
        var drawn = RoundDown(Select(at) ?? at);
        var index = Array.BinarySearch(_steps, drawn);
        return new TimeStatus(at, drawn, index >= 0 ? drawn : Before(drawn), After(drawn), TimeSelection.Continuous);
    }

    // No step before the first, past the last or in a gap: the step before is more than one interval old.
    private TimeStatus ResolveAtOrBefore(DateTime at) =>
        Select(at) is { } step
            ? new TimeStatus(at, step, Selection: TimeSelection.AtOrBefore)
            : new TimeStatus(at, null, Before(at), After(at), TimeSelection.AtOrBefore);

    private TimeStatus ResolveNearest(DateTime at)
    {
        var (start, end) = Range;
        if (at < start)
        {
            return new TimeStatus(at, null, Next: First);
        }

        if (at > end)
        {
            return new TimeStatus(at, null, Previous: Last);
        }

        // In range but within a gap: the nearest step is further than one interval away.
        return Select(at) is { } step
            ? new TimeStatus(at, step)
            : new TimeStatus(at, null, Before(at), After(at));
    }

    /// <summary>
    /// Gets the step drawn at <paramref name="at"/> by upstream's rule, with
    /// <see cref="Interval"/> as the tolerance; for a continuous layer, the
    /// time to the minute.
    /// </summary>
    private DateTime? Select(DateTime at) =>
        TimeStepSelector.Select(_steps, TimeSelections.ToStepSelection(Selection), Interval, at);

    /// <summary>
    /// Gets upstream's range for at-or-before steps: the first step to
    /// <see cref="Interval"/> after the last.
    /// </summary>
    private (DateTime Start, DateTime End) AtOrBeforeRange() =>
        TimeStepSelector.RangeOf(new TimeSemantics(TimeStepKind.Forecast, TimeStepSelection.AtOrBefore, Interval), _steps)
            ?? throw new InvalidOperationException("A step list is never empty.");

    /// <summary>
    /// Gets the last step before <paramref name="instant"/>, for
    /// <b>‹ Previous data</b> (design brief §6.6); <see langword="null"/> when
    /// there's none.
    /// </summary>
    /// <param name="instant">The map instant; local times are converted to UTC, unspecified ones taken as UTC.</param>
    public DateTime? Before(DateTime instant) => TimeStepSelector.Previous(_steps, Utc(instant));

    /// <summary>
    /// Gets the first step after <paramref name="instant"/>, for
    /// <b>Next data ›</b> (design brief §6.6); <see langword="null"/> when
    /// there's none.
    /// </summary>
    /// <param name="instant">The map instant; local times are converted to UTC, unspecified ones taken as UTC.</param>
    public DateTime? After(DateTime instant) => TimeStepSelector.Next(_steps, Utc(instant));

    /// <summary>
    /// Gets the map time to open at when the first time-varying layer arrives
    /// in a map without one (D32): now, snapped (rounded down for a
    /// continuous layer), when the data covers it; otherwise the nearer end
    /// of the data.
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

    private static DateTime Min(DateTime a, DateTime b) => a <= b ? a : b;

    private static DateTime Max(DateTime a, DateTime b) => a >= b ? a : b;

    /// <summary>Parses a time as the chart server writes it, <c>2025-12-17T13:00:00Z</c>.</summary>
    /// <param name="text">The text.</param>
    /// <param name="step">The time (UTC).</param>
    internal static bool TryParse(string? text, out DateTime step)
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
