namespace EncDotNet.SoundCharts.Features;

/// <summary>
/// The shape type of a SoundCharts feature table. A Pro feature class has
/// one shape type, so a feature type with point and area instances (an
/// S-101 <c>Landmark</c>, say) becomes two tables.
/// </summary>
public enum FeatureGeometryKind
{
    /// <summary>No geometry: a table, not a feature class.</summary>
    None,

    /// <summary>One position per feature.</summary>
    Point,

    /// <summary>Several positions per feature, such as S-101 soundings.</summary>
    Multipoint,

    /// <summary>One or more curves per feature.</summary>
    Polyline,

    /// <summary>A surface: an exterior ring and any interior rings.</summary>
    Polygon,
}
