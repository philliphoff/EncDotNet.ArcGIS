using EncDotNet.S100.DataModel;

namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>
/// One polygon of a dataset's footprint, in WGS-84: an exterior ring and
/// any no-coverage holes, as a <c>DataCoverage</c> (S-101, S-1xx GML) or
/// <c>M_COVR</c> (S-57) surface declares it.
/// </summary>
/// <param name="ExteriorRing">The ring bounding the covered area, at least three positions.</param>
/// <param name="Holes">Rings with no coverage inside the exterior ring.</param>
public sealed record FootprintPolygon(IReadOnlyList<GeoPosition> ExteriorRing, IReadOnlyList<IReadOnlyList<GeoPosition>> Holes)
{
    /// <summary>Creates a polygon with no holes.</summary>
    /// <param name="exteriorRing">The ring bounding the covered area.</param>
    public FootprintPolygon(IReadOnlyList<GeoPosition> exteriorRing)
        : this(exteriorRing, [])
    {
    }

    /// <summary>Gets the polygon's bounding box.</summary>
    public GeoExtent Extent
    {
        get
        {
            var west = double.PositiveInfinity;
            var south = double.PositiveInfinity;
            var east = double.NegativeInfinity;
            var north = double.NegativeInfinity;
            foreach (var position in ExteriorRing)
            {
                west = Math.Min(west, position.Longitude);
                east = Math.Max(east, position.Longitude);
                south = Math.Min(south, position.Latitude);
                north = Math.Max(north, position.Latitude);
            }

            return new GeoExtent(west, south, east, north);
        }
    }

    /// <summary>
    /// Creates the rectangle of <paramref name="extent"/>: the footprint of a
    /// grid, whose HDF5 bounds are all it declares.
    /// </summary>
    /// <param name="extent">The box.</param>
    public static FootprintPolygon FromExtent(GeoExtent extent) => new(
    [
        new GeoPosition(extent.South, extent.West),
        new GeoPosition(extent.North, extent.West),
        new GeoPosition(extent.North, extent.East),
        new GeoPosition(extent.South, extent.East),
        new GeoPosition(extent.South, extent.West),
    ]);

    /// <summary>
    /// Creates a polygon from rings read from a dataset, or returns
    /// <see langword="null"/> when the exterior ring has fewer than three
    /// distinct positions. Degenerate holes are dropped.
    /// </summary>
    /// <param name="exteriorRing">The exterior ring.</param>
    /// <param name="holes">The holes.</param>
    public static FootprintPolygon? TryCreate(IReadOnlyList<GeoPosition>? exteriorRing, IEnumerable<IReadOnlyList<GeoPosition>>? holes)
    {
        if (!IsRing(exteriorRing))
        {
            return null;
        }

        return new FootprintPolygon(exteriorRing, [.. (holes ?? []).Where(IsRing)]);
    }

    /// <summary>
    /// Gets whether a WGS-84 point is inside the polygon and outside its
    /// holes (even-odd ray cast, for areas that don't cross the antimeridian).
    /// </summary>
    /// <param name="latitude">The latitude in degrees.</param>
    /// <param name="longitude">The longitude in degrees.</param>
    public bool Contains(double latitude, double longitude) =>
        InRing(ExteriorRing, latitude, longitude) && !Holes.Any(hole => InRing(hole, latitude, longitude));

    private static bool IsRing([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] IReadOnlyList<GeoPosition>? ring) =>
        ring is not null
        && ring.All(p => double.IsFinite(p.Latitude) && double.IsFinite(p.Longitude))
        && ring.Distinct().Take(3).Count() == 3;

    private static bool InRing(IReadOnlyList<GeoPosition> ring, double latitude, double longitude)
    {
        var inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            var a = ring[i];
            var b = ring[j];
            if ((a.Latitude > latitude) != (b.Latitude > latitude)
                && longitude < ((b.Longitude - a.Longitude) * (latitude - a.Latitude) / (b.Latitude - a.Latitude)) + a.Longitude)
            {
                inside = !inside;
            }
        }

        return inside;
    }
}
