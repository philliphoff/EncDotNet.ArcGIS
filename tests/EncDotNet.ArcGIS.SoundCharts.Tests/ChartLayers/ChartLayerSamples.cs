using EncDotNet.SoundCharts.ChartLayers;

namespace EncDotNet.SoundCharts.Tests.ChartLayers;

/// <summary>Fixed chart layers for the CIM, marker and resolver tests.</summary>
internal static class ChartLayerSamples
{
    public static readonly ChartLayerDataset Cell = new(
        @"C:\Charts\US5MA1BO\US5MA1BO.000", "S-57", null, new GeoExtent(-71.06, 42.31, -70.94, 42.38), "US");

    public static readonly ChartLayerDataset S101 = new(
        @"C:\Charts\S101\101AA00DS0003.000", "S-101", "2.0.0", new GeoExtent(-1.0, 50.0, 0.0, 51.0), "AA00", NotForNavigation: true);

    public static readonly ChartLayerDataset Tide = new(
        @"C:\Charts\S104\104US004SC1BO_20251217T12Z.h5", "S-104", "2.0.0", new GeoExtent(-80.1, 32.6, -79.8, 32.9), "US");

    public static readonly ChartLayerDataset Currents = new(
        @"C:\Charts\S111\111US00_DBOFS_20260320T18Z_US4DE1BB.h5", "S-111", "2.0.0", new GeoExtent(-75.6, 38.7, -75.0, 39.9), "US");

    public static readonly Uri Capabilities = new("http://127.0.0.1:52100/abc-123/wmts/1.0.0/WMTSCapabilities.xml");

    public static ChartLayerDefinition Definition(ChartLayerKind kind, params ChartLayerDataset[] datasets) =>
        new(ChartLayerMarker.ForLayer(kind, datasets), Capabilities, MinScale: ChartLayerDocument.ScaleOfZoom(11), ServerVersion: "0.27.1-main.20261113");
}
