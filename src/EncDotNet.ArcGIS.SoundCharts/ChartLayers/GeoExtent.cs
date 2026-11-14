namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>
/// A WGS-84 bounding box in decimal degrees: a dataset's footprint, as
/// <b>Zoom to Data</b> uses it.
/// </summary>
/// <param name="West">The western longitude.</param>
/// <param name="South">The southern latitude.</param>
/// <param name="East">The eastern longitude.</param>
/// <param name="North">The northern latitude.</param>
public readonly record struct GeoExtent(double West, double South, double East, double North)
{
    /// <summary>
    /// Gets whether the box is a usable footprint: finite, latitudes in
    /// [-90, 90], south not above north and west not east of east.
    /// </summary>
    public bool IsValid =>
        double.IsFinite(West) && double.IsFinite(South) && double.IsFinite(East) && double.IsFinite(North)
        && South is >= -90.0 and <= 90.0 && North is >= -90.0 and <= 90.0
        && South <= North && West <= East;

    /// <summary>
    /// Gets the smallest box holding every valid box in
    /// <paramref name="extents"/>, or <see langword="null"/> when there is none.
    /// </summary>
    /// <param name="extents">The boxes.</param>
    public static GeoExtent? Union(IEnumerable<GeoExtent?> extents)
    {
        ArgumentNullException.ThrowIfNull(extents);

        GeoExtent? union = null;
        foreach (var extent in extents)
        {
            if (extent is not { IsValid: true } e)
            {
                continue;
            }

            union = union is { } u
                ? new GeoExtent(Math.Min(u.West, e.West), Math.Min(u.South, e.South), Math.Max(u.East, e.East), Math.Max(u.North, e.North))
                : e;
        }

        return union;
    }
}
