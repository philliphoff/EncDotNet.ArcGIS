namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>
/// The zoom floor (design brief D13): a chart layer's minimum scale is the
/// scale of its server's minimum zoom, below which the server draws
/// nothing; there, the <b>Coverage</b> footprints show where the data is.
/// </summary>
/// <remarks>
/// Scales are denominators, as in Pro: 1:100,000 is 100000, and a map
/// "below" a layer's minimum scale has a larger denominator.
/// </remarks>
public static class ZoomFloor
{
    /// <summary>How far inside the minimum scale Zoom to Data stops, so rounding can't leave the chart out of scale.</summary>
    public const double Inside = 0.95;

    /// <summary>The margin Zoom to Data leaves around the footprints, as a factor of their extent.</summary>
    public const double Margin = 1.1;

    /// <summary>
    /// Gets whether a map at <paramref name="scale"/> is below the minimum
    /// scale of one of the layers, so that layer draws nothing: "Zoom in to
    /// see the chart".
    /// </summary>
    /// <param name="scale">The map's scale denominator.</param>
    /// <param name="minScales">The layers' minimum scales; 0 means no limit.</param>
    public static bool IsBelow(double scale, IEnumerable<double> minScales)
    {
        ArgumentNullException.ThrowIfNull(minScales);
        return double.IsFinite(scale) && scale > 0 && minScales.Any(m => m > 0 && scale > m);
    }

    /// <summary>
    /// Gets the scale Zoom to Data moves to after showing the footprints at
    /// <paramref name="scale"/>, or <see langword="null"/> to stay.
    /// </summary>
    /// <remarks>
    /// When the footprints fit only below the chart's minimum scale, zooming
    /// in to it keeps the view's centre. That's right when the centre has
    /// data, as for a small cell whose footprint sits just beyond its
    /// chart's lowest zoom. When it hasn't, as between two distant datasets
    /// (S-57 Nantucket and S-104 Charleston), zooming in would show empty
    /// ocean: Zoom to Data stays, and the footprints show where the data is.
    /// </remarks>
    /// <param name="scale">The scale after zooming to the footprints.</param>
    /// <param name="minScale">The most zoomed-out minimum scale of the layers drawing the datasets; 0 means no limit.</param>
    /// <param name="centreHasData">Whether a footprint contains the view's centre.</param>
    public static double? ZoomInScale(double scale, double minScale, bool centreHasData) =>
        centreHasData && minScale > 0 && scale > minScale ? minScale * Inside : null;
}
