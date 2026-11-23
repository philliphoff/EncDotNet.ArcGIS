using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>
/// Writes SoundCharts layers as ArcGIS Pro CIM JSON: a
/// <c>CIMTiledServiceLayer</c> with a <c>CIMWMTSServiceConnection</c> to the
/// managed chart server for each chart layer, in a <c>CIMGroupLayer</c>
/// named <b>SoundCharts · {source}</b>, as a <c>.lyrx</c> layer document
/// (design brief §3, D10–D16).
/// </summary>
/// <remarks>
/// <para>
/// Pro can't add a WMTS layer from a path without a request (arcpy's
/// <c>addDataFromPath</c> fails), so the add-in authors the layers. The CIM
/// is the one step 0 found Pro accepts: the capabilities URL in a
/// <c>CIMInternetServerConnection</c>, layer <c>s100</c>, the palette as the
/// style, <c>WebMercatorQuad</c> and PNG.
/// </para>
/// <para>
/// The JSON is built without the Pro API, so it's tested without Pro; in Pro
/// the add-in reads it with <c>CIMLayerDocument.FromJson</c>.
/// </para>
/// </remarks>
public static class ChartLayerDocument
{
    /// <summary>The CIM version the documents declare: the Pro release the add-in targets.</summary>
    public const string CimVersion = "3.7.0";

    private const string UriPrefix = "CIMPATH=soundcharts/";

    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,

        // A readable .lyrx: "·" and the marker's quotes as themselves, not escaped. It's a file, not HTML.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Gets the CIM URI of the group in a document.</summary>
    public static string GroupUri => UriPrefix + "soundcharts.json";

    /// <summary>Gets the CIM URI of a chart layer of <paramref name="kind"/> in a document.</summary>
    /// <param name="kind">The layer's kind.</param>
    public static string LayerUri(ChartLayerKind kind) => UriPrefix + ChartLayerKinds.Id(kind) + ".json";

    /// <summary>
    /// Gets a layer document holding the group, named
    /// <paramref name="groupName"/>, and its chart layers, top first.
    /// </summary>
    /// <param name="groupName">The group's name, <b>SoundCharts · {source}</b>.</param>
    /// <param name="groupMarker">The group's marker.</param>
    /// <param name="layers">The chart layers.</param>
    /// <returns>The <c>.lyrx</c> JSON.</returns>
    public static string ForGroup(string groupName, ChartLayerMarker groupMarker, IReadOnlyCollection<ChartLayerDefinition> layers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
        ArgumentNullException.ThrowIfNull(groupMarker);
        ArgumentNullException.ThrowIfNull(layers);
        if (!groupMarker.IsGroup)
        {
            throw new ArgumentException("The group needs a group marker.", nameof(groupMarker));
        }

        var ordered = Order(layers);
        var datasets = ordered.SelectMany(l => l.Marker.Datasets).ToList();
        var group = new JsonObject
        {
            ["type"] = "CIMGroupLayer",
            ["name"] = groupName,
            ["uRI"] = GroupUri,
            ["description"] = ChartCredits.Description(datasets),
            ["layerType"] = "Operational",
            ["showLegends"] = true,
            ["visibility"] = true,
            ["expanded"] = true,
            ["customProperties"] = Marker(groupMarker),
            ["layers"] = new JsonArray([.. ordered.Select(l => JsonValue.Create(LayerUri(l.Kind)))]),
        };

        var definitions = new JsonArray { group };
        foreach (var layer in ordered)
        {
            definitions.Add(Layer(layer));
        }

        return Document([GroupUri], definitions);
    }

    /// <summary>Gets a layer document holding one chart layer, to add it to an existing group.</summary>
    /// <param name="layer">The chart layer.</param>
    /// <returns>The <c>.lyrx</c> JSON.</returns>
    public static string ForLayer(ChartLayerDefinition layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        return Document([LayerUri(layer.Kind)], [Layer(layer)]);
    }

    /// <summary>Gets the chart layer's <c>CIMTiledServiceLayer</c> definition as JSON.</summary>
    /// <param name="layer">The chart layer.</param>
    public static JsonObject Layer(ChartLayerDefinition layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        var datasets = layer.Marker.Datasets;
        return new JsonObject
        {
            ["type"] = "CIMTiledServiceLayer",
            ["name"] = layer.Name,
            ["uRI"] = LayerUri(layer.Kind),
            ["description"] = ChartCredits.Description(datasets),
            ["attribution"] = ChartCredits.Attribution(datasets, layer.ServerVersion),
            ["layerType"] = "Operational",
            ["minScale"] = Math.Round(Math.Max(0, layer.MinScale), 3),
            ["showLegends"] = true,
            ["visibility"] = true,
            ["displayCacheType"] = "InSession",
            ["maxDisplayCacheAge"] = 5,
            ["customProperties"] = Marker(layer.Marker),
            ["serviceConnection"] = Connection(layer.CapabilitiesUri, layer.Style),
        };
    }

    /// <summary>Gets the <c>CIMWMTSServiceConnection</c> to a chart server as JSON.</summary>
    /// <param name="capabilitiesUri">The server's WMTS capabilities URL.</param>
    /// <param name="style">The WMTS style (palette).</param>
    public static JsonObject Connection(Uri capabilitiesUri, string style)
    {
        ArgumentNullException.ThrowIfNull(capabilitiesUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(style);
        return new JsonObject
        {
            ["type"] = "CIMWMTSServiceConnection",
            ["serverConnection"] = new JsonObject
            {
                ["type"] = "CIMInternetServerConnection",
                ["anonymous"] = true,
                ["hideUserProperty"] = true,
                ["url"] = capabilitiesUri.AbsoluteUri,
            },
            ["layerName"] = ServerEndpoint.WmtsLayer,
            ["style"] = style,
            ["tileMatrixSet"] = ServerEndpoint.WmtsTileMatrixSet,
            ["imageFormat"] = "image/png",
        };
    }

    /// <summary>
    /// Gets the scale denominator Pro shows at Web Mercator zoom
    /// <paramref name="zoom"/>: Esri's 96 dpi tiling scheme, 591,657,527.591555
    /// at zoom 0, halving with each level.
    /// </summary>
    /// <param name="zoom">The zoom level, 0 to 30.</param>
    public static double ScaleOfZoom(int zoom)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(zoom);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(zoom, 30);
        return 591_657_527.591555 / Math.Pow(2, zoom);
    }

    private static List<ChartLayerDefinition> Order(IEnumerable<ChartLayerDefinition> layers) =>
        [.. layers.OrderBy(l => ChartLayerKinds.TopFirst.ToList().IndexOf(l.Kind))];

    private static JsonArray Marker(ChartLayerMarker marker) =>
    [
        new JsonObject
        {
            ["type"] = "CIMStringMap",
            ["key"] = ChartLayerMarker.PropertyKey,
            ["value"] = marker.ToJson(),
        },
    ];

    private static string Document(IEnumerable<string> topLevel, JsonArray definitions)
    {
        var document = new JsonObject
        {
            ["type"] = "CIMLayerDocument",
            ["version"] = CimVersion,
            ["build"] = 0,
            ["layers"] = new JsonArray([.. topLevel.Select(uri => JsonValue.Create(uri))]),
            ["layerDefinitions"] = definitions,
        };
        return document.ToJsonString(Indented).ReplaceLineEndings("\n");
    }

    /// <summary>Formats a scale denominator for the log, such as <c>1:288,895</c>.</summary>
    /// <param name="scale">The scale denominator.</param>
    public static string FormatScale(double scale) =>
        string.Create(CultureInfo.InvariantCulture, $"1:{scale:N0}");
}
