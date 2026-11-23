using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Geometry;
using EncDotNet.SoundCharts.Features;

namespace EncDotNet.SoundCharts.PluginDatasource;

/// <summary>
/// One <see cref="FeatureTable"/> as a Pro plugin table: a feature class
/// in WGS 84 when it has geometry, a table otherwise.
/// </summary>
public sealed class FeaturesPluginTable : PluginTableTemplate
{
    private readonly FeatureTable _table;
    private readonly IReadOnlyList<PluginField> _fields;

    /// <summary>Initializes a new instance of the <see cref="FeaturesPluginTable"/> class.</summary>
    /// <param name="table">The table.</param>
    public FeaturesPluginTable(FeatureTable table)
    {
        ArgumentNullException.ThrowIfNull(table);
        _table = table;
        _fields = [.. table.Fields.Select(f => new PluginField(f.Name, f.Alias, FieldTypeOf(f.Type)) { Length = f.Length })];
    }

    /// <inheritdoc/>
    public override string GetName() => _table.Name;

    /// <inheritdoc/>
    public override IReadOnlyList<PluginField> GetFields() => _fields;

    /// <inheritdoc/>
    public override GeometryType GetShapeType() => _table.Kind switch
    {
        FeatureGeometryKind.Point => GeometryType.Point,
        FeatureGeometryKind.Multipoint => GeometryType.Multipoint,
        FeatureGeometryKind.Polyline => GeometryType.Polyline,
        FeatureGeometryKind.Polygon => GeometryType.Polygon,
        _ => GeometryType.Unknown,
    };

    /// <inheritdoc/>
    public override Envelope? GetExtent()
    {
        if (!_table.IsSpatial)
        {
            return null;
        }

        return _table.Extent is { } e
            ? EnvelopeBuilderEx.CreateEnvelope(e.XMin, e.YMin, e.XMax, e.YMax, SpatialReferences.WGS84)
            : EnvelopeBuilderEx.CreateEnvelope(SpatialReferences.WGS84);
    }

    /// <inheritdoc/>
    public override bool IsNativeRowCountSupported() => true;

    /// <inheritdoc/>
    public override long GetNativeRowCount() => _table.Rows.Count;

    /// <inheritdoc/>
    public override PluginCursorTemplate Search(QueryFilter queryFilter)
    {
        ArgumentNullException.ThrowIfNull(queryFilter);
        return new FeaturesPluginCursor(_table, _table.Search(null, ObjectIds(queryFilter)));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The library's spatial index finds the features whose bounding
    /// rectangles meet the filter geometry's; Pro's geometry engine then
    /// tests the relationship asked for.
    /// </remarks>
    public override PluginCursorTemplate Search(SpatialQueryFilter spatialQueryFilter)
    {
        ArgumentNullException.ThrowIfNull(spatialQueryFilter);
        var objectIds = ObjectIds(spatialQueryFilter);
        if (!_table.IsSpatial || spatialQueryFilter.FilterGeometry is not { IsEmpty: false } filterGeometry)
        {
            return new FeaturesPluginCursor(_table, _table.Search(null, objectIds));
        }

        var filter = filterGeometry.SpatialReference is { } sr && !sr.IsEqual(SpatialReferences.WGS84)
            ? GeometryEngine.Instance.Project(filterGeometry, SpatialReferences.WGS84)
            : filterGeometry;
        var extent = filter.Extent;
        var candidates = _table.Search(new FeatureEnvelope(extent.XMin, extent.YMin, extent.XMax, extent.YMax), objectIds);
        var relationship = spatialQueryFilter.SpatialRelationship;
        if (relationship is SpatialRelationship.EnvelopeIntersects or SpatialRelationship.IndexIntersects)
        {
            return new FeaturesPluginCursor(_table, candidates);
        }

        var description = spatialQueryFilter.SpatialRelationshipDescription;
        return new FeaturesPluginCursor(
            _table,
            candidates.Where(row => row.Shape is { } shape && Matches(filter, FeaturesGeometry.ToGeometry(shape), relationship, description)));
    }

    // The filter geometry is the query geometry: Contains means it contains the feature.
    private static bool Matches(Geometry filter, Geometry shape, SpatialRelationship relationship, string? description) => relationship switch
    {
        SpatialRelationship.Contains => GeometryEngine.Instance.Contains(filter, shape),
        SpatialRelationship.Within => GeometryEngine.Instance.Within(filter, shape),
        SpatialRelationship.Touches => GeometryEngine.Instance.Touches(filter, shape),
        SpatialRelationship.Overlaps => GeometryEngine.Instance.Overlaps(filter, shape),
        SpatialRelationship.Crosses => GeometryEngine.Instance.Crosses(filter, shape),
        SpatialRelationship.Relation when !string.IsNullOrEmpty(description) => GeometryEngine.Instance.Relate(filter, shape, description),
        _ => GeometryEngine.Instance.Intersects(filter, shape),
    };

    private static IReadOnlyCollection<long>? ObjectIds(QueryFilter filter) =>
        filter.ObjectIDs is { Count: > 0 } ids ? ids.ToHashSet() : null;

    private static FieldType FieldTypeOf(FeatureFieldType type) => type switch
    {
        FeatureFieldType.ObjectId => FieldType.OID,
        FeatureFieldType.Geometry => FieldType.Geometry,
        FeatureFieldType.Integer => FieldType.Integer,
        FeatureFieldType.Double => FieldType.Double,
        FeatureFieldType.Date => FieldType.Date,
        _ => FieldType.String,
    };
}
