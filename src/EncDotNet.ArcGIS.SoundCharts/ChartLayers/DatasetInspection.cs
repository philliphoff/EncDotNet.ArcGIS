namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>
/// What <see cref="DatasetInspector"/> found in one path the user chose: the
/// datasets SoundCharts can draw, and warnings about the rest.
/// </summary>
/// <param name="Input">The input's name for the group: a cell or file name, or an exchange set's or folder's name.</param>
/// <param name="Datasets">The datasets found, each with its product and edition read from its content.</param>
/// <param name="Warnings">
/// What the user should know: files that aren't datasets SoundCharts can
/// read, unsupported editions, and exchange-set entries whose catalogue
/// disagrees with their content.
/// </param>
public sealed record DatasetInspection(string Input, IReadOnlyList<ChartLayerDataset> Datasets, IReadOnlyList<string> Warnings);
