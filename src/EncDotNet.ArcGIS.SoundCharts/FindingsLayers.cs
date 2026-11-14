using System.IO;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.Resources;
using EncDotNet.SoundCharts.RouteCheck;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;
using GeometryType = ArcGIS.Core.Geometry.GeometryType;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The route check findings layers (design brief §3.2, D14, D15): a
/// <b>Route check · {route}</b> group above the SoundCharts group, with
/// <b>Findings (points)</b> and <b>Findings (segments)</b> over feature
/// classes in the project's <c>SoundCharts.gdb</c>, symbolized by severity.
/// Checking the same route again replaces its features. Every method here
/// runs on the MCT.
/// </summary>
internal static class FindingsLayers
{
    // The viewer's tones: destructive red, warning amber, neutral blue-grey.
    private static readonly (string Severity, CIMColor Color)[] Severities =
    [
        (Strings.RouteCheck_GroupMustReview, ColorFactory.Instance.CreateRGBColor(214, 39, 40)),
        (Strings.RouteCheck_GroupCheck, ColorFactory.Instance.CreateRGBColor(242, 153, 0)),
        (Strings.RouteCheck_GroupInfo, ColorFactory.Instance.CreateRGBColor(84, 110, 140)),
    ];

    /// <summary>Writes a check's findings to the map, creating the group and layers on the first check of a route.</summary>
    /// <param name="map">The map.</param>
    /// <param name="routeName">The route's name.</param>
    /// <param name="features">The findings as features.</param>
    /// <param name="checkedAt">When the route was checked.</param>
    /// <returns>The group layer.</returns>
    public static GroupLayer Write(Map map, string routeName, IReadOnlyList<FindingFeature> features, DateTimeOffset checkedAt)
    {
        var path = Path.Combine(Project.Current.HomeFolderPath, FindingsSchema.GeodatabaseName);
        var connection = new FileGeodatabaseConnectionPath(new Uri(path));
        using var geodatabase = Directory.Exists(path) ? new Geodatabase(connection) : SchemaBuilder.CreateGeodatabase(connection);

        var pointsName = FindingsSchema.FeatureClassName(routeName, FindingShape.Point);
        var segmentsName = FindingsSchema.FeatureClassName(routeName, FindingShape.Segment);
        EnsureFeatureClass(geodatabase, pointsName, GeometryType.Point, routeName);
        EnsureFeatureClass(geodatabase, segmentsName, GeometryType.Polyline, routeName);

        var points = FindLayer(map, path, pointsName);
        var segments = FindLayer(map, path, segmentsName);
        var group = points?.Parent as GroupLayer ?? segments?.Parent as GroupLayer;
        if (group is null)
        {
            // Above the SoundCharts group: findings belong to a route, not the chart (§3.2).
            group = LayerFactory.Instance.CreateGroupLayer(map, 0, FindingsSchema.GroupName(routeName));
        }

        points ??= CreateLayer(geodatabase, pointsName, Strings.RouteCheck_FindingsLayerPoints, group, FindingShape.Point);
        segments ??= CreateLayer(geodatabase, segmentsName, Strings.RouteCheck_FindingsLayerSegments, group, FindingShape.Segment);

        WriteFeatures(points, [.. features.Where(f => f.Shape == FindingShape.Point)], routeName, checkedAt);
        WriteFeatures(segments, [.. features.Where(f => f.Shape == FindingShape.Segment)], routeName, checkedAt);
        return group;
    }

    private static void EnsureFeatureClass(Geodatabase geodatabase, string name, GeometryType geometryType, string routeName)
    {
        var existing = geodatabase.GetDefinitions<FeatureClassDefinition>()
            .FirstOrDefault(d => string.Equals(d.GetName(), name, StringComparison.OrdinalIgnoreCase));
        var builder = new SchemaBuilder(geodatabase);
        if (existing is not null)
        {
            var fields = existing.GetFields().Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (Fields().All(f => fields.Contains(f.Name)))
            {
                return;
            }

            // An older schema: start the route's findings afresh.
            builder.Delete(new FeatureClassDescription(existing));
        }

        builder.Create(new FeatureClassDescription(name, Fields(), new ShapeDescription(geometryType, SpatialReferences.WGS84))
        {
            AliasName = FindingsSchema.GroupName(routeName),
        });
        if (!builder.Build())
        {
            throw new InvalidOperationException($"Creating {name} in SoundCharts.gdb failed: {string.Join("; ", builder.ErrorMessages)}");
        }
    }

    private static FeatureLayer? FindLayer(Map map, string geodatabasePath, string featureClassName)
    {
        foreach (var layer in map.GetLayersAsFlattenedList().OfType<FeatureLayer>())
        {
            using var featureClass = layer.GetFeatureClass();
            if (featureClass is null || !string.Equals(featureClass.GetName(), featureClassName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using var datastore = featureClass.GetDatastore();
            var location = datastore.GetPath().LocalPath;
            if (string.Equals(Path.GetFullPath(location), Path.GetFullPath(geodatabasePath), StringComparison.OrdinalIgnoreCase))
            {
                return layer;
            }
        }

        return null;
    }

    private static FeatureLayer CreateLayer(Geodatabase geodatabase, string featureClassName, string layerName, GroupLayer group, FindingShape shape)
    {
        using var featureClass = geodatabase.OpenDataset<FeatureClass>(featureClassName);
        var parameters = new FeatureLayerCreationParams(featureClass)
        {
            Name = layerName,
            MapMemberPosition = shape == FindingShape.Point ? MapMemberPosition.AddToTop : MapMemberPosition.AddToBottom,
        };
        var layer = LayerFactory.Instance.CreateLayer<FeatureLayer>(parameters, group);
        Symbolize(layer, shape);
        return layer;
    }

    /// <summary>A unique value renderer on severity, MUST REVIEW drawn on top.</summary>
    private static void Symbolize(FeatureLayer layer, FindingShape shape)
    {
        if (layer.GetDefinition() is not CIMFeatureLayer cim)
        {
            return;
        }

        var classes = Severities.Select(s => new CIMUniqueValueClass
        {
            Label = s.Severity,
            Values = [new CIMUniqueValue { FieldValues = [s.Severity] }],
            Symbol = Symbol(shape, s.Color, s.Severity == Strings.RouteCheck_GroupMustReview).MakeSymbolReference(),
            Visible = true,
        }).ToArray();
        cim.Renderer = new CIMUniqueValueRenderer
        {
            Fields = [FindingsSchema.SeverityField],
            Groups = [new CIMUniqueValueGroup { Heading = FindingsSchema.SeverityField, Classes = classes }],
            UseDefaultSymbol = false,
        };

        cim.Description = Strings.RouteCheck_LayerDescription;
        layer.SetDefinition(cim);
        layer.SetSelectable(true);
    }

    private static CIMSymbol Symbol(FindingShape shape, CIMColor color, bool mustReview) => shape == FindingShape.Point
        ? SymbolFactory.Instance.ConstructPointSymbol(color, mustReview ? 12 : 10, mustReview ? SimpleMarkerStyle.Diamond : SimpleMarkerStyle.Circle)
        : SymbolFactory.Instance.ConstructLineSymbol(color, mustReview ? 4 : 3, SimpleLineStyle.Solid);

    private static void WriteFeatures(FeatureLayer layer, IReadOnlyList<FindingFeature> features, string routeName, DateTimeOffset checkedAt)
    {
        using var featureClass = layer.GetFeatureClass() ?? throw new InvalidOperationException($"{layer.Name} has no feature class.");
        using var geodatabase = featureClass.GetDatastore() as Geodatabase ?? throw new InvalidOperationException($"{layer.Name} isn't in a geodatabase.");
        var shapeField = featureClass.GetDefinition().GetShapeField();
        // INFO first, MUST REVIEW last, so MUST REVIEW draws on top.
        var rows = features
            .OrderByDescending(f => f.SeverityRank)
            .Select(f => (Values: Values(f, routeName, checkedAt), Shape: ToGeometry(f)))
            .Where(r => r.Shape is not null)
            .ToList();

        try
        {
            // Outside Pro's undo stack: findings follow the check, they're not the user's edits.
            geodatabase.ApplyEdits(() =>
            {
                featureClass.DeleteRows(new QueryFilter());
                foreach (var (values, shape) in rows)
                {
                    using var buffer = featureClass.CreateRowBuffer();
                    foreach (var (field, value) in values)
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
            WriteInEditSession(layer, featureClass, shapeField, rows);
        }
    }

    private static Dictionary<string, object?> Values(FindingFeature feature, string routeName, DateTimeOffset checkedAt)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            [FindingsSchema.SeverityField] = feature.Severity,
            [FindingsSchema.SeverityRankField] = (short)feature.SeverityRank,
            [FindingsSchema.RuleField] = feature.Rule,
            [FindingsSchema.CategoryField] = feature.Category,
            [FindingsSchema.TitleField] = feature.Title,
            [FindingsSchema.MessageField] = feature.Message,
            [FindingsSchema.FeaturesField] = feature.Features,
            [FindingsSchema.LegField] = feature.Leg is { } leg ? (short)Math.Min(leg, short.MaxValue) : null,
            [FindingsSchema.DistanceField] = feature.DistanceNm,
            [FindingsSchema.FindingIdField] = feature.FindingId,
            [FindingsSchema.RouteField] = routeName,
            [FindingsSchema.CheckedField] = checkedAt.UtcDateTime,
        };
        foreach (var (name, length) in FindingsSchema.TextFields)
        {
            values[name] = FindingsSchema.Fit(values[name] as string, length);
        }

        return values;
    }

    private static Geometry? ToGeometry(FindingFeature feature)
    {
        if (feature.Shape == FindingShape.Point)
        {
            var point = feature.Path[0];
            return MapPointBuilderEx.CreateMapPoint(point.Longitude, point.Latitude, SpatialReferences.WGS84);
        }

        var coordinates = feature.Path.Select(p => new Coordinate2D(p.Longitude, p.Latitude)).ToList();
        return coordinates.Count < 2 ? null : PolylineBuilderEx.CreatePolyline(coordinates, SpatialReferences.WGS84);
    }

    private static List<FieldDescription> Fields()
    {
        List<FieldDescription> fields = [.. FindingsSchema.TextFields.Select(f => new FieldDescription(f.Name, FieldType.String) { Length = f.Length })];
        fields.Add(new FieldDescription(FindingsSchema.SeverityRankField, FieldType.SmallInteger));
        fields.Add(new FieldDescription(FindingsSchema.LegField, FieldType.SmallInteger));
        fields.Add(new FieldDescription(FindingsSchema.DistanceField, FieldType.Double));
        fields.Add(new FieldDescription(FindingsSchema.CheckedField, FieldType.Date));
        return fields;
    }

    private static void WriteInEditSession(
        FeatureLayer layer, FeatureClass featureClass, string shapeField, List<(Dictionary<string, object?> Values, Geometry? Shape)> rows)
    {
        var operation = new EditOperation
        {
            Name = Strings.RouteCheck_EditName,
            SelectNewFeatures = false,
            ShowProgressor = false,
        };

        var objectIds = new List<long>();
        using (var cursor = featureClass.Search(null, false))
        {
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

        foreach (var (values, shape) in rows)
        {
            var attributes = values.ToDictionary(pair => pair.Key, pair => pair.Value ?? DBNull.Value);
            attributes[shapeField] = shape ?? (object)DBNull.Value;
            operation.Create(layer, attributes);
        }

        if (!operation.IsEmpty && !operation.Execute())
        {
            throw new InvalidOperationException($"Writing the findings failed: {operation.ErrorMessage}");
        }
    }
}
