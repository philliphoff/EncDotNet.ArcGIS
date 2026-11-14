namespace EncDotNet.SoundCharts.Export;

/// <summary>
/// Renders a SoundCharts layer for a layout export at the layout's scale
/// and resolution (issue #12).
/// </summary>
/// <remarks>
/// The export swaps each rendered layer in for the WMTS layer, so a renderer
/// only has to produce a georeferenced image. Today's renderer is
/// <see cref="TilesExportRenderer"/>; one over DPI-aware WMS GetMap (U8,
/// <c>DPI</c>, <c>MAP_RESOLUTION</c>, <c>FORMAT_OPTIONS=dpi:N</c>) can
/// replace it without changing the export.
/// </remarks>
public interface IChartRasterRenderer
{
    /// <summary>Gets a short name for the log.</summary>
    string Name { get; }

    /// <summary>Renders <paramref name="request"/> to its output path.</summary>
    /// <param name="request">The layer, frame and resolution.</param>
    /// <param name="cancellationToken">Cancels the render.</param>
    /// <returns>The rendered image.</returns>
    /// <exception cref="ChartRasterException">The render failed.</exception>
    Task<ChartRaster> RenderAsync(ChartRasterRequest request, CancellationToken cancellationToken = default);
}
