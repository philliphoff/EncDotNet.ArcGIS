using System.Globalization;
using EncDotNet.S100;
using EncDotNet.S100.DataModel;
using EncDotNet.S100.Datasets.Pipelines;
using EncDotNet.S100.Datasets.S101;
using EncDotNet.S100.Datasets.S57;

namespace EncDotNet.SoundCharts.Features;

/// <summary>
/// A dataset's features as tables, the model behind the SoundCharts plugin
/// datasource (design brief §3.2, issue #15): one table per feature type
/// and geometry kind, with attributes flattened into columns, and one table
/// of feature associations.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why per feature type.</b> A layer per feature type is what the brief
/// names (<b>{feature type} · {dataset}</b>), what a user selects and
/// symbolizes by, and what keeps each table's columns to that type's
/// attributes. A table per geometry kind alone would carry the union of
/// every type's attributes, hundreds of mostly empty columns, and need a
/// definition query to find one type. A Pro feature class has one shape
/// type, so a feature type found with several geometry kinds is split
/// (<c>Landmark_Point</c>, <c>Landmark_Area</c>).
/// </para>
/// <para>
/// <b>Columns.</b> Every table has <c>OBJECTID</c>, <c>Shape</c> (feature
/// classes), <c>FeatureRef</c> and <c>FeatureType</c>, then the attributes
/// the dataset's features of that type carry, as
/// <see cref="AttributeFlattener"/> flattens them. The schema comes from
/// the data rather than the feature catalogue, which lists every attribute
/// a type may have. A column whose every value is an integer, a number or
/// a single date is typed so; the rest are text.
/// </para>
/// <para>
/// <b>Associations.</b> Information types the library resolves for a
/// feature are columns of its row. Feature-to-feature associations are
/// rows of <see cref="FeatureNames.AssociationsTable"/>, keyed by
/// <c>FeatureRef</c> and <c>TargetRef</c>, for relates and joins.
/// </para>
/// <para>
/// Object ids are the features' positions in the library's enumeration,
/// plus one, so they're unique across a dataset's tables and stable for
/// the same file.
/// </para>
/// </remarks>
public sealed class DatasetTables
{
    private readonly Dictionary<string, FeatureTable> _byName;

    private DatasetTables(string path, string product, IReadOnlyList<FeatureTable> tables)
    {
        Path = path;
        Product = product;
        Tables = tables;
        _byName = tables.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Gets the dataset's full path.</summary>
    public string Path { get; }

    /// <summary>Gets the dataset's name for layer names: its file name without the extension.</summary>
    public string Name => NameOf(Path);

    /// <summary>Gets the product specification, such as <c>S-101</c>.</summary>
    public string Product { get; }

    /// <summary>Gets the tables: feature types by name, then the association table when there is one.</summary>
    public IReadOnlyList<FeatureTable> Tables { get; }

    /// <summary>Gets the table named <paramref name="name"/> (ignoring case), or <see langword="null"/>.</summary>
    /// <param name="name">The table name.</param>
    public FeatureTable? Table(string name) => _byName.GetValueOrDefault(name);

    /// <summary>Gets a dataset's name for layer names from its path.</summary>
    /// <param name="path">The dataset's path.</param>
    public static string NameOf(string path) => System.IO.Path.GetFileNameWithoutExtension(path);

    /// <summary>
    /// Reads the dataset at <paramref name="path"/> with EncDotNet.S100,
    /// applying S-101 update files beside a base cell.
    /// </summary>
    /// <param name="path">The dataset file.</param>
    /// <param name="factory">The processor factory; the library's bundled one when <see langword="null"/>.</param>
    /// <exception cref="FileNotFoundException">There's no file at <paramref name="path"/>.</exception>
    public static DatasetTables Open(string path, IDatasetProcessorFactory? factory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var full = System.IO.Path.GetFullPath(path);
        if (!File.Exists(full))
        {
            throw new FileNotFoundException("The dataset doesn't exist.", full);
        }

        if (factory is not null)
        {
            return OpenWith(factory, full);
        }

        using var bundled = BundledDatasetProcessorFactory.Create();
        return OpenWith(bundled, full);
    }

    private static DatasetTables OpenWith(IDatasetProcessorFactory factory, string path)
    {
        var processor = factory.CreateProcessorWithFilesystemUpdates(path);
        try
        {
            var geometries = processor.Spec.Name switch
            {
                "S-101" => VectorGeometries(S101Dataset.OpenWithUpdates(path, UpdateFiles(path))),
                "S-57" => VectorGeometries(S57Cell(path)),
                _ => null,
            };
            return Build(processor, path, geometries);
        }
        finally
        {
            (processor as IDisposable)?.Dispose();
        }
    }

    /// <summary>Builds the tables of <paramref name="processor"/>'s features.</summary>
    /// <param name="processor">An open dataset.</param>
    /// <param name="path">The dataset's path.</param>
    public static DatasetTables Build(IDatasetProcessor processor, string path) => Build(processor, path, null);

    // ISO 8211 products: the processor gives attributes but not geometry, so
    // geometry comes from the library's S-101 vector source (S-57 through its
    // S-57 to S-101 translation, as the processor does), matched by feature
    // reference (the record id).
    private static DatasetTables Build(IDatasetProcessor processor, string path, Dictionary<string, SourceGeometry>? geometries)
    {
        ArgumentNullException.ThrowIfNull(processor);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var features = new List<SourceFeature>();
        foreach (var summary in processor.EnumerateFeatures())
        {
            var info = processor.GetFeatureInfoAt(summary.Ordinal) ?? processor.GetFeatureInfo(summary.FeatureRef);
            var geometry = geometries is not null
                ? geometries.GetValueOrDefault(summary.FeatureRef)
                : SourceGeometry.From(processor.GetFeatureGeometryAt(summary.Ordinal));
            features.Add(new SourceFeature(
                summary.Ordinal + 1L,
                summary.FeatureRef,
                summary.FeatureType,
                summary.FeatureTypeName ?? info?.FeatureTypeName,
                info,
                geometry));
        }

        var tables = new List<FeatureTable>();
        foreach (var group in features
            .GroupBy(f => (f.FeatureType, Kind: f.Geometry?.BaseKind ?? FeatureGeometryKind.None))
            .OrderBy(g => g.Key.FeatureType, StringComparer.Ordinal)
            .ThenBy(g => g.Key.Kind))
        {
            tables.Add(BuildTable(group.Key.FeatureType, group.Key.Kind, [.. group]));
        }

        if (BuildAssociations(features) is { } associations)
        {
            tables.Add(associations);
        }

        return new DatasetTables(path, processor.Spec.Name, tables);
    }

    /// <summary>
    /// Gets the S-101 update files beside a base cell: the same name with
    /// a numeric extension (<c>.001</c>, <c>.002</c>), in order.
    /// </summary>
    /// <param name="path">The base cell (<c>.000</c>).</param>
    public static IReadOnlyList<string> UpdateFiles(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var folder = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path));
        if (folder is null || !Directory.Exists(folder))
        {
            return [];
        }

        var stem = System.IO.Path.GetFileNameWithoutExtension(path);
        return [.. Directory.EnumerateFiles(folder, stem + ".*")
            .Select(f => (File: f, Extension: System.IO.Path.GetExtension(f).TrimStart('.')))
            .Where(f => f.Extension.Length == 3 && f.Extension.All(char.IsAsciiDigit) && f.Extension != "000")
            .OrderBy(f => int.Parse(f.Extension, CultureInfo.InvariantCulture))
            .Select(f => f.File)];
    }

    private static S101Dataset S57Cell(string path)
    {
        var s57 = S57Dataset.Open(path);
        return S101Dataset.FromDocument(S57ToS101Translator.ForTarget(s57.TranslationTarget).Translate(s57));
    }

    private static Dictionary<string, SourceGeometry> VectorGeometries(S101Dataset dataset)
    {
        var result = new Dictionary<string, SourceGeometry>(StringComparer.Ordinal);
        foreach (var feature in new S101VectorSource(dataset).GetFeatures())
        {
            result[feature.Id.ToString(CultureInfo.InvariantCulture)] = SourceGeometry.From(feature);
        }

        return result;
    }

    private static FeatureTable BuildTable(string featureType, FeatureGeometryKind baseKind, List<SourceFeature> features)
    {
        var kind = baseKind == FeatureGeometryKind.Point && features.Any(f => f.Geometry?.Points.Count > 1)
            ? FeatureGeometryKind.Multipoint
            : baseKind;

        var fields = new List<FeatureField> { new(FeatureNames.ObjectIdField, "Object ID", FeatureFieldType.ObjectId) };
        if (kind != FeatureGeometryKind.None)
        {
            fields.Add(new FeatureField(FeatureNames.ShapeField, "Shape", FeatureFieldType.Geometry));
        }

        var flat = features.Select(f => f.Info is { } info ? AttributeFlattener.Flatten(info.Attributes) : []).ToList();
        var taken = new HashSet<string>(fields.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
        fields.Add(new FeatureField(
            FeatureNames.Unique(FeatureNames.FeatureRefField, taken), "Feature reference", FeatureFieldType.String,
            TextLength(features.Select(f => f.FeatureRef))));
        fields.Add(new FeatureField(
            FeatureNames.Unique(FeatureNames.FeatureTypeField, taken), "Feature type", FeatureFieldType.String,
            TextLength([featureType])));
        var fixedCount = fields.Count;

        // Columns in first-seen order across the type's features.
        var columns = new List<(string Path, string Alias)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var attribute in flat.SelectMany(a => a))
        {
            if (seen.Add(attribute.Path))
            {
                columns.Add((attribute.Path, attribute.Alias));
            }
        }

        var columnIndex = columns.Select((c, i) => (c.Path, i)).ToDictionary(c => c.Path, c => c.i, StringComparer.Ordinal);
        var cells = new FlatAttribute?[features.Count, columns.Count];
        for (var row = 0; row < features.Count; row++)
        {
            foreach (var attribute in flat[row])
            {
                cells[row, columnIndex[attribute.Path]] = attribute;
            }
        }

        var types = new FeatureFieldType[columns.Count];
        for (var c = 0; c < columns.Count; c++)
        {
            var values = Enumerable.Range(0, features.Count).Select(r => cells[r, c]).OfType<FlatAttribute>().Where(a => a.Text is not null).ToList();
            types[c] = InferType(values);
            fields.Add(new FeatureField(
                FeatureNames.Unique(FeatureNames.Field(columns[c].Path), taken),
                columns[c].Alias,
                types[c],
                types[c] == FeatureFieldType.String ? TextLength(values.Select(v => v.Text)) : 0));
        }

        var rows = new List<FeatureRow>(features.Count);
        for (var r = 0; r < features.Count; r++)
        {
            var feature = features[r];
            var values = new object?[fields.Count];
            values[0] = feature.ObjectId;
            values[fixedCount - 2] = feature.FeatureRef;
            values[fixedCount - 1] = feature.FeatureType;
            for (var c = 0; c < columns.Count; c++)
            {
                values[fixedCount + c] = cells[r, c] is { Text: { } text } cell ? Convert(cell, text, types[c]) : null;
            }

            rows.Add(new FeatureRow(feature.ObjectId, values, kind != FeatureGeometryKind.None && feature.Geometry is { } geometry ? Shape(kind, geometry) : null));
        }

        var typeName = features.Select(f => f.FeatureTypeName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
        return new FeatureTable(FeatureNames.Table(featureType, kind), featureType, typeName, kind, fields, rows);
    }

    private static FeatureTable? BuildAssociations(List<SourceFeature> features)
    {
        var links = features
            .SelectMany(f => (f.Info?.References ?? []).Select(r => (Feature: f, Reference: r)))
            .ToList();
        if (links.Count == 0)
        {
            return null;
        }

        FeatureField[] fields =
        [
            new(FeatureNames.ObjectIdField, "Object ID", FeatureFieldType.ObjectId),
            new(FeatureNames.FeatureRefField, "Feature reference", FeatureFieldType.String, TextLength(links.Select(l => l.Feature.FeatureRef))),
            new(FeatureNames.FeatureTypeField, "Feature type", FeatureFieldType.String, TextLength(links.Select(l => l.Feature.FeatureType))),
            new(FeatureNames.AssociationField, "Association", FeatureFieldType.String, TextLength(links.Select(l => l.Reference.Role))),
            new(FeatureNames.RoleField, "Role", FeatureFieldType.String, TextLength(links.Select(l => l.Reference.ArcRole))),
            new(FeatureNames.TargetRefField, "Target feature reference", FeatureFieldType.String, TextLength(links.Select(l => l.Reference.TargetRef))),
        ];
        var rows = links
            .Select((l, i) => new FeatureRow(
                i + 1L,
                [i + 1L, l.Feature.FeatureRef, l.Feature.FeatureType, l.Reference.Role, l.Reference.ArcRole, l.Reference.TargetRef],
                null))
            .ToList();
        return new FeatureTable(FeatureNames.AssociationsTable, null, null, FeatureGeometryKind.None, fields, rows);
    }

    private static FeatureFieldType InferType(List<FlatAttribute> values)
    {
        if (values.Count == 0 || values.Any(v => v.IsRepeated))
        {
            return FeatureFieldType.String;
        }

        if (values.All(v => v.Date is not null))
        {
            return FeatureFieldType.Date;
        }

        if (values.All(v => int.TryParse(v.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _)))
        {
            return FeatureFieldType.Integer;
        }

        return values.All(v => double.TryParse(v.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && double.IsFinite(d))
            ? FeatureFieldType.Double
            : FeatureFieldType.String;
    }

    private static object Convert(FlatAttribute cell, string text, FeatureFieldType type) => type switch
    {
        FeatureFieldType.Date when cell.Date is { } date => date.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(date, DateTimeKind.Utc) : date,
        FeatureFieldType.Integer => int.Parse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture),
        FeatureFieldType.Double => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture),
        _ => text,
    };

    // At least 16, so short columns don't truncate when a later edition has a longer value.
    private static int TextLength(IEnumerable<string?> values) => Math.Max(16, values.Select(v => v?.Length ?? 0).DefaultIfEmpty(0).Max());

    private static FeatureShape? Shape(FeatureGeometryKind kind, SourceGeometry geometry)
    {
        static FeatureVertex V(GeoPosition p) => new(p.Longitude, p.Latitude);
        IReadOnlyList<IReadOnlyList<FeatureVertex>> parts = kind switch
        {
            FeatureGeometryKind.Point or FeatureGeometryKind.Multipoint when geometry.Points.Count > 0 =>
                kind == FeatureGeometryKind.Point ? [[V(geometry.Points[0])]] : [[.. geometry.Points.Select(V)]],
            FeatureGeometryKind.Polyline =>
                [.. geometry.Curves.Where(c => c.Count >= 2).Select(c => (IReadOnlyList<FeatureVertex>)[.. c.Select(V)])],
            FeatureGeometryKind.Polygon =>
                [.. geometry.Surfaces.Where(s => s.Exterior.Count >= 3).SelectMany(s => (IEnumerable<IReadOnlyList<FeatureVertex>>)
                [
                    FeatureShape.OrientRing([.. s.Exterior.Select(V)], clockwise: true),
                    .. s.Interiors.Where(r => r.Count >= 3).Select(r => FeatureShape.OrientRing([.. r.Select(V)], clockwise: false)),
                ])],
            _ => [],
        };
        return parts.Count > 0 && parts.Any(p => p.Count > 0) ? new FeatureShape(kind, parts) : null;
    }

    private sealed record SourceFeature(
        long ObjectId,
        string FeatureRef,
        string FeatureType,
        string? FeatureTypeName,
        FeatureInfo? Info,
        SourceGeometry? Geometry);
}
