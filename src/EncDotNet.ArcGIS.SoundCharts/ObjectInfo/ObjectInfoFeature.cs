using EncDotNet.S100.Datasets.Pipelines;

namespace EncDotNet.SoundCharts.ObjectInfo;

/// <summary>The kind of thing Object information found.</summary>
public enum ObjectInfoFeatureKind
{
    /// <summary>A point feature within the search radius.</summary>
    Point,

    /// <summary>A curve feature within the search radius.</summary>
    Curve,

    /// <summary>An area feature that contains the point.</summary>
    Surface,

    /// <summary>
    /// A coverage value sampled at the point: an S-102 depth, an S-104
    /// water level or an S-111 surface current.
    /// </summary>
    Coverage,
}

/// <summary>
/// One feature or coverage value at the point, as the viewer's Object
/// information shows it.
/// </summary>
/// <param name="FeatureRef">The feature's id in its dataset; for a coverage value, the grid cell or station.</param>
/// <param name="FeatureType">The feature type code, such as <c>LightAllAround</c>.</param>
/// <param name="FeatureTypeName">The feature type's catalogue name; <c>null</c> when the catalogue has none.</param>
/// <param name="Kind">Whether it's a point, curve or area feature, or a coverage value.</param>
/// <param name="DistanceMeters">The distance from the point to the feature, 0 when the point is inside it.</param>
/// <param name="Attributes">The attributes, information associations and coverage values, in display order.</param>
/// <param name="References">The feature's associations to other features.</param>
/// <param name="Geometry">The feature's geometry, for the highlight; <c>null</c> for a coverage value.</param>
public sealed record ObjectInfoFeature(
    string FeatureRef,
    string FeatureType,
    string? FeatureTypeName,
    ObjectInfoFeatureKind Kind,
    double DistanceMeters,
    IReadOnlyList<ObjectInfoAttribute> Attributes,
    IReadOnlyList<ObjectInfoReference> References,
    S100FeatureGeometry? Geometry)
{
    /// <summary>Gets the title to show: the catalogue name when there is one, otherwise the code.</summary>
    public string Title => FeatureTypeName ?? FeatureType;
}

/// <summary>
/// One attribute row. Complex attributes are flattened: the parent row comes
/// first, then its sub-attributes one <see cref="Level"/> deeper.
/// </summary>
/// <param name="Code">The attribute code as encoded, such as <c>OBJNAM</c>.</param>
/// <param name="Name">The catalogue name, or the code when the catalogue has none.</param>
/// <param name="Value">The decoded value to show.</param>
/// <param name="RawValue">The value as encoded.</param>
/// <param name="Level">How deep the row is: 0 for a feature's own attribute, 1 for a sub-attribute, and so on.</param>
/// <param name="ExternalText">The text of a file the attribute refers to, when the library resolved it.</param>
public sealed record ObjectInfoAttribute(
    string Code,
    string Name,
    string Value,
    string RawValue,
    int Level,
    string? ExternalText = null);

/// <summary>An association from a feature to another feature or information type.</summary>
/// <param name="Role">The association's role, such as <c>AtoNStatus</c>.</param>
/// <param name="TargetRef">The target's id in the same dataset.</param>
/// <param name="Target">What to show for the target: its type name when the dataset can resolve it, otherwise its id.</param>
public sealed record ObjectInfoReference(string Role, string TargetRef, string Target);
