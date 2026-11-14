namespace EncDotNet.SoundCharts.Features;

/// <summary>
/// A feature's geometry in WGS 84, ready for Pro's geometry builders.
/// </summary>
/// <remarks>
/// <para>
/// For a <see cref="FeatureGeometryKind.Point"/> there is one part with one
/// vertex; for a <see cref="FeatureGeometryKind.Multipoint"/>, one part with
/// every point; for a <see cref="FeatureGeometryKind.Polyline"/>, one part
/// per curve.
/// </para>
/// <para>
/// For a <see cref="FeatureGeometryKind.Polygon"/>, the first part is the
/// exterior ring and the rest are interior rings, each closed (first vertex
/// repeated last) and oriented as Esri's geometry model expects: the
/// exterior clockwise, interior rings counter-clockwise.
/// </para>
/// </remarks>
public sealed class FeatureShape
{
    /// <summary>Initializes a new instance of the <see cref="FeatureShape"/> class.</summary>
    /// <param name="kind">The shape type.</param>
    /// <param name="parts">The parts, as described in the remarks.</param>
    public FeatureShape(FeatureGeometryKind kind, IReadOnlyList<IReadOnlyList<FeatureVertex>> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        if (kind == FeatureGeometryKind.None)
        {
            throw new ArgumentException("A shape needs a geometry kind.", nameof(kind));
        }

        Kind = kind;
        Parts = parts;
        Envelope = FeatureEnvelope.Of(parts.SelectMany(p => p))
            ?? throw new ArgumentException("A shape needs at least one vertex.", nameof(parts));
    }

    /// <summary>Gets the shape type.</summary>
    public FeatureGeometryKind Kind { get; }

    /// <summary>Gets the parts.</summary>
    public IReadOnlyList<IReadOnlyList<FeatureVertex>> Parts { get; }

    /// <summary>Gets the bounding rectangle of every vertex.</summary>
    public FeatureEnvelope Envelope { get; }

    /// <summary>
    /// Returns <paramref name="ring"/> closed and in the requested
    /// orientation (in X-east, Y-north axes).
    /// </summary>
    /// <param name="ring">The ring's vertices.</param>
    /// <param name="clockwise">Whether the result runs clockwise.</param>
    public static IReadOnlyList<FeatureVertex> OrientRing(IReadOnlyList<FeatureVertex> ring, bool clockwise)
    {
        ArgumentNullException.ThrowIfNull(ring);
        var result = ring.ToList();
        if (result.Count > 0 && result[0] != result[^1])
        {
            result.Add(result[0]);
        }

        // Shoelace: positive area means counter-clockwise.
        var twiceArea = 0.0;
        for (var i = 0; i + 1 < result.Count; i++)
        {
            twiceArea += (result[i].X * result[i + 1].Y) - (result[i + 1].X * result[i].Y);
        }

        var isClockwise = twiceArea < 0;
        if (twiceArea != 0 && isClockwise != clockwise)
        {
            result.Reverse();
        }

        return result;
    }
}
