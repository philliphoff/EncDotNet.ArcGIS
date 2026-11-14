using EncDotNet.S100.DataModel;
using EncDotNet.S100.Datasets.Pipelines;
using EncDotNet.S100.Pipelines.Vector;

namespace EncDotNet.SoundCharts.Features;

/// <summary>
/// A feature's geometry as the library gives it, from either of its two
/// sources: the dataset processor (GML products) or the S-101 vector
/// source (ISO 8211 products, whose processor doesn't give geometry).
/// </summary>
/// <param name="Points">The positions of a point or multipoint feature.</param>
/// <param name="Curves">The curves of a curve feature.</param>
/// <param name="Surfaces">The surfaces of a surface feature: each an exterior ring and its interior rings.</param>
internal sealed record SourceGeometry(
    IReadOnlyList<GeoPosition> Points,
    IReadOnlyList<IReadOnlyList<GeoPosition>> Curves,
    IReadOnlyList<(IReadOnlyList<GeoPosition> Exterior, IReadOnlyList<IReadOnlyList<GeoPosition>> Interiors)> Surfaces)
{
    /// <summary>Gets the geometry kind, before points and multipoints are told apart.</summary>
    public FeatureGeometryKind BaseKind =>
        Surfaces.Any(s => s.Exterior.Count >= 3) ? FeatureGeometryKind.Polygon
        : Curves.Any(c => c.Count >= 2) ? FeatureGeometryKind.Polyline
        : Points.Count > 0 ? FeatureGeometryKind.Point
        : FeatureGeometryKind.None;

    /// <summary>Converts the processor's geometry.</summary>
    public static SourceGeometry? From(S100FeatureGeometry? geometry) => geometry is null
        ? null
        : new SourceGeometry(
            geometry.Points,
            geometry.Curves,
            geometry.ExteriorRing.Count > 0 ? [(geometry.ExteriorRing, geometry.InteriorRings)] : []);

    /// <summary>Converts a vector source feature's geometry.</summary>
    public static SourceGeometry From(Feature feature) => feature.GeometryType switch
    {
        GeometryType.Point => new SourceGeometry(feature.Coordinates, [], []),
        GeometryType.Curve => new SourceGeometry(
            [],
            feature.Curves.Count > 0 ? feature.Curves : feature.Coordinates.Count > 0 ? [feature.Coordinates] : [],
            []),
        GeometryType.Surface => new SourceGeometry(
            [],
            [],
            feature.SurfaceParts.Count > 0
                ? [.. feature.SurfaceParts.Select(p => (p.ExteriorRing, p.InteriorRings))]
                : feature.Coordinates.Count > 0 ? [(feature.Coordinates, feature.InteriorRings)] : []),
        _ => new SourceGeometry([], [], []),
    };
}
