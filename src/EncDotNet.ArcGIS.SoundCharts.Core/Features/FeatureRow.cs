namespace EncDotNet.SoundCharts.Features;

/// <summary>A row of a SoundCharts feature table.</summary>
public sealed class FeatureRow
{
    /// <summary>Initializes a new instance of the <see cref="FeatureRow"/> class.</summary>
    /// <param name="objectId">The object id, unique in the dataset.</param>
    /// <param name="values">
    /// One value per field of the table, in the table's field order:
    /// <see cref="long"/> for the object id, <see langword="null"/> for the
    /// shape (it's <paramref name="shape"/>) and for empty values, otherwise
    /// as the field's <see cref="FeatureFieldType"/> says.
    /// </param>
    /// <param name="shape">The geometry, or <see langword="null"/> for a table row or a feature without one.</param>
    public FeatureRow(long objectId, IReadOnlyList<object?> values, FeatureShape? shape)
    {
        ArgumentNullException.ThrowIfNull(values);
        ObjectId = objectId;
        Values = values;
        Shape = shape;
    }

    /// <summary>Gets the object id.</summary>
    public long ObjectId { get; }

    /// <summary>Gets the values, one per field.</summary>
    public IReadOnlyList<object?> Values { get; }

    /// <summary>Gets the geometry.</summary>
    public FeatureShape? Shape { get; }
}
