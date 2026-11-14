using System.Text;

namespace EncDotNet.SoundCharts.Features;

/// <summary>
/// Table and field names of the SoundCharts plugin datasource. They're
/// part of every saved layer's data connection, so they must not change
/// between releases.
/// </summary>
public static class FeatureNames
{
    /// <summary>
    /// The plugin datasource's id: the <c>PluginDatasource</c> id in the
    /// plugin's Config.xml, which Pro's <c>PluginDatasourceConnectionPath</c>
    /// and every saved layer's data connection name.
    /// </summary>
    public const string PluginDatasourceId = "SoundCharts_FeaturesDatasource";

    /// <summary>The longest field name: a file geodatabase's limit, so the tables copy into one unchanged.</summary>
    public const int MaxFieldNameLength = 64;

    /// <summary>The object id field.</summary>
    public const string ObjectIdField = "OBJECTID";

    /// <summary>The shape field of a feature class.</summary>
    public const string ShapeField = "Shape";

    /// <summary>The feature's identifier in its dataset (the S-101 record id, the GML <c>gml:id</c>).</summary>
    public const string FeatureRefField = "FeatureRef";

    /// <summary>The feature type code (<c>DepthArea</c>).</summary>
    public const string FeatureTypeField = "FeatureType";

    /// <summary>The association table: one row per feature-to-feature association.</summary>
    public const string AssociationsTable = "FeatureAssociations";

    /// <summary>The association's code, in <see cref="AssociationsTable"/>.</summary>
    public const string AssociationField = "Association";

    /// <summary>The role of the target, in <see cref="AssociationsTable"/>.</summary>
    public const string RoleField = "Role";

    /// <summary>The associated feature's <see cref="FeatureRefField"/>, in <see cref="AssociationsTable"/>.</summary>
    public const string TargetRefField = "TargetRef";

    /// <summary>
    /// Gets the table name of a feature type's features of one geometry
    /// kind: the code and the kind, such as <c>Landmark_Point</c> or
    /// <c>DepthArea_Area</c>. The kind is always there, so a table keeps its
    /// name when a later edition adds the type's other geometry.
    /// </summary>
    /// <param name="featureType">The feature type code.</param>
    /// <param name="kind">The geometry kind.</param>
    public static string Table(string featureType, FeatureGeometryKind kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureType);
        var suffix = kind switch
        {
            FeatureGeometryKind.Point => "Point",
            FeatureGeometryKind.Multipoint => "Multipoint",
            FeatureGeometryKind.Polyline => "Line",
            FeatureGeometryKind.Polygon => "Area",
            _ => "Table",
        };
        return Field($"{featureType}_{suffix}");
    }

    /// <summary>
    /// Makes <paramref name="name"/> a valid field or table name: letters,
    /// digits and underscores, starting with a letter, at most
    /// <see cref="MaxFieldNameLength"/> characters.
    /// </summary>
    /// <param name="name">The name to clean.</param>
    public static string Field(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var builder = new StringBuilder(name.Length + 1);
        foreach (var c in name)
        {
            builder.Append(char.IsAsciiLetterOrDigit(c) || c == '_' ? c : '_');
        }

        if (builder.Length == 0 || !char.IsAsciiLetter(builder[0]))
        {
            builder.Insert(0, 'F');
        }

        return builder.Length > MaxFieldNameLength ? builder.ToString(0, MaxFieldNameLength) : builder.ToString();
    }

    /// <summary>
    /// Returns <paramref name="name"/>, or it with a number appended (and
    /// shortened to fit), so it isn't in <paramref name="taken"/>; adds the
    /// result to <paramref name="taken"/>.
    /// </summary>
    /// <param name="name">A valid field name.</param>
    /// <param name="taken">Names already used, compared ignoring case as geodatabases do.</param>
    public static string Unique(string name, ISet<string> taken)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(taken);
        var candidate = name;
        for (var i = 2; !taken.Add(candidate); i++)
        {
            var suffix = $"_{i}";
            candidate = (name.Length + suffix.Length > MaxFieldNameLength
                ? name[..(MaxFieldNameLength - suffix.Length)]
                : name) + suffix;
        }

        return candidate;
    }
}
