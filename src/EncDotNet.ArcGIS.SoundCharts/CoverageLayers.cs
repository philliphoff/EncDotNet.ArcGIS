using System.IO;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.ChartLayers;
using EncDotNet.SoundCharts.Resources;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;
using GeometryType = ArcGIS.Core.Geometry.GeometryType;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The <b>Coverage</b> layer of a SoundCharts group (design brief §3.2,
/// D12, D13, D15): a native feature layer over a feature class in the
/// project's <c>SoundCharts.gdb</c>, one outlined, labelled footprint per
/// dataset. Every method here runs on the MCT.
/// </summary>
internal static class CoverageLayers
{
    /// <summary>The outline: a chart's magenta, like data limits on a paper chart.</summary>
    private static readonly CIMColor Outline = ColorFactory.Instance.CreateRGBColor(163, 0, 163);

    /// <summary>Finds the Coverage layer of <paramref name="map"/>, by its marker.</summary>
    /// <param name="map">The map.</param>
    public static FeatureLayer? Find(Map map) =>
        map.GetLayersAsFlattenedList()
            .OfType<FeatureLayer>()
            .FirstOrDefault(l => ChartLayerManager.ReadMarker(l) is { IsCoverage: true });

    /// <summary>
    /// Creates a new coverage feature class in the project's
    /// <c>SoundCharts.gdb</c> (creating the geodatabase if needed) and a
    /// <b>Coverage</b> layer over it at the bottom of <paramref name="container"/>.
    /// </summary>
    /// <param name="container">The SoundCharts group, or the map.</param>
    public static FeatureLayer Create(ILayerContainerEdit container)
    {
        var path = Path.Combine(Project.Current.HomeFolderPath, CoverageTable.GeodatabaseName);
        var connection = new FileGeodatabaseConnectionPath(new Uri(path));
        using var geodatabase = Directory.Exists(path) ? new Geodatabase(connection) : SchemaBuilder.CreateGeodatabase(connection);

        var existing = geodatabase.GetDefinitions<FeatureClassDefinition>().Select(d => d.GetName()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var name = CoverageTable.NewFeatureClassName();
        while (existing.Contains(name))
        {
            name = CoverageTable.NewFeatureClassName();
        }

        var description = new FeatureClassDescription(name, Fields(), new ShapeDescription(GeometryType.Polygon, SpatialReferences.WGS84))
        {
            AliasName = Strings.Layer_Coverage,
        };
        var builder = new SchemaBuilder(geodatabase);
        builder.Create(description);
        if (!builder.Build())
        {
            throw new InvalidOperationException($"Creating {name} in {path} failed: {string.Join("; ", builder.ErrorMessages)}");
        }

        using var featureClass = geodatabase.OpenDataset<FeatureClass>(name);
        var parameters = new FeatureLayerCreationParams(featureClass)
        {
            Name = Strings.Layer_Coverage,
            MapMemberPosition = MapMemberPosition.AddToBottom,
        };
        var layer = LayerFactory.Instance.CreateLayer<FeatureLayer>(parameters, container);
        Style(layer);
        return layer;
    }

    /// <summary>Reads each feature's dataset path and <see cref="CoverageTable.KeyOf(ChartLayerDataset)"/>.</summary>
    /// <param name="layer">The Coverage layer.</param>
    public static List<KeyValuePair<string, string>> ReadKeys(FeatureLayer layer)
    {
        var keys = new List<KeyValuePair<string, string>>();
        using var featureClass = layer.GetFeatureClass();
        if (featureClass is null)
        {
            return keys;
        }

        using var cursor = featureClass.Search(null, false);
        while (cursor.MoveNext())
        {
            using var row = cursor.Current;
            if (Text(row, CoverageTable.PathField) is { Length: > 0 } path)
            {
                keys.Add(new(path, CoverageTable.KeyOf(Text(row, CoverageTable.ProductField), Text(row, CoverageTable.EditionField), Text(row, CoverageTable.LayerField))));
            }
        }

        return keys;
    }

    /// <summary>
    /// Deletes the footprints of <paramref name="delete"/> and writes
    /// <paramref name="add"/>, outside Pro's undo stack: the footprints follow
    /// the chart, they're not the user's edits.
    /// </summary>
    /// <param name="layer">The Coverage layer.</param>
    /// <param name="add">The footprints to write.</param>
    /// <param name="delete">The dataset paths whose features go first.</param>
    public static void Write(FeatureLayer layer, IReadOnlyList<DatasetFootprint> add, IReadOnlyList<string> delete)
    {
        if (add.Count == 0 && delete.Count == 0)
        {
            return;
        }

        using var featureClass = layer.GetFeatureClass() ?? throw new InvalidOperationException($"{layer.Name} has no feature class.");
        using var geodatabase = featureClass.GetDatastore() as Geodatabase ?? throw new InvalidOperationException($"{layer.Name} isn't in a geodatabase.");
        var shapeField = featureClass.GetDefinition().GetShapeField();
        var shapes = add.Select(f => (Footprint: f, Shape: ToPolygon(f))).Where(s => s.Shape is not null).ToList();

        try
        {
            geodatabase.ApplyEdits(() =>
            {
                foreach (var path in delete.Concat(add.Select(f => f.Dataset.Path)))
                {
                    featureClass.DeleteRows(new QueryFilter { WhereClause = CoverageTable.WherePath(path) });
                }

                foreach (var (footprint, shape) in shapes)
                {
                    using var buffer = featureClass.CreateRowBuffer();
                    foreach (var (field, value) in CoverageTable.ValuesOf(footprint))
                    {
                        buffer[field] = value;
                    }

                    buffer[shapeField] = shape;
                    featureClass.CreateRow(buffer).Dispose();
                }
            });
        }
        catch (InvalidOperationException)
        {
            // The user has an edit session on SoundCharts.gdb: edit through it instead.
            WriteInEditSession(layer, featureClass, shapeField, shapes, delete);
        }
    }

    /// <summary>
    /// Labels each layer's footprints only below that layer's minimum scale
    /// (D13): where the chart draws, its own text says what it is.
    /// </summary>
    /// <param name="layer">The Coverage layer.</param>
    /// <param name="minScales">Each chart layer's minimum scale; 0 or missing means the chart draws at every scale.</param>
    public static void UpdateLabels(FeatureLayer layer, IReadOnlyDictionary<ChartLayerKind, double> minScales)
    {
        if (layer.GetDefinition() is not CIMFeatureLayer cim)
        {
            return;
        }

        var template = cim.LabelClasses?.FirstOrDefault();
        var classes = new List<CIMLabelClass>();
        foreach (var kind in ChartLayerKinds.TopFirst)
        {
            var label = template?.Clone() as CIMLabelClass ?? new CIMLabelClass();
            label.Name = ChartLayerKinds.DisplayName(kind);
            label.ExpressionEngine = LabelExpressionEngine.Arcade;
            label.Expression = CoverageTable.LabelExpression;
            label.WhereClause = CoverageTable.WhereLayer(kind);
            label.TextSymbol = SymbolFactory.Instance.ConstructTextSymbol(Outline, 9, "Tahoma", "Regular").MakeSymbolReference();
            label.MinimumScale = 0;

            // MaximumScale is the "in beyond" limit: no labels once the chart draws.
            var minScale = minScales.TryGetValue(kind, out var scale) ? scale : 0;
            label.MaximumScale = minScale;
            label.Visibility = minScale > 0;
            classes.Add(label);
        }

        cim.LabelClasses = [.. classes];
        cim.LabelVisibility = true;
        layer.SetDefinition(cim);
    }

    /// <summary>
    /// Reads the footprints of <paramref name="paths"/> as WGS-84 polygons,
    /// only the selected ones when <paramref name="selectedOnly"/>.
    /// </summary>
    /// <param name="layer">The Coverage layer.</param>
    /// <param name="paths">The dataset paths.</param>
    /// <param name="selectedOnly">Whether to read only the layer's selected features.</param>
    public static Dictionary<string, Polygon> ReadShapes(FeatureLayer layer, IReadOnlySet<string> paths, bool selectedOnly)
    {
        var shapes = new Dictionary<string, Polygon>(StringComparer.OrdinalIgnoreCase);
        using var featureClass = layer.GetFeatureClass();
        if (featureClass is null)
        {
            return shapes;
        }

        using var cursor = selectedOnly ? layer.GetSelection().Search(null, false) : featureClass.Search(null, false);
        while (cursor.MoveNext())
        {
            using var row = cursor.Current;
            if (row is Feature feature
                && Text(row, CoverageTable.PathField) is { } path
                && paths.Contains(path)
                && feature.GetShape() is Polygon { IsEmpty: false } polygon)
            {
                shapes[path] = (Polygon)GeometryEngine.Instance.Project(polygon, SpatialReferences.WGS84);
            }
        }

        return shapes;
    }

    private static List<FieldDescription> Fields()
    {
        FieldDescription Text(string name, string alias, int length) => new(name, FieldType.String) { AliasName = alias, Length = length };
        FieldDescription Date(string name, string alias) => new(name, FieldType.Date) { AliasName = alias };

        return
        [
            Text(CoverageTable.NameField, Strings.Coverage_FieldName, CoverageTable.TextLength),
            Text(CoverageTable.ProductField, Strings.Coverage_FieldProduct, CoverageTable.TextLength),
            Text(CoverageTable.EditionField, Strings.Coverage_FieldEdition, CoverageTable.TextLength),
            Text(CoverageTable.LayerField, Strings.Coverage_FieldLayer, CoverageTable.TextLength),
            Text(CoverageTable.PathField, Strings.Coverage_FieldPath, CoverageTable.PathLength),
            Date(CoverageTable.IssueDateField, Strings.Coverage_FieldIssueDate),
            Date(CoverageTable.FirstTimeField, Strings.Coverage_FieldFirstTime),
            Date(CoverageTable.LastTimeField, Strings.Coverage_FieldLastTime),
            Text(CoverageTable.SourceField, Strings.Coverage_FieldSource, CoverageTable.TextLength),
        ];
    }

    /// <summary>Gives a new Coverage layer its marker, outline-only symbol and description.</summary>
    private static void Style(FeatureLayer layer)
    {
        if (layer.GetDefinition() is not CIMFeatureLayer cim)
        {
            return;
        }

        var stroke = SymbolFactory.Instance.ConstructStroke(Outline, 1.5, SimpleLineStyle.Solid);
        var symbol = SymbolFactory.Instance.ConstructPolygonSymbol(ColorFactory.Instance.CreateRGBColor(0, 0, 0, 0), SimpleFillStyle.Null, stroke);
        cim.Renderer = new CIMSimpleRenderer { Symbol = symbol.MakeSymbolReference() };
        cim.CustomProperties = [new CIMStringMap { Key = ChartLayerMarker.PropertyKey, Value = ChartLayerMarker.ForCoverage().ToJson() }];
        cim.Description = Strings.Coverage_Description;
        layer.SetDefinition(cim);
    }

    private static Polygon? ToPolygon(DatasetFootprint footprint)
    {
        var builder = new PolygonBuilderEx(SpatialReferences.WGS84);
        foreach (var polygon in footprint.Polygons)
        {
            builder.AddPart(polygon.ExteriorRing.Select(p => new Coordinate2D(p.Longitude, p.Latitude)));
            foreach (var hole in polygon.Holes)
            {
                builder.AddPart(hole.Select(p => new Coordinate2D(p.Longitude, p.Latitude)));
            }
        }

        // Ring orientation in the data varies: let Pro decide which rings are holes.
        return GeometryEngine.Instance.SimplifyAsFeature(builder.ToGeometry()) as Polygon is { IsEmpty: false } simple ? simple : null;
    }

    private static void WriteInEditSession(
        FeatureLayer layer, FeatureClass featureClass, string shapeField, List<(DatasetFootprint Footprint, Polygon? Shape)> shapes, IReadOnlyList<string> delete)
    {
        var operation = new EditOperation
        {
            Name = Strings.Coverage_EditName,
            SelectNewFeatures = false,
            ShowProgressor = false,
        };

        var paths = delete.Concat(shapes.Select(s => s.Footprint.Dataset.Path)).ToList();
        var objectIds = new List<long>();
        foreach (var path in paths)
        {
            using var cursor = featureClass.Search(new QueryFilter { WhereClause = CoverageTable.WherePath(path) }, false);
            while (cursor.MoveNext())
            {
                using var row = cursor.Current;
                objectIds.Add(row.GetObjectID());
            }
        }

        if (objectIds.Count > 0)
        {
            operation.Delete(layer, objectIds);
        }

        foreach (var (footprint, shape) in shapes)
        {
            var attributes = CoverageTable.ValuesOf(footprint).ToDictionary(pair => pair.Key, pair => pair.Value ?? DBNull.Value);
            attributes[shapeField] = shape ?? (object)DBNull.Value;
            operation.Create(layer, attributes);
        }

        if (!operation.IsEmpty && !operation.Execute())
        {
            throw new InvalidOperationException($"Writing the footprints failed: {operation.ErrorMessage}");
        }
    }

    private static string? Text(Row row, string field)
    {
        var index = row.FindField(field);
        return index < 0 ? null : row[index]?.ToString();
    }
}
