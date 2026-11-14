namespace EncDotNet.SoundCharts.Features;

/// <summary>
/// A WGS 84 bounding rectangle, in longitude (X) and latitude (Y) degrees.
/// </summary>
/// <param name="XMin">The west edge.</param>
/// <param name="YMin">The south edge.</param>
/// <param name="XMax">The east edge.</param>
/// <param name="YMax">The north edge.</param>
public readonly record struct FeatureEnvelope(double XMin, double YMin, double XMax, double YMax)
{
    /// <summary>Gets whether this rectangle and <paramref name="other"/> share any point, edges included.</summary>
    /// <param name="other">The other rectangle.</param>
    public bool Intersects(FeatureEnvelope other) =>
        XMin <= other.XMax && other.XMin <= XMax && YMin <= other.YMax && other.YMin <= YMax;

    /// <summary>Returns the smallest rectangle holding this one and <paramref name="other"/>.</summary>
    /// <param name="other">The other rectangle.</param>
    public FeatureEnvelope Union(FeatureEnvelope other) => new(
        Math.Min(XMin, other.XMin),
        Math.Min(YMin, other.YMin),
        Math.Max(XMax, other.XMax),
        Math.Max(YMax, other.YMax));

    /// <summary>Returns the bounding rectangle of <paramref name="vertices"/>, or <see langword="null"/> when there are none.</summary>
    /// <param name="vertices">The vertices.</param>
    public static FeatureEnvelope? Of(IEnumerable<FeatureVertex> vertices)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        FeatureEnvelope? result = null;
        foreach (var v in vertices)
        {
            var point = new FeatureEnvelope(v.X, v.Y, v.X, v.Y);
            result = result is { } r ? r.Union(point) : point;
        }

        return result;
    }
}
