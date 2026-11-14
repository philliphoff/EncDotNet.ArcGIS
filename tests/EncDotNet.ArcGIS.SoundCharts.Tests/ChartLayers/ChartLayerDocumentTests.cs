using System.Text.Json.Nodes;
using EncDotNet.SoundCharts.ChartLayers;
using static EncDotNet.SoundCharts.Tests.ChartLayers.ChartLayerSamples;

namespace EncDotNet.SoundCharts.Tests.ChartLayers;

/// <summary>
/// The CIM the add-in authors (issue #5): JSON snapshots of the <c>.lyrx</c>
/// documents, checked by hand in Pro (and with arcpy) when they change. Set
/// <c>SOUNDCHARTS_UPDATE_SNAPSHOTS=1</c> to rewrite the snapshots after a
/// deliberate change, and review the diff.
/// </summary>
public class ChartLayerDocumentTests
{
    private const string UpdateVariable = "SOUNDCHARTS_UPDATE_SNAPSHOTS";

    [Fact]
    public void Group_MatchesSnapshot()
    {
        var json = ChartLayerDocument.ForGroup(
            "SoundCharts · 3 datasets",
            ChartLayerMarker.ForGroup(["US5MA1BO", "S104"]),
            [Definition(ChartLayerKind.Chart, Cell, S101), Definition(ChartLayerKind.WaterLevel, Tide)]);

        MatchSnapshot("group.lyrx", json);
    }

    [Fact]
    public void Layer_MatchesSnapshot()
    {
        MatchSnapshot("surface-currents.lyrx", ChartLayerDocument.ForLayer(Definition(ChartLayerKind.SurfaceCurrents, Currents)));
    }

    [Fact]
    public void Layer_IsATiledServiceLayerWithAWmtsConnectionToTheServer()
    {
        var layer = ChartLayerDocument.Layer(Definition(ChartLayerKind.Chart, Cell));

        Assert.Equal("CIMTiledServiceLayer", (string?)layer["type"]);
        Assert.Equal("Chart", (string?)layer["name"]);
        var connection = layer["serviceConnection"]!;
        Assert.Equal("CIMWMTSServiceConnection", (string?)connection["type"]);
        Assert.Equal("s100", (string?)connection["layerName"]);
        Assert.Equal("day", (string?)connection["style"]);
        Assert.Equal("WebMercatorQuad", (string?)connection["tileMatrixSet"]);
        Assert.Equal("image/png", (string?)connection["imageFormat"]);
        Assert.Equal("CIMInternetServerConnection", (string?)connection["serverConnection"]!["type"]);
        Assert.Equal(Capabilities.AbsoluteUri, (string?)connection["serverConnection"]!["url"]);
    }

    [Fact]
    public void Layer_CarriesItsMarkerAsACustomProperty()
    {
        var definition = Definition(ChartLayerKind.WaterLevel, Tide);
        var layer = ChartLayerDocument.Layer(definition);

        var property = Assert.Single(layer["customProperties"]!.AsArray())!;
        Assert.Equal("CIMStringMap", (string?)property["type"]);
        Assert.Equal(ChartLayerMarker.PropertyKey, (string?)property["key"]);
        Assert.Equal(definition.Marker, ChartLayerMarker.TryParse((string?)property["value"]));
    }

    [Fact]
    public void Layer_SaysItsProvenanceAndThatItsNotForNavigation()
    {
        var layer = ChartLayerDocument.Layer(Definition(ChartLayerKind.Chart, Cell, S101));

        Assert.Equal("SoundCharts portrayal of S-101, S-57 from AA00, US. Not for navigation.", (string?)layer["description"]);
        Assert.Equal("Data: AA00, US. Portrayal: SoundCharts (0.27.1-main.20261113). Not for navigation.", (string?)layer["attribution"]);
    }

    [Fact]
    public void Layer_MinimumScaleIsTheServersMinimumZoom()
    {
        var layer = ChartLayerDocument.Layer(Definition(ChartLayerKind.Chart, Cell));

        Assert.Equal(288_895.277, (double)layer["minScale"]!, 3);
    }

    [Fact]
    public void Group_ListsItsLayersTopFirst()
    {
        var json = ChartLayerDocument.ForGroup(
            "SoundCharts · 3 datasets",
            ChartLayerMarker.ForGroup(["a", "b"]),
            [Definition(ChartLayerKind.Chart, Cell), Definition(ChartLayerKind.WaterLevel, Tide), Definition(ChartLayerKind.SurfaceCurrents, Currents)]);

        var document = JsonNode.Parse(json)!;
        Assert.Equal([ChartLayerDocument.GroupUri], document["layers"]!.AsArray().Select(n => (string?)n));
        var group = document["layerDefinitions"]![0]!;
        Assert.Equal("CIMGroupLayer", (string?)group["type"]);
        Assert.Equal(
            ["Surface currents", "Water level", "Chart"],
            group["layers"]!.AsArray().Select(uri => (string?)document["layerDefinitions"]!.AsArray().Single(d => (string?)d!["uRI"] == (string?)uri)!["name"]));
    }

    [Fact]
    public void Group_NeedsAGroupMarker()
    {
        Assert.Throws<ArgumentException>(() => ChartLayerDocument.ForGroup(
            "SoundCharts · x", ChartLayerMarker.ForLayer(ChartLayerKind.Chart, [Cell]), [Definition(ChartLayerKind.Chart, Cell)]));
    }

    [Theory]
    [InlineData(0, 591_657_527.591555)]
    [InlineData(10, 577_790.554289)]
    [InlineData(14, 36_111.909643)]
    public void ScaleOfZoom_IsEsrisWebMercatorScale(int zoom, double scale)
    {
        Assert.Equal(scale, ChartLayerDocument.ScaleOfZoom(zoom), 5);
    }

    private static void MatchSnapshot(string name, string json)
    {
        var committed = Path.Combine(SourceDirectory(), "Snapshots", name);
        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(committed)!);
            File.WriteAllText(committed, json + "\n");
        }

        var expected = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ChartLayers", "Snapshots", name)).ReplaceLineEndings("\n").TrimEnd('\n');
        Assert.Equal(expected, json);
    }

    private static string SourceDirectory([System.Runtime.CompilerServices.CallerFilePath] string path = "") =>
        Path.GetDirectoryName(path)!;
}
