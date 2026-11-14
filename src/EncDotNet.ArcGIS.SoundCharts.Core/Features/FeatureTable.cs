using EncDotNet.S100.Pipelines;
using EncDotNet.S100.Pipelines.Vector;
using EncDotNet.S100.Pipelines.Vector.Spatial;

namespace EncDotNet.SoundCharts.Features;

/// <summary>
/// One table of a SoundCharts feature dataset: the features of one type and
/// geometry kind, or the dataset's feature associations. Immutable, so
/// any number of threads can search it.
/// </summary>
/// <remarks>
/// Spatial searches go through EncDotNet.S100's own spatial index
/// (<see cref="IVectorSpatialIndex"/>, an STR-packed R-tree over each
/// feature's bounding rectangle), built once when the table is.
/// </remarks>
public sealed class FeatureTable
{
    private static readonly IReadOnlyDictionary<string, object?> NoAttributes = new Dictionary<string, object?>();

    private readonly Dictionary<long, FeatureRow> _byObjectId;
    private readonly IVectorSpatialIndex? _index;

    /// <summary>Initializes a new instance of the <see cref="FeatureTable"/> class.</summary>
    /// <param name="name">The table name (<see cref="FeatureNames.Table"/>).</param>
    /// <param name="featureType">The feature type code, or <see langword="null"/> for the association table.</param>
    /// <param name="featureTypeName">The feature type's name from the feature catalogue, when known.</param>
    /// <param name="kind">The geometry kind.</param>
    /// <param name="fields">The fields, the object id first.</param>
    /// <param name="rows">The rows, in object id order.</param>
    public FeatureTable(
        string name,
        string? featureType,
        string? featureTypeName,
        FeatureGeometryKind kind,
        IReadOnlyList<FeatureField> fields,
        IReadOnlyList<FeatureRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(rows);
        Name = name;
        FeatureType = featureType;
        FeatureTypeName = featureTypeName;
        Kind = kind;
        Fields = fields;
        Rows = rows;
        _byObjectId = rows.ToDictionary(r => r.ObjectId);

        if (kind != FeatureGeometryKind.None)
        {
            var proxies = new List<Feature>(rows.Count);
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].Shape is { } shape)
                {
                    // The index reads only Coordinates for the bounding
                    // rectangle; its two corners are enough.
                    var e = shape.Envelope;
                    proxies.Add(new Feature
                    {
                        FeatureType = featureType ?? name,
                        Id = i,
                        GeometryType = GeometryType.Point,
                        Coordinates = [new(e.YMin, e.XMin), new(e.YMax, e.XMax)],
                        Attributes = NoAttributes,
                    });
                    Extent = Extent is { } extent ? extent.Union(e) : e;
                }
            }

            _index = IVectorSpatialIndex.Build(proxies);
        }
    }

    /// <summary>Gets the table name.</summary>
    public string Name { get; }

    /// <summary>Gets the feature type code; <see langword="null"/> for the association table.</summary>
    public string? FeatureType { get; }

    /// <summary>Gets the feature type's name from the feature catalogue, when the library resolved one.</summary>
    public string? FeatureTypeName { get; }

    /// <summary>Gets the feature type's name, or its code when it has none.</summary>
    public string DisplayName => FeatureTypeName ?? FeatureType ?? Name;

    /// <summary>Gets the geometry kind; <see cref="FeatureGeometryKind.None"/> for a table.</summary>
    public FeatureGeometryKind Kind { get; }

    /// <summary>Gets whether the table has a shape field.</summary>
    public bool IsSpatial => Kind != FeatureGeometryKind.None;

    /// <summary>Gets the fields, the object id first.</summary>
    public IReadOnlyList<FeatureField> Fields { get; }

    /// <summary>Gets the rows, in object id order.</summary>
    public IReadOnlyList<FeatureRow> Rows { get; }

    /// <summary>Gets the bounding rectangle of every shape, or <see langword="null"/> when there are none.</summary>
    public FeatureEnvelope? Extent { get; }

    /// <summary>Gets the row with object id <paramref name="objectId"/>, or <see langword="null"/>.</summary>
    /// <param name="objectId">The object id.</param>
    public FeatureRow? Find(long objectId) => _byObjectId.GetValueOrDefault(objectId);

    /// <summary>
    /// Returns the rows whose shape's bounding rectangle meets
    /// <paramref name="envelope"/> (all rows when it's
    /// <see langword="null"/>), limited to <paramref name="objectIds"/> when
    /// given, in object id order. A table without geometry ignores
    /// <paramref name="envelope"/>.
    /// </summary>
    /// <param name="envelope">The WGS 84 rectangle, or <see langword="null"/>.</param>
    /// <param name="objectIds">The object ids to choose from, or <see langword="null"/> or empty for all.</param>
    public IEnumerable<FeatureRow> Search(FeatureEnvelope? envelope, IReadOnlyCollection<long>? objectIds = null)
    {
        IEnumerable<FeatureRow> rows;
        if (envelope is { } e && _index is { } index)
        {
            rows = index.Query(new BoundingBox(e.YMin, e.XMin, e.YMax, e.XMax))
                .Select(f => (int)f.Id)
                .Order()
                .Select(i => Rows[i]);
        }
        else
        {
            rows = Rows;
        }

        if (objectIds is { Count: > 0 })
        {
            var wanted = objectIds as ISet<long> ?? objectIds.ToHashSet();
            rows = rows.Where(r => wanted.Contains(r.ObjectId));
        }

        return rows;
    }
}
