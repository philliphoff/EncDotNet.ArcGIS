using System.IO;

namespace EncDotNet.SoundCharts.ObjectInfo;

/// <summary>
/// A stop-gap resolver for trying Object information before SoundCharts
/// layers exist: it returns a fixed list of datasets for any map, whatever
/// its layers. Used by tests, and by Debug builds when the
/// <see cref="EnvironmentVariable"/> environment variable is set. Release
/// builds never use it.
/// </summary>
internal sealed class FixedChartDatasetResolver : IChartDatasetResolver
{
    /// <summary>
    /// The environment variable a Debug build reads at start-up: dataset
    /// paths separated by <c>;</c>.
    /// </summary>
    public const string EnvironmentVariable = "SOUNDCHARTS_DEBUG_DATASETS";

    private readonly IReadOnlyList<ChartDataset> _datasets;

    /// <summary>Creates a resolver that always returns <paramref name="datasets"/>.</summary>
    /// <param name="datasets">The datasets to return.</param>
    public FixedChartDatasetResolver(IReadOnlyList<ChartDataset> datasets)
    {
        ArgumentNullException.ThrowIfNull(datasets);
        _datasets = datasets;
    }

    /// <summary>
    /// Creates a resolver from a <c>;</c>-separated list of paths, such as the
    /// value of <see cref="EnvironmentVariable"/>; <c>null</c> when the list
    /// names no dataset.
    /// </summary>
    /// <param name="paths">The paths, or <c>null</c>.</param>
    /// <returns>The resolver, or <c>null</c>.</returns>
    public static FixedChartDatasetResolver? FromPathList(string? paths)
    {
        if (string.IsNullOrWhiteSpace(paths))
        {
            return null;
        }

        var datasets = paths
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => new ChartDataset(Path.GetFullPath(p)))
            .ToList();
        return datasets.Count == 0 ? null : new FixedChartDatasetResolver(datasets);
    }

    /// <inheritdoc />
    public IReadOnlyList<ChartDataset> Resolve(IReadOnlyList<ChartLayerReference> layers) => _datasets;
}
