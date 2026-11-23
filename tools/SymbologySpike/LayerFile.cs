using System.Text.Json.Nodes;

namespace EncDotNet.SoundCharts.SymbologySpike;

/// <summary>
/// Writes a <c>.lyrx</c> for one instruction layer: a feature layer over
/// the file geodatabase's feature class with a unique value renderer on
/// <c>SymKey</c>, one class per symbol, in S-100 drawing priority order.
/// </summary>
internal static class LayerFile
{
    public static JsonObject Build(
        string name,
        string geodatabase,
        IReadOnlyDictionary<string, (JsonObject Symbol, int Priority)> symbols,
        bool rotates,
        IReadOnlyList<double> scamins)
    {
        var classes = new JsonArray();
        // Pro draws a unique value renderer's classes in order, the first
        // class at the bottom (checked with arcpy): lowest priority first.
        foreach (var (key, (symbol, _)) in symbols.OrderBy(s => s.Value.Priority).ThenBy(s => s.Key, StringComparer.Ordinal))
        {
            var reference = Cim.Reference(symbol);
            if (name == "Text")
            {
                reference["primitiveOverrides"] = new JsonArray(new JsonObject
                {
                    ["type"] = "CIMPrimitiveOverride",
                    ["primitiveName"] = "txt",
                    ["propertyName"] = "TextString",
                    ["valueExpressionInfo"] = Cim.Expression("$feature.Txt"),
                });
            }

            classes.Add(new JsonObject
            {
                ["type"] = "CIMUniqueValueClass",
                ["label"] = key,
                ["patch"] = "Default",
                ["symbol"] = reference,
                ["values"] = new JsonArray(new JsonObject
                {
                    ["type"] = "CIMUniqueValue",
                    ["fieldValues"] = new JsonArray(key),
                }),
                ["visible"] = true,
            });
        }

        var renderer = new JsonObject
        {
            ["type"] = "CIMUniqueValueRenderer",
            ["fields"] = new JsonArray("SymKey"),
            ["groups"] = new JsonArray(new JsonObject
            {
                ["type"] = "CIMUniqueValueGroup",
                ["classes"] = classes,
                ["heading"] = "SymKey",
            }),
            ["useDefaultSymbol"] = false,
            ["polygonSymbolColorTarget"] = "Fill",
        };
        if (rotates)
        {
            renderer["visualVariables"] = new JsonArray(new JsonObject
            {
                ["type"] = "CIMRotationVisualVariable",
                ["normalToSurface"] = false,
                ["visualVariableInfoX"] = new JsonObject { ["type"] = "CIMVisualVariableInfo", ["visualVariableInfoType"] = "None" },
                ["visualVariableInfoY"] = new JsonObject { ["type"] = "CIMVisualVariableInfo", ["visualVariableInfoType"] = "None" },
                ["visualVariableInfoZ"] = new JsonObject
                {
                    ["type"] = "CIMVisualVariableInfo",
                    ["visualVariableInfoType"] = "Expression",
                    ["valueExpressionInfo"] = Cim.Expression("$feature.Rotation"),
                },
                ["rotationTypeZ"] = "Geographic",
            });
        }

        var uri = $"CIMPATH=spike/{name.ToLowerInvariant()}.json";
        var layer = new JsonObject
        {
            ["type"] = "CIMFeatureLayer",
            ["name"] = name,
            ["uRI"] = uri,
            ["layerType"] = "Operational",
            ["showLegends"] = true,
            ["visibility"] = true,
            ["displayCacheType"] = "Permanent",
            ["maxDisplayCacheAge"] = 5,
            ["showPopups"] = true,
            ["autoGenerateFeatureTemplates"] = false,
            ["featureTable"] = new JsonObject
            {
                ["type"] = "CIMFeatureTable",
                ["displayField"] = "SymKey",
                ["editable"] = false,
                ["dataConnection"] = new JsonObject
                {
                    ["type"] = "CIMStandardDataConnection",
                    ["workspaceConnectionString"] = "DATABASE=" + geodatabase,
                    ["workspaceFactory"] = "FileGDB",
                    ["dataset"] = name,
                    ["datasetType"] = "esriDTFeatureClass",
                },
                ["studyAreaSpatialRel"] = "esriSpatialRelUndefined",
                ["searchOrder"] = "esriSearchOrderSpatial",
            },
            ["htmlPopupEnabled"] = true,
            ["selectable"] = true,
            ["featureCacheType"] = "Session",
            ["renderer"] = renderer,
            ["scaleSymbols"] = false,
            ["snappable"] = true,
            ["symbolLayerDrawing"] = new JsonObject
            {
                ["type"] = "CIMSymbolLayerDrawing",
                ["useSymbolLayerDrawing"] = false,
            },
        };

        if (scamins.Count > 0)
        {
            layer["displayFilters"] = ScaminFilters(scamins);
            layer["enableDisplayFilters"] = true;
            layer["displayFiltersType"] = "ByScale";
        }

        return new JsonObject
        {
            ["type"] = "CIMLayerDocument",
            ["version"] = "3.7.0",
            ["build"] = 0,
            ["layers"] = new JsonArray(uri),
            ["layerDefinitions"] = new JsonArray(layer),
        };
    }

    /// <summary>
    /// SCAMIN (S-100 Part 9 scale minimum) as scale-based display filters:
    /// between two SCAMIN values the filter keeps the rows whose SCAMIN is
    /// at least the larger one. Display filters change what's drawn without
    /// re-querying, so zooming costs no re-portrayal.
    /// </summary>
    private static JsonArray ScaminFilters(IReadOnlyList<double> scamins)
    {
        var filters = new JsonArray();
        var bands = new List<double> { 0 };
        bands.AddRange(scamins.Order());
        for (var i = 0; i < bands.Count; i++)
        {
            var maxScale = bands[i];
            var minScale = i + 1 < bands.Count ? bands[i + 1] : 0;
            var where = i == 0 ? "1=1" : FormattableString.Invariant($"Scamin IS NULL OR Scamin > {maxScale}");
            var filter = new JsonObject
            {
                ["type"] = "CIMDisplayFilter",
                ["name"] = FormattableString.Invariant($"SCAMIN {maxScale}"),
                ["whereClause"] = where,
                ["maxScale"] = maxScale,
            };
            if (minScale > 0)
            {
                filter["minScale"] = minScale;
            }

            filters.Add(filter);
        }

        return filters;
    }
}
