namespace EncDotNet.SoundCharts.SymbologySpike;

/// <summary>
/// Factors on S-100 Part 9 millimetre sizes, per kind of graphic.
/// </summary>
/// <remarks>
/// <see cref="True"/> draws the catalogue's millimetres as millimetres on
/// paper. <see cref="Raster"/> reproduces the sizes EncDotNet.S100's
/// headless renderer (the chart server) draws at 0.28 mm per pixel: lines
/// and offsets at 1 px per 0.32 mm (S-100 Part 9 §3.10.4), point symbols at
/// 0.6 of 96 dpi, line-style symbols at 1 / 0.32 mm, pattern tiles at
/// 1.5 px/mm and font sizes in pixels rather than points
/// (<c>VectorSceneBuilder</c>, <c>SkiaSvgRasterizer</c>).
/// </remarks>
internal sealed record SizeProfile(string Name, double Line, double PointSymbol, double LineSymbol, double Pattern, double Text, double MinLineMm)
{
    private const double RasterPixelMm = 0.28;

    public static SizeProfile True { get; } = new("true", 1, 1, 1, 1, 1, 0);

    public static SizeProfile Raster { get; } = new(
        "raster",
        Line: RasterPixelMm / 0.32,
        PointSymbol: 0.6 * 96 / 25.4 * RasterPixelMm,
        LineSymbol: RasterPixelMm / 0.32,
        Pattern: 1.5 * RasterPixelMm,
        Text: 72.0 / 25.4 * RasterPixelMm,
        MinLineMm: RasterPixelMm);

    public double LineWidth(double millimetres) => Math.Max(millimetres * Line, MinLineMm);
}
