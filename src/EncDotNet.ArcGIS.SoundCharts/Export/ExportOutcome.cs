namespace EncDotNet.SoundCharts.Export;

/// <summary>What an export did.</summary>
/// <param name="Exported">Whether Pro wrote the file.</param>
/// <param name="RenderedLayers">The number of SoundCharts layers rendered for the export.</param>
/// <param name="Failures">A message for each layer drawn from the chart server's tiles instead.</param>
internal sealed record ExportOutcome(bool Exported, int RenderedLayers, IReadOnlyList<string> Failures);
