namespace EncDotNet.SoundCharts.Features;

/// <summary>
/// The type of a column in a SoundCharts feature table. The plugin
/// datasource maps each to Pro's <c>FieldType</c>.
/// </summary>
public enum FeatureFieldType
{
    /// <summary>The object id (<c>OBJECTID</c>), a 64-bit integer.</summary>
    ObjectId,

    /// <summary>The shape column of a feature class.</summary>
    Geometry,

    /// <summary>Text. Values are <see cref="string"/>.</summary>
    String,

    /// <summary>A 32-bit integer. Values are <see cref="int"/>.</summary>
    Integer,

    /// <summary>A double. Values are <see cref="double"/>.</summary>
    Double,

    /// <summary>A date and time, in UTC. Values are <see cref="DateTime"/>.</summary>
    Date,
}
