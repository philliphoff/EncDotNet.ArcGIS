namespace EncDotNet.SoundCharts.MapTime;

/// <summary>
/// How a time layer picks what it draws at map time: the chart server's
/// <c>timeSelection</c> for each time-varying dataset (EncDotNet.S100's
/// <c>TimeStepSelection</c>, issue #64).
/// </summary>
public enum TimeSelection
{
    /// <summary>The nearest step, before or after map time: S-111, GRIB2 and NetCDF weather.</summary>
    Nearest,

    /// <summary>The latest step at or before map time, never a later one: S-104, S-411, S-412, CAP.</summary>
    AtOrBefore,

    /// <summary>
    /// Map time itself: a tropical cyclone, drawn at its best-track position
    /// before the advisory and estimated between forecast points after it.
    /// Its steps are only what to step between.
    /// </summary>
    Continuous,
}

/// <summary>Reads and combines <see cref="TimeSelection"/>s.</summary>
public static class TimeSelections
{
    /// <summary>
    /// Reads the chart server's name for a selection: <c>nearest</c>,
    /// <c>atOrBefore</c> or <c>continuous</c>; <see langword="null"/> for any
    /// other.
    /// </summary>
    /// <param name="name">The name, as <c>/datasets</c> and TileJSON give it.</param>
    public static TimeSelection? TryParse(string? name) => name switch
    {
        "nearest" => TimeSelection.Nearest,
        "atOrBefore" => TimeSelection.AtOrBefore,
        "continuous" => TimeSelection.Continuous,
        _ => null,
    };

    /// <summary>Gets the chart server's name for <paramref name="selection"/>, for the log.</summary>
    /// <param name="selection">The selection.</param>
    public static string Name(TimeSelection selection) => selection switch
    {
        TimeSelection.AtOrBefore => "atOrBefore",
        TimeSelection.Continuous => "continuous",
        _ => "nearest",
    };

    /// <summary>
    /// Gets the selection of a layer whose datasets select as
    /// <paramref name="selections"/> say: theirs when they agree;
    /// <see cref="TimeSelection.Continuous"/> when any is, so the storm moves
    /// and isn't hidden; otherwise <see cref="TimeSelection.AtOrBefore"/>, so
    /// an at-or-before dataset never draws a step later than map time.
    /// <see cref="TimeSelection.Nearest"/> when there are none, as before the
    /// server said.
    /// </summary>
    /// <param name="selections">The datasets' selections.</param>
    public static TimeSelection Combine(IEnumerable<TimeSelection> selections)
    {
        ArgumentNullException.ThrowIfNull(selections);
        var distinct = selections.Distinct().ToList();
        return distinct switch
        {
            [] => TimeSelection.Nearest,
            [var only] => only,
            _ when distinct.Contains(TimeSelection.Continuous) => TimeSelection.Continuous,
            _ => TimeSelection.AtOrBefore,
        };
    }
}
