namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>
/// The SoundCharts layers of the open project, by layer URI, with their
/// markers (design brief §3.4). The chart layer manager fills it from the
/// markers as layers are added, found when a project opens, or removed;
/// Object information and <b>Zoom to Data</b> read it without touching Pro.
/// </summary>
/// <remarks>Safe to use from any thread.</remarks>
public sealed class ChartLayerRegistry
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, Entry> _layers = new(StringComparer.Ordinal);

    /// <summary>Gets the number of registered layers, the group included.</summary>
    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _layers.Count;
            }
        }
    }

    /// <summary>Registers, or updates, the layer <paramref name="layerUri"/> of map <paramref name="mapUri"/>.</summary>
    /// <param name="layerUri">The layer's URI (Pro's <c>MapMember.URI</c>).</param>
    /// <param name="mapUri">The URI of the map that holds it.</param>
    /// <param name="marker">Its marker.</param>
    public void Set(string layerUri, string mapUri, ChartLayerMarker marker)
    {
        ArgumentException.ThrowIfNullOrEmpty(layerUri);
        ArgumentNullException.ThrowIfNull(mapUri);
        ArgumentNullException.ThrowIfNull(marker);
        lock (_gate)
        {
            _layers[layerUri] = new Entry(mapUri, marker);
        }
    }

    /// <summary>Forgets the layer <paramref name="layerUri"/>.</summary>
    /// <param name="layerUri">The layer's URI.</param>
    /// <returns>Whether it was registered.</returns>
    public bool Remove(string layerUri)
    {
        lock (_gate)
        {
            return _layers.Remove(layerUri);
        }
    }

    /// <summary>Forgets every layer, as when the project closes.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            _layers.Clear();
        }
    }

    /// <summary>Gets the marker of layer <paramref name="layerUri"/>, or <see langword="null"/> when it isn't a SoundCharts layer.</summary>
    /// <param name="layerUri">The layer's URI.</param>
    public ChartLayerMarker? Find(string layerUri)
    {
        lock (_gate)
        {
            return _layers.TryGetValue(layerUri, out var entry) ? entry.Marker : null;
        }
    }

    /// <summary>Gets the chart layers (not groups) of map <paramref name="mapUri"/>, with their URIs.</summary>
    /// <param name="mapUri">The map's URI.</param>
    public IReadOnlyList<KeyValuePair<string, ChartLayerMarker>> LayersOf(string mapUri)
    {
        lock (_gate)
        {
            return [.. _layers
                .Where(pair => pair.Value.MapUri == mapUri && !pair.Value.Marker.IsGroup)
                .Select(pair => new KeyValuePair<string, ChartLayerMarker>(pair.Key, pair.Value.Marker))];
        }
    }

    private sealed record Entry(string MapUri, ChartLayerMarker Marker);
}
