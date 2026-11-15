using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using EncDotNet.S100.DataModel;
using EncDotNet.S100.Datasets.Pipelines.Portrayal;
using EncDotNet.S100.Pipelines.Vector;

namespace EncDotNet.SoundCharts.SymbologySpike;

/// <summary>
/// Turns a dataset's S-100 drawing instructions into five layers of rows,
/// each row one instruction with the feature's geometry, and the CIM
/// symbol each row's key draws with.
/// </summary>
internal sealed class PortrayalExport
{
    private static readonly string[] LayerOrder = ["AreaFills", "AreaSymbols", "Lines", "Points", "Text"];

    private readonly VectorPortrayalResult _portrayal;
    private readonly Dictionary<string, List<JsonObject>> _rows = LayerOrder.ToDictionary(l => l, _ => new List<JsonObject>());
    private readonly Dictionary<string, Dictionary<string, (JsonObject Symbol, int Priority)>> _symbols =
        LayerOrder.ToDictionary(l => l, _ => new Dictionary<string, (JsonObject, int)>());
    private readonly Dictionary<string, int> _counts = [];
    private readonly Dictionary<string, int> _approximations = [];
    private readonly Dictionary<string, SvgMarker.Result?> _markers = [];
    private int _ordinal;

    private readonly SizeProfile _sizes;

    public PortrayalExport(VectorPortrayalResult portrayal, SizeProfile sizes)
    {
        _portrayal = portrayal;
        _sizes = sizes;
    }

    public void Build()
    {
        // S-100 order: display plane, drawing priority, then areas, lines,
        // points, text (VectorPipeline's sort, kept by the sub-layers).
        var instructions = _portrayal.SubLayers
            .OrderBy(l => l.Plane)
            .ThenBy(l => l.WithinPlanePriority)
            .SelectMany(l => l.Instructions)
            .OrderBy(i => i.Plane)
            .ThenBy(i => i.DrawingPriority)
            .ThenBy(TypeOrder)
            .ToList();

        foreach (var instruction in instructions)
        {
            _ordinal++;
            var geometry = _portrayal.GeometryProvider.GetGeometry(instruction.FeatureReference);
            switch (instruction)
            {
                case AreaInstruction area when geometry is not null:
                    AddArea(area, geometry);
                    break;
                case LineInstruction line:
                    AddLine(line, geometry);
                    break;
                case PointInstruction point:
                    AddPoint(point, geometry);
                    break;
                case TextInstruction text:
                    AddText(text, geometry);
                    break;
                default:
                    Count("skipped.noGeometry");
                    break;
            }
        }
    }

    private static int TypeOrder(DrawingInstruction i) => i switch
    {
        AreaInstruction => 0,
        LineInstruction => 1,
        PointInstruction => 2,
        _ => 3,
    };

    private string Color(string? token) =>
        token is null ? "#000000" : token.StartsWith('#') ? token : _portrayal.Palette.Resolve(token);

    // ---- Areas ----

    private void AddArea(AreaInstruction area, FeatureGeometry geometry)
    {
        string key;
        if (area.AreaFillReference is { } fillName)
        {
            key = "pattern:" + fillName;
            Define("AreaFills", key, area.DrawingPriority, () => PatternFill(fillName));
            Count("area.pattern");
        }
        else
        {
            var alpha = 1.0 - (area.Transparency ?? 0);
            key = string.Create(CultureInfo.InvariantCulture, $"fill:{area.FillColor}:{alpha:0.##}");
            Define("AreaFills", key, area.DrawingPriority, () => Cim.PolygonSymbol(Cim.SolidFill(Color(area.FillColor), alpha)));
            Count("area.color");
        }

        if (Polygon(geometry) is { } shape)
        {
            AddRow("AreaFills", area, key, shape);
        }
    }

    private JsonObject PatternFill(string name)
    {
        var fill = _portrayal.AreaFillProvider?.Invoke(name);
        if (fill?.PatternSymbol is not { } symbolName || Marker(symbolName, _sizes.Pattern) is not { } marker)
        {
            Approximate("area.pattern.missing");
            return Cim.PolygonSymbol(Cim.SolidFill(fill?.Color is { } c ? Color(c) : "#FF00FF", 0.3));
        }

        // S-100 tiles along two lattice vectors; v1 is horizontal in every
        // S-101 fill. CIM grids are rectangular, with an optional half-step
        // shift of odd rows: exact only when v2.x is 0 or half of v1.x.
        var stepX = fill.V1X * _sizes.Pattern;
        var stepY = Math.Abs(fill.V2Y) * _sizes.Pattern;
        var shiftRatio = fill.V1X == 0 ? 0 : Math.Abs(fill.V2X) / fill.V1X % 1;
        var shift = shiftRatio > 0.01;
        if (shift && Math.Abs(shiftRatio - 0.5) > 0.01)
        {
            Approximate("area.pattern.obliqueLattice");
        }

        var layer = marker.Marker.DeepClone().AsObject();
        layer["markerPlacement"] = Cim.InsidePolygonPlacement(stepX, stepY, shift);
        var layers = new List<JsonObject> { layer };
        if (fill.Color is { } color)
        {
            layers.Add(Cim.SolidFill(Color(color)));
        }

        return Cim.PolygonSymbol([.. layers]);
    }

    // ---- Lines ----

    private void AddLine(LineInstruction line, FeatureGeometry? geometry)
    {
        JsonObject? shape = line.CoordinatesOverride is { Count: > 1 } over
            ? new JsonObject { ["type"] = "LineString", ["coordinates"] = Positions(over) }
            : geometry is null ? null : Polyline(geometry);
        if (shape is null)
        {
            Count("skipped.noGeometry");
            return;
        }

        string key;
        if (line.LineStyleReference is { } styleName && styleName != LineInstruction.SimpleLineStyleReference)
        {
            key = "style:" + styleName;
            Define("Lines", key, line.DrawingPriority, () => ComplexLine(styleName));
            Count("line.complex");
        }
        else
        {
            var dashes = line.Dashes ?? [];
            key = string.Create(
                CultureInfo.InvariantCulture,
                $"simple:{line.LineColor}:{line.LineWidth:0.###}:{string.Join(",", dashes.Select(d => $"{d.Offset:0.##}+{d.Length:0.##}"))}/{line.DashIntervalLength:0.##}");
            Define("Lines", key, line.DrawingPriority, () => SimpleLine(line));
            Count(dashes.Count > 0 ? "line.dashed" : "line.solid");
        }

        AddRow("Lines", line, key, shape);
    }

    private JsonObject SimpleLine(LineInstruction line)
    {
        var effects = line.Dashes is { Count: > 0 } dashes
            ? Cim.Dashes([.. dashes.Select(d => (d.Offset * _sizes.Line, d.Length * _sizes.Line))], DashInterval(dashes, line.DashIntervalLength) * _sizes.Line)
            : null;
        return Cim.LineSymbol(Cim.SolidStroke(
            Color(line.LineColor), Cim.Pt(_sizes.LineWidth(line.LineWidth)), 1.0, effects is null ? null : new JsonArray(effects)));
    }

    private static double DashInterval(IReadOnlyList<(double Offset, double Length)> dashes, double interval)
    {
        if (interval > dashes.Max(d => d.Offset + d.Length))
        {
            return interval;
        }

        // No interval: each dash is followed by a gap as long as itself (VectorSceneBuilder).
        var end = dashes.Max(d => d.Offset + d.Length);
        return end * 2;
    }

    private JsonObject ComplexLine(string name)
    {
        var style = _portrayal.LineStyleProvider?.Invoke(name);
        if (style is null)
        {
            Approximate("line.style.missing");
            return Cim.LineSymbol(Cim.SolidStroke("#FF00FF", 1));
        }

        // CIM symbol layers: first draws on top. A composite's components
        // draw in order, so the last component goes first.
        var layers = new List<JsonObject>();
        foreach (var component in (style.Components.Count > 0 ? style.Components : [style]).Reverse())
        {
            layers.AddRange(LineStyleLayers(component));
        }

        if (style.Components.Count > 0)
        {
            Count("line.style.composite");
        }

        return Cim.LineSymbol([.. layers]);
    }

    private IEnumerable<JsonObject> LineStyleLayers(LineStyle style)
    {
        foreach (var symbol in style.Symbols)
        {
            if (Marker(symbol.Reference, _sizes.LineSymbol) is not { } marker)
            {
                continue;
            }

            var layer = marker.Marker.DeepClone().AsObject();
            var placement = Cim.AlongLinePlacement(style.IntervalLength * _sizes.LineSymbol, symbol.Position * _sizes.LineSymbol, symbol.Rotation);
            layer["markerPlacement"] = placement;
            if (symbol.Rotation != 0)
            {
                // S-100 rotation is clockwise; CIM marker rotation is counter-clockwise.
                layer["rotation"] = -symbol.Rotation;
            }

            yield return layer;
        }

        if (style.HasPen)
        {
            JsonObject? dash = null;
            if (style.DashPattern is { Length: >= 2 } pattern)
            {
                var pairs = Enumerable.Range(0, pattern.Length / 2).Select(i => ((double)pattern[2 * i] * _sizes.Line, (double)pattern[(2 * i) + 1] * _sizes.Line)).ToList();
                dash = Cim.Dashes(pairs, style.IntervalLength * _sizes.Line);
            }

            yield return Cim.SolidStroke(Color(style.Color), Cim.Pt(_sizes.LineWidth(style.Width)), 1.0, dash is null ? null : new JsonArray(dash));
        }
    }

    // ---- Points ----

    private void AddPoint(PointInstruction point, FeatureGeometry? geometry)
    {
        if (point.SymbolReference is not { } name || Marker(name, _sizes.PointSymbol) is not { } marker)
        {
            Count("skipped.point.noSymbol");
            return;
        }

        var key = string.Create(CultureInfo.InvariantCulture, $"{name}@{point.SymbolScale:0.##}");
        var offset = (X: point.LocalOffsetX * _sizes.Line, Y: point.LocalOffsetY * _sizes.Line);
        if (offset != (0, 0))
        {
            key += string.Create(CultureInfo.InvariantCulture, $"+{offset.X:0.##},{offset.Y:0.##}");
        }

        var rotation = point.Rotation ?? 0;
        if (point.CoordinateOverride is null && geometry?.Type.ToString() == "Surface")
        {
            // Pro places it at the polygon's centre, as S-100 places a
            // symbol in the visible part of an area (closest CIM fit).
            Define("AreaSymbols", key, point.DrawingPriority, () =>
            {
                var layer = Scaled(marker, point.SymbolScale, offset);
                layer["markerPlacement"] = Cim.PolygonCenterPlacement();
                return Cim.PolygonSymbol(layer);
            });
            if (Polygon(geometry) is { } shape)
            {
                AddRow("AreaSymbols", point, key, shape);
            }

            Count("point.onArea");
            return;
        }

        Define("Points", key, point.DrawingPriority, () => Cim.PointSymbol(Scaled(marker, point.SymbolScale, offset)));
        foreach (var at in PointsFor(point.CoordinateOverride, geometry, point.LinePlacementPosition))
        {
            var row = AddRow("Points", point, key, new JsonObject { ["type"] = "Point", ["coordinates"] = Position(at) });
            row["Rotation"] = rotation;
            row["RotationCrs"] = point.RotationCrs.ToString();
        }

        Count(point.CoordinateOverride is not null ? "point.override" : "point." + geometry?.Type.ToString());
    }

    private static JsonObject Scaled(SvgMarker.Result marker, double scale, (double X, double Y) offsetMm)
    {
        var layer = marker.Marker.DeepClone().AsObject();
        if (scale != 1)
        {
            layer["size"] = Math.Round(marker.SizePt * scale, 4);
        }

        if (offsetMm != (0, 0))
        {
            layer["offsetX"] = Cim.Pt(offsetMm.X);
            layer["offsetY"] = -Cim.Pt(offsetMm.Y);
        }

        return layer;
    }

    // ---- Text ----

    private void AddText(TextInstruction text, FeatureGeometry? geometry)
    {
        var points = PointsFor(text.CoordinateOverride, geometry, text.LinePlacementPosition).Take(1).ToList();
        if (points.Count == 0)
        {
            Count("skipped.text.noGeometry");
            return;
        }

        var key = string.Create(
            CultureInfo.InvariantCulture,
            $"{text.FontColor}:{text.FontSize:0.#}:{text.FontWeight}:{text.HorizontalAlignment}:{text.VerticalAlignment}:{text.OffsetX ?? 0:0.##},{text.OffsetY ?? 0:0.##}:{text.HaloColor}");
        Define("Text", key, text.DrawingPriority, () => TextMarker(text));
        var row = AddRow("Text", text, key, new JsonObject { ["type"] = "Point", ["coordinates"] = Position(points[0]) });
        row["Txt"] = text.Text;
        row["Rotation"] = text.Rotation ?? 0;
        Count("text." + (geometry?.Type.ToString() ?? "override"));
    }

    /// <summary>
    /// A point symbol whose one graphic is text: the string comes from the
    /// row's <c>Txt</c> through a primitive override, so there's no label
    /// engine and no conflict resolution, as an ECDIS draws chart text.
    /// </summary>
    private JsonObject TextMarker(TextInstruction text)
    {
        var textSymbol = new JsonObject
        {
            ["type"] = "CIMTextSymbol",
            ["fontFamilyName"] = "Arial",
            ["fontStyleName"] = (text.FontWeight == TextFontWeight.Bold ? "Bold" : "Regular"),
            ["height"] = text.FontSize * _sizes.Text,
            ["horizontalAlignment"] = text.HorizontalAlignment switch
            {
                TextHorizontalAlignment.Start => "Left",
                TextHorizontalAlignment.End => "Right",
                _ => "Center",
            },
            ["verticalAlignment"] = text.VerticalAlignment switch
            {
                TextVerticalAlignment.Top => "Top",
                TextVerticalAlignment.Bottom => "Bottom",
                _ => "Center",
            },
            ["symbol"] = Cim.PolygonSymbol(Cim.SolidFill(Color(text.FontColor))),
        };
        if (text.HaloColor is { } halo && text.HaloWidth > 0)
        {
            textSymbol["haloSize"] = Cim.Pt(text.HaloWidth);
            textSymbol["haloSymbol"] = Cim.PolygonSymbol(Cim.SolidFill(Color(halo)));
        }

        var x = Cim.Pt((text.OffsetX ?? 0) * _sizes.Line);
        var y = Cim.Pt((text.OffsetY ?? 0) * _sizes.Line);
        var marker = new JsonObject
        {
            ["type"] = "CIMVectorMarker",
            ["enable"] = true,
            ["anchorPointUnits"] = "Relative",
            ["dominantSizeAxis3D"] = "Y",
            ["size"] = 10,
            ["billboardMode3D"] = "FaceNearPlane",
            ["frame"] = new JsonObject { ["xmin"] = -5, ["ymin"] = -5, ["xmax"] = 5, ["ymax"] = 5 },
            ["markerGraphics"] = new JsonArray(new JsonObject
            {
                ["type"] = "CIMMarkerGraphic",
                ["primitiveName"] = "txt",
                ["geometry"] = new JsonObject { ["x"] = x, ["y"] = -y },
                ["symbol"] = textSymbol,
                ["textString"] = "Text",
            }),
            ["scaleSymbolsProportionally"] = true,
            ["respectFrame"] = false,
        };
        return Cim.PointSymbol(marker);
    }

    // ---- Shared ----

    private SvgMarker.Result? Marker(string name, double scale)
    {
        var cacheKey = string.Create(CultureInfo.InvariantCulture, $"{name}@{scale}");
        if (_markers.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        SvgMarker.Result? result = null;
        if (_portrayal.SymbolProvider?.Invoke(name) is { } svg)
        {
            result = SvgMarker.Convert(svg, Color, scale);
            if (result.Skipped > 0)
            {
                Approximate("symbol.elementsSkipped");
            }
        }
        else
        {
            Approximate("symbol.missing");
        }

        _markers[cacheKey] = result;
        return result;
    }

    // A class per symbol and drawing priority: Pro draws a unique value
    // renderer class by class, so the class order is the drawing order.
    private static string ClassKey(string key, int priority) =>
        string.Create(CultureInfo.InvariantCulture, $"{key}|{priority}");

    private void Define(string layer, string key, int priority, Func<JsonObject> symbol)
    {
        var classKey = ClassKey(key, priority);
        if (!_symbols[layer].ContainsKey(classKey))
        {
            _symbols[layer][classKey] = (symbol(), priority);
        }
    }

    private JsonObject AddRow(string layer, DrawingInstruction instruction, string key, JsonObject geometry)
    {
        var properties = new JsonObject
        {
            ["Ord"] = _ordinal,
            ["FeatureRef"] = instruction.FeatureReference,
            ["SymKey"] = ClassKey(key, instruction.DrawingPriority),
            ["Priority"] = instruction.DrawingPriority,
            ["ViewingGroup"] = instruction.ViewingGroup,
            ["Plane"] = instruction.Plane.ToString(),
            ["Scamin"] = instruction.ScaleMinimum,
        };
        _rows[layer].Add(new JsonObject
        {
            ["type"] = "Feature",
            ["geometry"] = geometry,
            ["properties"] = properties,
        });
        return properties;
    }

    private void Count(string what) => _counts[what] = _counts.GetValueOrDefault(what) + 1;

    private void Approximate(string what) => _approximations[what] = _approximations.GetValueOrDefault(what) + 1;

    private static JsonArray Position(GeoPosition p) => new(Math.Round(p.Longitude, 8), Math.Round(p.Latitude, 8));

    private static JsonArray Positions(IEnumerable<GeoPosition> ps) => new([.. ps.Select(p => (JsonNode)Position(p))]);

    private static JsonArray Ring(IReadOnlyList<GeoPosition> ring)
    {
        var positions = Positions(ring);
        if (ring.Count > 0 && ring[0] != ring[^1])
        {
            positions.Add(Position(ring[0]));
        }

        return positions;
    }

    private static JsonObject? Polygon(FeatureGeometry g)
    {
        if (g.Parts.Count > 0)
        {
            var polygons = new JsonArray();
            for (var i = 0; i < g.Parts.Count; i++)
            {
                var rings = new JsonArray(Ring(g.Parts[i]));
                if (i < g.PartInteriorRings.Count)
                {
                    foreach (var hole in g.PartInteriorRings[i])
                    {
                        rings.Add(Ring(hole));
                    }
                }

                polygons.Add(rings);
            }

            return new JsonObject { ["type"] = "MultiPolygon", ["coordinates"] = polygons };
        }

        if (g.Coordinates.Count < 3)
        {
            return null;
        }

        var all = new JsonArray(Ring(g.Coordinates));
        foreach (var hole in g.InteriorRings)
        {
            all.Add(Ring(hole));
        }

        return new JsonObject { ["type"] = "Polygon", ["coordinates"] = all };
    }

    private static JsonObject? Polyline(FeatureGeometry g)
    {
        var lines = new JsonArray();
        var surface = g.Type.ToString() == "Surface";
        var parts = g.Parts.Count > 0 ? g.Parts : [g.Coordinates];
        foreach (var part in parts)
        {
            if (part.Count > 1)
            {
                lines.Add(surface ? Ring(part) : Positions(part));
            }
        }

        if (surface)
        {
            foreach (var hole in g.InteriorRings.Concat(g.PartInteriorRings.SelectMany(h => h)))
            {
                lines.Add(Ring(hole));
            }
        }

        return lines.Count == 0 ? null : new JsonObject { ["type"] = "MultiLineString", ["coordinates"] = lines };
    }

    private static IEnumerable<GeoPosition> PointsFor(GeoPosition? over, FeatureGeometry? g, double? linePosition)
    {
        if (over is { } at)
        {
            yield return at;
            yield break;
        }

        if (g is null || g.Coordinates.Count == 0)
        {
            yield break;
        }

        switch (g.Type.ToString())
        {
            case "Curve":
                yield return AlongLine(g.Parts.Count > 0 ? g.Parts[0] : g.Coordinates, linePosition ?? 0.5);
                break;
            case "Surface":
                yield return Centroid(g.Parts.Count > 0 ? g.Parts[0] : g.Coordinates);
                break;
            default:
                foreach (var p in g.Coordinates)
                {
                    yield return p;
                }

                break;
        }
    }

    private static GeoPosition AlongLine(IReadOnlyList<GeoPosition> line, double fraction)
    {
        var lengths = new double[line.Count];
        for (var i = 1; i < line.Count; i++)
        {
            lengths[i] = lengths[i - 1] + Distance(line[i - 1], line[i]);
        }

        var target = lengths[^1] * Math.Clamp(fraction, 0, 1);
        for (var i = 1; i < line.Count; i++)
        {
            if (lengths[i] >= target)
            {
                var span = lengths[i] - lengths[i - 1];
                var t = span == 0 ? 0 : (target - lengths[i - 1]) / span;
                return new GeoPosition(
                    line[i - 1].Latitude + (t * (line[i].Latitude - line[i - 1].Latitude)),
                    line[i - 1].Longitude + (t * (line[i].Longitude - line[i - 1].Longitude)));
            }
        }

        return line[^1];
    }

    private static double Distance(GeoPosition a, GeoPosition b)
    {
        var dx = (b.Longitude - a.Longitude) * Math.Cos(a.Latitude * Math.PI / 180);
        var dy = b.Latitude - a.Latitude;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static GeoPosition Centroid(IReadOnlyList<GeoPosition> ring)
    {
        double area = 0, cx = 0, cy = 0;
        for (var i = 0; i < ring.Count; i++)
        {
            var a = ring[i];
            var b = ring[(i + 1) % ring.Count];
            var cross = (a.Longitude * b.Latitude) - (b.Longitude * a.Latitude);
            area += cross;
            cx += (a.Longitude + b.Longitude) * cross;
            cy += (a.Latitude + b.Latitude) * cross;
        }

        if (Math.Abs(area) < 1e-15)
        {
            return ring[0];
        }

        return new GeoPosition(cy / (3 * area), cx / (3 * area));
    }

    // ---- Output ----

    public void Write(string folder, string geodatabase)
    {
        foreach (var layer in LayerOrder)
        {
            var collection = new JsonObject
            {
                ["type"] = "FeatureCollection",
                ["features"] = new JsonArray([.. _rows[layer].Select(r => (JsonNode)r.DeepClone())]),
            };
            File.WriteAllText(Path.Combine(folder, layer + ".geojson"), collection.ToJsonString());
            if (_rows[layer].Count > 0)
            {
                File.WriteAllText(
                    Path.Combine(folder, layer + ".lyrx"),
                    LayerFile.Build(layer, geodatabase, _symbols[layer], layer == "Points" || layer == "Text", Scamins(layer)).ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            }
        }
    }

    /// <summary>Converts every SVG file of a portrayal catalogue, to measure its coverage.</summary>
    public static JsonObject ConvertAll(VectorPortrayalResult portrayal, IEnumerable<string> files)
    {
        int converted = 0, missing = 0, elements = 0, skipped = 0, failed = 0;
        foreach (var file in files)
        {
            var svg = File.ReadAllText(file);
            if (svg.Length == 0)
            {
                missing++;
                continue;
            }

            try
            {
                var result = SvgMarker.Convert(svg, portrayal.Palette.Resolve);
                converted++;
                elements += result.Elements;
                skipped += result.Skipped;
            }
            catch (System.Xml.XmlException)
            {
                failed++;
            }
        }

        return new JsonObject
        {
            ["converted"] = converted,
            ["missing"] = missing,
            ["failed"] = failed,
            ["elements"] = elements,
            ["elementsSkipped"] = skipped,
        };
    }

    private List<double> Scamins(string layer) =>
        [.. _rows[layer]
            .Select(r => r["properties"]!["Scamin"]?.GetValue<double>())
            .OfType<double>()
            .Distinct()
            .Order()];

    public JsonObject Summary()
    {
        var rows = new JsonObject();
        var classes = new JsonObject();
        foreach (var layer in LayerOrder)
        {
            rows[layer] = _rows[layer].Count;
            classes[layer] = _symbols[layer].Count;
        }

        var priorities = new JsonObject();
        foreach (var layer in LayerOrder)
        {
            var values = _rows[layer].Select(r => (int)r["properties"]!["Priority"]!).ToList();
            priorities[layer] = values.Count == 0 ? null : new JsonArray(values.Min(), values.Max());
        }

        return new JsonObject
        {
            ["rows"] = rows,
            ["classes"] = classes,
            ["priorityRange"] = priorities,
            ["orderInversions"] = OrderInversions(),
            ["counts"] = ToJson(_counts),
            ["approximations"] = ToJson(_approximations),
            ["symbols"] = new JsonObject
            {
                ["converted"] = _markers.Count(m => m.Value is not null),
                ["missing"] = _markers.Count(m => m.Value is null),
                ["elements"] = _markers.Values.Sum(m => m?.Elements ?? 0),
                ["elementsSkipped"] = _markers.Values.Sum(m => m?.Skipped ?? 0),
            },
            ["scaminValues"] = new JsonArray([.. _rows.Values.SelectMany(r => r)
                .Select(r => r["properties"]!["Scamin"]?.GetValue<double>())
                .Where(s => s is not null)
                .Distinct()
                .Order()
                .Select(s => (JsonNode)s!.Value)]),
        };
    }

    /// <summary>
    /// Rows that Pro's fixed layer stack draws out of S-100 order: a row in
    /// a higher layer whose (plane, priority) is below a row in a lower one.
    /// </summary>
    private JsonObject OrderInversions()
    {
        var result = new JsonObject();
        for (var upper = 1; upper < LayerOrder.Length; upper++)
        {
            var maxBelow = LayerOrder.Take(upper)
                .SelectMany(l => _rows[l])
                .Select(r => (int)r["properties"]!["Priority"]!)
                .DefaultIfEmpty(int.MinValue)
                .Max();
            var count = _rows[LayerOrder[upper]].Count(r => (int)r["properties"]!["Priority"]! < maxBelow);
            result[LayerOrder[upper]] = count;
        }

        return result;
    }

    private static JsonObject ToJson(Dictionary<string, int> counts) =>
        new([.. counts.OrderBy(c => c.Key, StringComparer.Ordinal).Select(c => KeyValuePair.Create(c.Key, (JsonNode?)c.Value))]);
}
