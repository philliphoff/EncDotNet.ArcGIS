namespace EncDotNet.SoundCharts.MapTime;

/// <summary>
/// What the add-in writes into a time-varying layer's CIM for a status
/// (design brief §6.1, §6.3, D29, D31).
/// </summary>
/// <remarks>
/// <para>
/// <b>Time</b> goes in the WMTS connection's <c>Dimensions</c>
/// (<c>Time</c> → the step). Pro 3.7 fills the REST template's
/// <c>{Time}</c> from it, so each step has its own tile URL and Pro caches
/// each; without it Pro uses the capabilities' default, the first step.
/// A CIM WMTS layer can't itself be time-enabled (no time definition on
/// <c>CIMTiledServiceLayer</c>), which is why the add-in drives it.
/// </para>
/// <para>
/// <b>No data</b> is drawn as nothing through the layer's visibility time
/// extent, so the Contents check box stays the user's (P9): with data the
/// extent is the data's range, which Pro also applies by itself before the
/// add-in hears of a time change; with none it's an extent no map time
/// reaches. The server would otherwise draw the last step (U6).
/// </para>
/// </remarks>
/// <param name="TimeSegment">
/// The <c>Time</c> dimension value to write, such as
/// <c>2025-12-17T13:00:00Z</c>; <see langword="null"/> to leave it as it is
/// (no data: the tiles already cached for the last step stay valid).
/// </param>
/// <param name="VisibleFrom">The start (UTC) of the layer's visibility time extent.</param>
/// <param name="VisibleTo">The end (UTC) of the layer's visibility time extent.</param>
public sealed record LayerTime(string? TimeSegment, DateTime VisibleFrom, DateTime VisibleTo)
{
    /// <summary>The <c>Dimensions</c> key of the chart server's WMTS time dimension.</summary>
    public const string DimensionName = "Time";

    /// <summary>
    /// The visibility extent used when a layer has no data at map time: an
    /// instant long before any S-100 data, which no map time overlaps.
    /// </summary>
    public static readonly DateTime NeverVisible = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Gets what to write for <paramref name="status"/> of a layer with <paramref name="steps"/>.</summary>
    /// <param name="steps">The layer's steps.</param>
    /// <param name="status">The layer's status at map time.</param>
    public static LayerTime For(TimeSteps steps, TimeStatus status)
    {
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(status);
        if (status.Step is { } step)
        {
            var (start, end) = steps.Range;
            return new LayerTime(TimeSteps.FormatSegment(step), start, end);
        }

        return new LayerTime(null, NeverVisible, NeverVisible);
    }
}
