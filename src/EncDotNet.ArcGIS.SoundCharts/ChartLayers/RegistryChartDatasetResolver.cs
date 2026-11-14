using EncDotNet.SoundCharts.ObjectInfo;

namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>
/// The Object information resolver of the chart layer (A3): from a map's
/// visible layers, to the SoundCharts layers among them by their provenance
/// markers, to each layer's dataset set, to the datasets.
/// </summary>
/// <remarks>
/// Layers without a marker, such as Esri ENC layers or basemaps, are skipped.
/// The datasets come in the layers' order, top first, each once; within a
/// layer, in the dataset set's canonical order.
/// </remarks>
public sealed class RegistryChartDatasetResolver : IChartDatasetResolver
{
    private readonly ChartLayerRegistry _registry;

    /// <summary>Creates a resolver over <paramref name="registry"/>.</summary>
    /// <param name="registry">The project's SoundCharts layers.</param>
    public RegistryChartDatasetResolver(ChartLayerRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ChartDataset> Resolve(IReadOnlyList<ChartLayerReference> layers)
    {
        ArgumentNullException.ThrowIfNull(layers);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var datasets = new List<ChartDataset>();
        foreach (var layer in layers)
        {
            if (_registry.Find(layer.LayerUri) is not { IsGroup: false } marker)
            {
                continue;
            }

            // The server's dataset set fixes the canonical order (A2).
            var byPath = marker.ToChartDatasets().ToDictionary(d => d.Path, StringComparer.OrdinalIgnoreCase);
            foreach (var source in marker.ToDatasetSet().Sources)
            {
                if (byPath.TryGetValue(source.Path, out var dataset) && seen.Add(dataset.Path))
                {
                    datasets.Add(dataset);
                }
            }
        }

        return datasets;
    }
}
