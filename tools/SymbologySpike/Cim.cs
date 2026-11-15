using System.Globalization;
using System.Text.Json.Nodes;

namespace EncDotNet.SoundCharts.SymbologySpike;

/// <summary>
/// Small builders for the CIM (Cartographic Information Model) JSON that
/// ArcGIS Pro reads from a layer file. Sizes are in points, as CIM wants.
/// </summary>
internal static class Cim
{
    /// <summary>Points per millimetre: S-100 Part 9 sizes are in millimetres.</summary>
    public const double PointsPerMillimetre = 72.0 / 25.4;

    public static double Pt(double millimetres) => Math.Round(millimetres * PointsPerMillimetre, 4);

    public static JsonObject Color(string hex, double alpha = 1.0)
    {
        var (r, g, b) = ParseHex(hex);
        return new JsonObject
        {
            ["type"] = "CIMRGBColor",
            ["values"] = new JsonArray(r, g, b, Math.Round(alpha * 100, 2)),
        };
    }

    public static (int R, int G, int B) ParseHex(string hex)
    {
        var h = hex.TrimStart('#');
        if (h.Length < 6)
        {
            return (0, 0, 0);
        }

        return (
            int.Parse(h[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            int.Parse(h[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            int.Parse(h[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }

    public static JsonObject SolidStroke(string hex, double widthPt, double alpha = 1.0, JsonArray? effects = null)
    {
        var stroke = new JsonObject
        {
            ["type"] = "CIMSolidStroke",
            ["enable"] = true,
            ["capStyle"] = "Round",
            ["joinStyle"] = "Round",
            ["lineStyle3D"] = "Strip",
            ["miterLimit"] = 10,
            ["width"] = widthPt,
            ["color"] = Color(hex, alpha),
        };
        if (effects is not null)
        {
            stroke["effects"] = effects;
        }

        return stroke;
    }

    public static JsonObject SolidFill(string hex, double alpha = 1.0) => new()
    {
        ["type"] = "CIMSolidFill",
        ["enable"] = true,
        ["color"] = Color(hex, alpha),
    };

    public static JsonObject PolygonSymbol(params JsonObject[] layers) => new()
    {
        ["type"] = "CIMPolygonSymbol",
        ["symbolLayers"] = new JsonArray([.. layers]),
    };

    public static JsonObject LineSymbol(params JsonObject[] layers) => new()
    {
        ["type"] = "CIMLineSymbol",
        ["symbolLayers"] = new JsonArray([.. layers]),
    };

    public static JsonObject PointSymbol(params JsonObject[] layers) => new()
    {
        ["type"] = "CIMPointSymbol",
        ["symbolLayers"] = new JsonArray([.. layers]),
        ["haloSize"] = 1,
        ["scaleX"] = 1,
        ["angleAlignment"] = "Map",
    };

    public static JsonObject Reference(JsonObject symbol) => new()
    {
        ["type"] = "CIMSymbolReference",
        ["symbol"] = symbol,
    };

    /// <summary>A dash effect from S-100 dashes: (start, length) pairs in an interval, in millimetres.</summary>
    public static JsonObject? Dashes(IReadOnlyList<(double Start, double Length)> dashes, double interval)
    {
        if (dashes.Count == 0 || interval <= 0)
        {
            return null;
        }

        var ordered = dashes.OrderBy(d => d.Start).ToList();
        var template = new JsonArray();
        for (var i = 0; i < ordered.Count; i++)
        {
            var end = ordered[i].Start + ordered[i].Length;
            var next = i + 1 < ordered.Count ? ordered[i + 1].Start : interval + ordered[0].Start;
            template.Add(Pt(ordered[i].Length));
            template.Add(Pt(Math.Max(0, next - end)));
        }

        return new JsonObject
        {
            ["type"] = "CIMGeometricEffectDashes",
            ["dashTemplate"] = template,
            ["lineDashEnding"] = "NoConstraint",
            ["offsetAlongLine"] = Pt(ordered[0].Start),
        };
    }

    public static JsonObject AlongLinePlacement(double intervalMm, double positionMm, double rotation) => new()
    {
        ["type"] = "CIMMarkerPlacementAlongLineSameSize",
        ["angleToLine"] = true,
        ["placementTemplate"] = new JsonArray(Pt(intervalMm)),
        ["offsetAlongLine"] = Pt(positionMm),
        ["endings"] = "NoConstraint",
        ["placePerPart"] = true,
    };

    public static JsonObject InsidePolygonPlacement(double stepXmm, double stepYmm, bool shiftOddRows) => new()
    {
        ["type"] = "CIMMarkerPlacementInsidePolygon",
        ["gridType"] = "Fixed",
        ["stepX"] = Pt(stepXmm),
        ["stepY"] = Pt(stepYmm),
        ["shiftOddRows"] = shiftOddRows,
        ["clipping"] = "ClipAtBoundary",
        ["randomness"] = 100,
        ["seed"] = 0,
        ["gridAngle"] = 0,
        ["offsetX"] = 0,
        ["offsetY"] = 0,
    };

    public static JsonObject PolygonCenterPlacement() => new()
    {
        ["type"] = "CIMMarkerPlacementPolygonCenter",
        ["method"] = "OnPolygon",
        ["clipAtBoundary"] = false,
    };

    public static JsonObject Expression(string arcade) => new()
    {
        ["type"] = "CIMExpressionInfo",
        ["title"] = "Custom",
        ["expression"] = arcade,
        ["returnType"] = "Default",
    };
}
