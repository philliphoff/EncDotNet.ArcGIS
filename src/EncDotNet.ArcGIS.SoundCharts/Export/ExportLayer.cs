using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.ChartLayers;
using EncDotNet.SoundCharts.Charts;

namespace EncDotNet.SoundCharts.Export;

/// <summary>A SoundCharts chart layer a layout export draws, in one map frame.</summary>
/// <param name="Layer">The WMTS chart layer.</param>
/// <param name="FrameName">The map frame's name.</param>
/// <param name="Name">The layer's name.</param>
/// <param name="Marker">The layer's provenance marker.</param>
/// <param name="Datasets">What its chart server serves.</param>
/// <param name="Palette">Its WMTS style.</param>
/// <param name="Time">The instant it draws, or <see langword="null"/>.</param>
/// <param name="Frame">What the map frame shows.</param>
internal sealed record ExportLayer(
    Layer Layer,
    string FrameName,
    string Name,
    ChartLayerMarker Marker,
    DatasetSet Datasets,
    string Palette,
    DateTime? Time,
    ExportFrame Frame);
