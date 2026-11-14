using EncDotNet.SoundCharts.Charts;

namespace EncDotNet.SoundCharts.Export;

/// <summary>
/// One SoundCharts layer of one map frame, to be rendered for an export:
/// the layer's datasets and server options, its palette and time, and the
/// frame at the export's resolution.
/// </summary>
/// <param name="Name">The layer's name, for the log.</param>
/// <param name="Datasets">What the layer's chart server serves, with its start-up options.</param>
/// <param name="Palette">The layer's WMTS style: <c>day</c>, <c>dusk</c> or <c>night</c>.</param>
/// <param name="Frame">What the map frame shows.</param>
/// <param name="Dpi">The export resolution, in dots per inch.</param>
/// <param name="OutputPath">The GeoTIFF to write.</param>
public sealed record ChartRasterRequest(
    string Name,
    DatasetSet Datasets,
    string Palette,
    ExportFrame Frame,
    int Dpi,
    string OutputPath)
{
    /// <summary>
    /// Gets the instant the layer draws (its WMTS <c>Time</c> dimension, A8),
    /// or <see langword="null"/> when it isn't time-varying or has no time set.
    /// </summary>
    public DateTime? Time { get; init; }

    /// <summary>
    /// Gets the zero-based step of <see cref="Time"/> among the layer's time
    /// steps, for renderers that take a step rather than an instant, or
    /// <see langword="null"/> for the first step.
    /// </summary>
    public int? TimeStep { get; init; }
}
