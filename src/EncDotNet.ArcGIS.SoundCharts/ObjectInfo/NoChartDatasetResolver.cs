namespace EncDotNet.SoundCharts.ObjectInfo;

/// <summary>
/// The resolver used until the chart layer (A3) registers its own: it knows
/// no SoundCharts layers, so Object information finds no datasets.
/// </summary>
internal sealed class NoChartDatasetResolver : IChartDatasetResolver
{
    /// <summary>Gets the shared instance.</summary>
    public static NoChartDatasetResolver Instance { get; } = new();

    private NoChartDatasetResolver()
    {
    }

    /// <inheritdoc />
    public IReadOnlyList<ChartDataset> Resolve(IReadOnlyList<ChartLayerReference> layers) => [];
}
