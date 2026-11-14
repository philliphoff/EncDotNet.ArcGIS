namespace EncDotNet.SoundCharts.Features;

/// <summary>A WGS 84 position: longitude (X) and latitude (Y), in degrees.</summary>
/// <param name="X">The longitude.</param>
/// <param name="Y">The latitude.</param>
public readonly record struct FeatureVertex(double X, double Y);
