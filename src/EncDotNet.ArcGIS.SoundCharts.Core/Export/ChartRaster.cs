namespace EncDotNet.SoundCharts.Export;

/// <summary>A rendered SoundCharts layer: a georeferenced image Pro draws in its place.</summary>
/// <param name="Path">The GeoTIFF, in Web Mercator (EPSG:3857), with an alpha band.</param>
/// <param name="Description">How it was rendered, for the log: zoom, pixel ratio, resolution.</param>
public sealed record ChartRaster(string Path, string Description);
