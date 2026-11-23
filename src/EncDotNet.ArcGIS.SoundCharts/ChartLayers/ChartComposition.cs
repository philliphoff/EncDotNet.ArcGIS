using System.Globalization;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>
/// The SoundCharts layers of one map, as markers: the group and a chart
/// layer per <see cref="ChartLayerKind"/> that has datasets (design brief
/// §3.1, D11, D12). Adding data gives a new composition; the add-in then
/// creates, repoints or leaves each layer to match.
/// </summary>
public sealed class ChartComposition
{
    private ChartComposition(ChartLayerMarker group, IReadOnlyDictionary<ChartLayerKind, ChartLayerMarker> layers)
    {
        Group = group;
        Layers = layers;
    }

    /// <summary>Gets an empty composition: a map without SoundCharts layers.</summary>
    public static ChartComposition Empty { get; } = new(ChartLayerMarker.ForGroup([]), new Dictionary<ChartLayerKind, ChartLayerMarker>());

    /// <summary>Gets the group's marker.</summary>
    public ChartLayerMarker Group { get; }

    /// <summary>Gets the chart layers' markers, by kind; a kind without datasets has none.</summary>
    public IReadOnlyDictionary<ChartLayerKind, ChartLayerMarker> Layers { get; }

    /// <summary>Gets every dataset of the chart, in layer order, top first.</summary>
    public IReadOnlyList<ChartLayerDataset> Datasets =>
        [.. ChartLayerKinds.TopFirst.Where(Layers.ContainsKey).SelectMany(kind => Layers[kind].Datasets)];

    /// <summary>
    /// Gets the group's name: <b>SoundCharts · {source}</b>, where source is
    /// the one input's name (a cell or an exchange set), or "{n} datasets"
    /// when more than one input was added (§3.3).
    /// </summary>
    public string GroupName
    {
        get
        {
            var source = Group.Inputs.Count == 1
                ? Group.Inputs[0]
                : string.Format(CultureInfo.CurrentCulture, Strings.Group_DatasetCount, Datasets.Count);
            return string.Format(CultureInfo.CurrentCulture, Strings.Group_Name, source);
        }
    }

    /// <summary>Gets the composition of a map from its existing markers.</summary>
    /// <param name="group">The group's marker, or <see langword="null"/> when the map has none.</param>
    /// <param name="layers">The chart layers' markers; two of one kind are merged.</param>
    public static ChartComposition From(ChartLayerMarker? group, IEnumerable<ChartLayerMarker> layers)
    {
        ArgumentNullException.ThrowIfNull(layers);
        var byKind = layers
            .Where(m => m is { IsGroup: false, Kind: not null })
            .GroupBy(m => m.Kind!.Value)
            .ToDictionary(g => g.Key, g => ChartLayerMarker.ForLayer(g.Key, g.SelectMany(m => m.Datasets)));
        return new ChartComposition(group is { IsGroup: true } ? group : ChartLayerMarker.ForGroup([]), byKind);
    }

    /// <summary>
    /// Gets the composition with <paramref name="datasets"/> added under the
    /// input name <paramref name="input"/>: each dataset joins the layer of its
    /// kind; a dataset already in the chart is kept once.
    /// </summary>
    /// <param name="input">The name of what the user added, such as <c>US5MA1BO</c> or an exchange set's folder name.</param>
    /// <param name="datasets">The datasets found in it.</param>
    public ChartComposition With(string input, IEnumerable<ChartLayerDataset> datasets)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);
        ArgumentNullException.ThrowIfNull(datasets);

        var added = datasets.ToList();
        if (added.Count == 0)
        {
            return this;
        }

        var layers = new Dictionary<ChartLayerKind, ChartLayerMarker>(Layers);
        foreach (var group in added.GroupBy(d => d.Kind))
        {
            var existing = layers.TryGetValue(group.Key, out var marker) ? marker.Datasets : [];

            // A dataset added again replaces its old entry: its file may have a new edition.
            var merged = group.Concat(existing.Where(d => !group.Any(a => string.Equals(a.Path, d.Path, StringComparison.OrdinalIgnoreCase))));
            layers[group.Key] = ChartLayerMarker.ForLayer(group.Key, merged);
        }

        return new ChartComposition(ChartLayerMarker.ForGroup([.. Group.Inputs, input]), layers);
    }

    /// <summary>
    /// Gets the kinds whose datasets differ between <paramref name="before"/>
    /// and this composition: the layers to create or repoint.
    /// </summary>
    /// <param name="before">The earlier composition.</param>
    public IReadOnlyList<ChartLayerKind> ChangedSince(ChartComposition before)
    {
        ArgumentNullException.ThrowIfNull(before);
        return [.. ChartLayerKinds.TopFirst.Where(kind =>
            Layers.TryGetValue(kind, out var now)
            && (!before.Layers.TryGetValue(kind, out var then) || !now.ToDatasetSet().Equals(then.ToDatasetSet())))];
    }
}
