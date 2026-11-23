using EncDotNet.S100.Collections;

namespace EncDotNet.SoundCharts.Library;

/// <summary>
/// A Library dataset's footprint as the index records it, without opening
/// the dataset: its coverage polygons (an exchange set's GML coverage), or
/// else its bounding box, in WGS-84 longitude and latitude.
/// </summary>
/// <param name="Rings">The rings, each a closed list of (longitude, latitude) vertices: exteriors and holes, as published.</param>
/// <param name="West">The west edge of the footprint's bounds.</param>
/// <param name="South">The south edge.</param>
/// <param name="East">The east edge.</param>
/// <param name="North">The north edge.</param>
/// <param name="IsBoundingBox">Whether the footprint is only the bounding box, because the index has no coverage polygons.</param>
public sealed record LibraryFootprint(
    IReadOnlyList<IReadOnlyList<(double Longitude, double Latitude)>> Rings,
    double West,
    double South,
    double East,
    double North,
    bool IsBoundingBox)
{
    private static readonly double[] AntimeridianShifts = [-360, 0, 360];

    /// <summary>Gets the footprint of <paramref name="item"/>.</summary>
    /// <param name="item">The dataset.</param>
    /// <returns>The footprint, or <see langword="null"/> when the index has neither coverage nor bounds.</returns>
    public static LibraryFootprint? Of(CollectionItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Coverage is { Polygons.Count: > 0 } coverage && coverage.ComputeBounds() is { } covered)
        {
            var rings = new List<IReadOnlyList<(double, double)>>();
            foreach (var polygon in coverage.Polygons)
            {
                if (polygon.Exterior.Count >= 3)
                {
                    rings.Add(Close(polygon.Exterior.Select(p => (p.Longitude, p.Latitude))));
                    rings.AddRange(polygon.Holes.Where(h => h.Count >= 3).Select(h => Close(h.Select(p => (p.Longitude, p.Latitude)))));
                }
            }

            if (rings.Count > 0)
            {
                return new LibraryFootprint(rings, covered.West, covered.South, covered.East, covered.North, IsBoundingBox: false);
            }
        }

        return item.Bounds is { } b ? FromBounds(b) : null;
    }

    /// <summary>Gets a footprint that is only a bounding box.</summary>
    /// <param name="bounds">The box, in WGS-84.</param>
    public static LibraryFootprint FromBounds(GeoBounds bounds)
    {
        // A box across the antimeridian is drawn from west to east + 360.
        var b = bounds;
        var east = b.CrossesAntimeridian ? b.East + 360 : b.East;
        IReadOnlyList<(double, double)> box = [(b.West, b.South), (b.West, b.North), (east, b.North), (east, b.South), (b.West, b.South)];
        return new LibraryFootprint([box], b.West, b.South, east, b.North, IsBoundingBox: true);
    }

    /// <summary>
    /// Gets whether the footprint's bounds overlap a box, such as the map's
    /// extent, in WGS-84: <b>Add Datasets in View</b>. Bounds, not the
    /// coverage polygons, so a dataset near the view's corner may be included.
    /// Either box may cross the antimeridian (east past 180, or east less than west).
    /// </summary>
    /// <param name="west">The box's west edge.</param>
    /// <param name="south">The box's south edge.</param>
    /// <param name="east">The box's east edge.</param>
    /// <param name="north">The box's north edge.</param>
    public bool Intersects(double west, double south, double east, double north)
    {
        if (South > north || south > North)
        {
            return false;
        }

        if (east - west >= 360 || East - West >= 360)
        {
            return true;
        }

        var (w1, e1) = Unwrap(West, East);
        var (w2, e2) = Unwrap(west, east);
        foreach (var shift in AntimeridianShifts)
        {
            if (w1 <= e2 + shift && w2 + shift <= e1)
            {
                return true;
            }
        }

        return false;
    }

    private static (double West, double East) Unwrap(double west, double east) =>
        east < west ? (west, east + 360) : (west, east);

    private static IReadOnlyList<(double, double)> Close(IEnumerable<(double, double)> vertices)
    {
        var ring = vertices.ToList();
        if (ring[0] != ring[^1])
        {
            ring.Add(ring[0]);
        }

        return ring;
    }
}
