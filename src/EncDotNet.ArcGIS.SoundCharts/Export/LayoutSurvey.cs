using EncDotNet.SoundCharts.ChartLayers;

namespace EncDotNet.SoundCharts.Export;

/// <summary>What a layout's export draws from SoundCharts.</summary>
/// <param name="Layers">The visible SoundCharts chart layers, frame by frame.</param>
/// <param name="HasNotice">Whether a text element already says "Not for navigation" (D34).</param>
/// <param name="GeoreferenceFrameName">
/// The map frame raster formats are georeferenced to: the largest frame
/// with SoundCharts layers, else the first frame; <see langword="null"/>
/// without map frames.
/// </param>
internal sealed record LayoutSurvey(IReadOnlyList<ExportLayer> Layers, bool HasNotice, string? GeoreferenceFrameName)
{
    /// <summary>Gets the producers of the exported layers' datasets, as layer credits name them.</summary>
    public string Producers => ChartCredits.Producers(
        Layers.SelectMany(l => l.Marker.Datasets).DistinctBy(d => d.Path, StringComparer.OrdinalIgnoreCase));
}
