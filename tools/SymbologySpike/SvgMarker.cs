using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace EncDotNet.SoundCharts.SymbologySpike;

/// <summary>
/// Converts an S-100 Part 9 SVG symbol (SVG Tiny: <c>path</c> with
/// M/L/Z, <c>circle</c>, <c>rect</c>, colours as palette classes) to a
/// <c>CIMVectorMarker</c> whose geometry is the symbol's own vectors.
/// </summary>
/// <remarks>
/// The marker's frame is centred on the symbol's pivot (0,0), so Pro
/// places the pivot on the feature without an anchor offset; frame units
/// are points, so the marker's size is the frame's height and stroke
/// widths need no scaling.
/// </remarks>
internal static partial class SvgMarker
{
    public sealed record Result(JsonObject Marker, int Elements, int Skipped, double SizePt);

    public static Result Convert(string svg, Func<string, string> resolveToken, double scale = 1.0)
    {
        var document = XDocument.Parse(svg);
        var graphics = new JsonArray();
        var extent = 0.0;
        var elements = 0;
        var skipped = 0;

        foreach (var element in document.Descendants())
        {
            var name = element.Name.LocalName;
            if (name is not ("path" or "circle" or "rect" or "ellipse" or "polygon" or "polyline" or "line" or "text"))
            {
                continue;
            }

            var classes = ((string?)element.Attribute("class") ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (classes.Contains("layout"))
            {
                continue;
            }

            var style = Style.From(element, classes, resolveToken);
            List<List<(double X, double Y)>> parts;
            bool closed;
            switch (name)
            {
                case "path":
                    (parts, closed) = ParsePath((string?)element.Attribute("d") ?? string.Empty);
                    break;
                case "circle":
                    parts = [Circle(Num(element, "cx"), Num(element, "cy"), Num(element, "r"))];
                    closed = true;
                    break;
                case "rect":
                    {
                        var x = Num(element, "x");
                        var y = Num(element, "y");
                        var w = Num(element, "width");
                        var h = Num(element, "height");
                        parts = [[(x, y), (x + w, y), (x + w, y + h), (x, y + h), (x, y)]];
                        closed = true;
                        break;
                    }

                default:
                    skipped++;
                    continue;
            }

            if (parts.Count == 0 || (style.Fill is null && style.Stroke is null))
            {
                skipped++;
                continue;
            }

            elements++;
            var half = style.Stroke is null ? 0 : style.StrokeWidthMm / 2;
            foreach (var part in parts)
            {
                foreach (var (x, y) in part)
                {
                    extent = Math.Max(extent, Math.Max(Math.Abs(x), Math.Abs(y)) + half);
                }
            }

            // Pt coordinates, y up.
            var coords = parts
                .Select(p => new JsonArray([.. p.Select(v => (JsonNode)new JsonArray(Cim.Pt(v.X * scale), Cim.Pt(-v.Y * scale)))]))
                .ToList();

            if (style.Fill is { } fill && closed)
            {
                var layers = new List<JsonObject>();
                if (style.Stroke is { } stroke)
                {
                    layers.Add(Cim.SolidStroke(stroke, Cim.Pt(style.StrokeWidthMm * scale), style.StrokeOpacity));
                }

                layers.Add(Cim.SolidFill(fill, style.FillOpacity));
                graphics.Add(Graphic(new JsonObject { ["rings"] = new JsonArray([.. coords]) }, Cim.PolygonSymbol([.. layers])));
            }
            else if (style.Stroke is { } stroke)
            {
                graphics.Add(Graphic(
                    new JsonObject { ["paths"] = new JsonArray([.. coords]) },
                    Cim.LineSymbol(Cim.SolidStroke(stroke, Cim.Pt(style.StrokeWidthMm * scale), style.StrokeOpacity))));
            }
        }

        var m = Math.Max(Cim.Pt(Math.Max(extent, 0.1) * scale), 0.5);

        // Pro fits the graphics' envelope, not the frame, to the marker's
        // size, so a transparent square the size of the frame keeps the
        // pivot at the centre and the symbol at its millimetre size.
        var square = new JsonArray(new JsonArray(
            new JsonArray(-m, -m), new JsonArray(-m, m), new JsonArray(m, m), new JsonArray(m, -m), new JsonArray(-m, -m)));
        graphics.Insert(0, Graphic(new JsonObject { ["rings"] = square }, Cim.PolygonSymbol(Cim.SolidFill("#FFFFFF", 0))));
        var marker = new JsonObject
        {
            ["type"] = "CIMVectorMarker",
            ["enable"] = true,
            ["anchorPointUnits"] = "Relative",
            ["dominantSizeAxis3D"] = "Y",
            ["size"] = Math.Round(2 * m, 4),
            ["billboardMode3D"] = "FaceNearPlane",
            ["frame"] = new JsonObject { ["xmin"] = -m, ["ymin"] = -m, ["xmax"] = m, ["ymax"] = m },
            ["markerGraphics"] = graphics,
            ["scaleSymbolsProportionally"] = true,
            ["respectFrame"] = true,
        };
        return new Result(marker, elements, skipped, 2 * m);
    }

    private static JsonObject Graphic(JsonObject geometry, JsonObject symbol) => new()
    {
        ["type"] = "CIMMarkerGraphic",
        ["geometry"] = geometry,
        ["symbol"] = symbol,
    };

    private static double Num(XElement element, string attribute) =>
        double.TryParse((string?)element.Attribute(attribute), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;

    private static List<(double X, double Y)> Circle(double cx, double cy, double r)
    {
        var segments = r < 0.3 ? 12 : r < 1 ? 24 : 48;
        var ring = new List<(double, double)>(segments + 1);
        for (var i = 0; i <= segments; i++)
        {
            var a = 2 * Math.PI * i / segments;
            ring.Add((cx + (r * Math.Cos(a)), cy - (r * Math.Sin(a))));
        }

        return ring;
    }

    /// <summary>Parses SVG path data with M, L, H, V and Z (absolute or relative).</summary>
    public static (List<List<(double X, double Y)>> Parts, bool Closed) ParsePath(string d)
    {
        var parts = new List<List<(double, double)>>();
        List<(double X, double Y)>? current = null;
        var closed = false;
        var x = 0.0;
        var y = 0.0;
        var command = 'M';
        var tokens = PathToken().Matches(d).Select(m => m.Value).ToList();
        var i = 0;
        double Next() => double.Parse(tokens[i++], NumberStyles.Float, CultureInfo.InvariantCulture);

        while (i < tokens.Count)
        {
            if (char.IsLetter(tokens[i][0]))
            {
                command = tokens[i][0];
                i++;
                if (command is 'Z' or 'z')
                {
                    if (current is { Count: > 0 })
                    {
                        current.Add(current[0]);
                        (x, y) = current[0];
                    }

                    closed = true;
                    continue;
                }
            }

            var relative = char.IsLower(command);
            switch (char.ToUpperInvariant(command))
            {
                case 'M':
                    x = Next() + (relative ? x : 0);
                    y = Next() + (relative ? y : 0);
                    current = [(x, y)];
                    parts.Add(current);
                    command = relative ? 'l' : 'L';
                    break;
                case 'L':
                    x = Next() + (relative ? x : 0);
                    y = Next() + (relative ? y : 0);
                    current?.Add((x, y));
                    break;
                case 'H':
                    x = Next() + (relative ? x : 0);
                    current?.Add((x, y));
                    break;
                case 'V':
                    y = Next() + (relative ? y : 0);
                    current?.Add((x, y));
                    break;
                default:
                    // Curves and arcs don't occur in the S-101 catalogue.
                    i++;
                    break;
            }
        }

        // A filled path is closed whether or not it says Z; SVG fills
        // open subpaths too.
        return (parts.Where(p => p.Count > 1).ToList(), closed || parts.Any(p => p.Count > 2));
    }

    [GeneratedRegex(@"[A-Za-z]|-?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?")]
    private static partial Regex PathToken();

    private sealed record Style(string? Fill, string? Stroke, double StrokeWidthMm, double FillOpacity, double StrokeOpacity)
    {
        public static Style From(XElement element, string[] classes, Func<string, string> resolveToken)
        {
            string? fill = null;
            string? stroke = null;
            var noFill = false;
            foreach (var c in classes)
            {
                if (c == "f0")
                {
                    noFill = true;
                }
                else if (c.Length == 6 && c[0] == 'f')
                {
                    fill = resolveToken(c[1..]);
                }
                else if (c.Length == 6 && c[0] == 's' && c != "symbol")
                {
                    stroke = resolveToken(c[1..]);
                }
            }

            // A palette-resolved SVG carries colours as attributes.
            fill = Attr(element, "fill") is { } f ? (f == "none" ? null : f) : fill;
            stroke = Attr(element, "stroke") is { } s ? (s == "none" ? null : s) : stroke;
            if (noFill)
            {
                fill = null;
            }

            var width = double.TryParse(Attr(element, "stroke-width"), NumberStyles.Float, CultureInfo.InvariantCulture, out var w) ? w : 0.32;
            var opacity = Opacity(element, "opacity");
            return new Style(fill, stroke, width, opacity * Opacity(element, "fill-opacity"), opacity * Opacity(element, "stroke-opacity"));
        }

        private static string? Attr(XElement element, string name)
        {
            if ((string?)element.Attribute(name) is { } value)
            {
                return value;
            }

            var style = (string?)element.Attribute("style");
            if (style is null)
            {
                return null;
            }

            foreach (var declaration in style.Split(';'))
            {
                var pair = declaration.Split(':', 2);
                if (pair.Length == 2 && pair[0].Trim() == name)
                {
                    return pair[1].Trim();
                }
            }

            return null;
        }

        private static double Opacity(XElement element, string name) =>
            double.TryParse(Attr(element, name), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 1.0;
    }
}
