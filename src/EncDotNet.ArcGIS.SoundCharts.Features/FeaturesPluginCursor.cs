using ArcGIS.Core.Data.PluginDatastore;
using EncDotNet.SoundCharts.Features;

namespace EncDotNet.SoundCharts.PluginDatasource;

/// <summary>
/// A cursor over the rows a <see cref="FeaturesPluginTable"/> search found,
/// with every field's value in the table's field order.
/// </summary>
public sealed class FeaturesPluginCursor : PluginCursorTemplate
{
    private readonly FeatureTable _table;
    private readonly IEnumerator<FeatureRow> _rows;

    /// <summary>Initializes a new instance of the <see cref="FeaturesPluginCursor"/> class.</summary>
    /// <param name="table">The table the rows are from.</param>
    /// <param name="rows">The rows, read as the cursor moves.</param>
    public FeaturesPluginCursor(FeatureTable table, IEnumerable<FeatureRow> rows)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(rows);
        _table = table;
        _rows = rows.GetEnumerator();
    }

    /// <inheritdoc/>
    public override bool MoveNext() => _rows.MoveNext();

    /// <inheritdoc/>
    public override PluginRow GetCurrentRow()
    {
        var row = _rows.Current;
        var values = new List<object>(_table.Fields.Count);
        for (var i = 0; i < _table.Fields.Count; i++)
        {
            values.Add(_table.Fields[i].Type == FeatureFieldType.Geometry
                ? (row.Shape is { } shape ? FeaturesGeometry.ToGeometry(shape) : DBNull.Value)
                : row.Values[i] ?? DBNull.Value);
        }

        return new PluginRow(values);
    }
}
