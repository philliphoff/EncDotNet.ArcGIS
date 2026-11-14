namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>
/// What keeps a map's <b>Coverage</b> layer in step with its chart layers'
/// datasets: footprints to read and write, and features to delete.
/// </summary>
/// <param name="ToRead">Datasets with no footprint, or whose footprint records another edition or layer.</param>
/// <param name="ToDelete">Paths of features to delete first: datasets gone from the chart, and those read again.</param>
public sealed record CoverageSyncPlan(IReadOnlyList<ChartLayerDataset> ToRead, IReadOnlyList<string> ToDelete)
{
    /// <summary>Gets whether the layer already matches the datasets.</summary>
    public bool IsEmpty => ToRead.Count == 0 && ToDelete.Count == 0;

    /// <summary>
    /// Plans the changes that make <paramref name="features"/> match
    /// <paramref name="datasets"/>, comparing paths without case, as
    /// Windows does.
    /// </summary>
    /// <param name="datasets">The datasets the map's chart layers draw.</param>
    /// <param name="features">The coverage features, as path and <see cref="CoverageTable.KeyOf(ChartLayerDataset)"/> pairs.</param>
    public static CoverageSyncPlan For(IEnumerable<ChartLayerDataset> datasets, IEnumerable<KeyValuePair<string, string>> features)
    {
        ArgumentNullException.ThrowIfNull(datasets);
        ArgumentNullException.ThrowIfNull(features);

        var wanted = datasets
            .DistinctBy(d => d.Path, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(d => d.Path, StringComparer.OrdinalIgnoreCase);

        // A path written twice (a crash between delete and write) is deleted and read again.
        var present = features.GroupBy(f => f.Key, StringComparer.OrdinalIgnoreCase).ToList();

        var toDelete = new List<string>();
        var current = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in present)
        {
            if (wanted.TryGetValue(group.Key, out var dataset)
                && group.Count() == 1
                && string.Equals(group.Single().Value, CoverageTable.KeyOf(dataset), StringComparison.Ordinal))
            {
                current.Add(group.Key);
            }
            else
            {
                toDelete.Add(group.First().Key);
            }
        }

        var toRead = wanted.Values
            .Where(d => !current.Contains(d.Path))
            .OrderBy(d => d.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new CoverageSyncPlan(toRead, toDelete);
    }
}
