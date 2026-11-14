using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.ChartLayers;
using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The <b>Coverage</b> layer and the zoom floor (A7, design brief §3.2,
/// D12, D13, D15): footprints that follow the chart's datasets, Zoom to
/// Data from those footprints, and what the contextual tab needs to say
/// "Zoom in to see the chart".
/// </summary>
internal sealed partial class ChartLayerManager
{
    private static readonly Lazy<FootprintReader> Footprints = new(() => new FootprintReader());

    private readonly SemaphoreSlim _coverageGate = new(1, 1);

    /// <summary>
    /// Zooms <paramref name="view"/> to the footprints of the selected
    /// layers' own datasets (§2.2), or of the footprints selected in the
    /// Coverage layer among them; never to the bounds the chart servers
    /// publish, which are the union of all their datasets.
    /// </summary>
    /// <param name="view">The map view.</param>
    /// <param name="layers">The selected chart layers' markers.</param>
    /// <returns>Whether there was a footprint to zoom to.</returns>
    public static Task<bool> ZoomToDataAsync(MapView view, IReadOnlyList<ChartLayerMarker> layers) =>
        ZoomToAsync(view, layers.SelectMany(l => l.Datasets), useSelectedFootprints: true);

    /// <summary>
    /// Zooms <paramref name="view"/> to <paramref name="datasets"/>'
    /// footprints, then in to the minimum scale of the layers drawing them
    /// when the footprints fit only below it and the view's centre has data
    /// (D13, <see cref="ZoomFloor.ZoomInScale"/>). Uses each dataset's
    /// footprint feature, or the box Add SoundCharts Data found before the
    /// footprint is written.
    /// </summary>
    /// <param name="view">The map view.</param>
    /// <param name="datasets">The datasets.</param>
    /// <param name="useSelectedFootprints">Whether footprints selected in the Coverage layer narrow the datasets.</param>
    /// <returns>Whether there was a footprint to zoom to.</returns>
    public static async Task<bool> ZoomToAsync(MapView view, IEnumerable<ChartLayerDataset> datasets, bool useSelectedFootprints = false)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(datasets);
        var list = datasets.DistinctBy(d => d.Path, StringComparer.OrdinalIgnoreCase).ToList();
        if (list.Count == 0 || view.Map is not { } map)
        {
            return false;
        }

        var target = await QueuedTask.Run(() => FindTarget(map, list, useSelectedFootprints));
        if (target is null || !await view.ZoomToAsync(target.Box, TimeSpan.Zero))
        {
            return false;
        }

        if (view.Camera is { } camera)
        {
            var centreHasData = await QueuedTask.Run(() => target.HasDataAt(camera));
            if (ZoomFloor.ZoomInScale(camera.Scale, target.MinScale, centreHasData) is { } scale)
            {
                camera.Scale = scale;
                await view.ZoomToAsync(camera, TimeSpan.Zero);
            }
        }

        return true;
    }

    /// <summary>
    /// Gets the minimum scales of the SoundCharts chart layers selected in
    /// Contents (a selected group or Coverage layer gives all its map's),
    /// for the contextual tab's scale note.
    /// </summary>
    /// <param name="view">The map view.</param>
    public IReadOnlyList<double> SelectedMinScales(MapView? view)
    {
        if (view?.Map is not { } map)
        {
            return [];
        }

        // Called from the ribbon on the UI thread: only the registry and layer properties, no CIM.
        var layers = new Dictionary<string, Layer>(StringComparer.Ordinal);
        foreach (var layer in view.GetSelectedLayers())
        {
            switch (Registry.Find(layer.URI))
            {
                case { IsGroup: true } or { IsCoverage: true }:
                    var chartLayers = Registry.LayersOf(map.URI).Select(pair => pair.Key).ToHashSet(StringComparer.Ordinal);
                    foreach (var chartLayer in map.GetLayersAsFlattenedList().Where(l => chartLayers.Contains(l.URI)))
                    {
                        layers[chartLayer.URI] = chartLayer;
                    }

                    break;
                case { }:
                    layers[layer.URI] = layer;
                    break;
            }
        }

        return [.. layers.Values.Select(l => l.MinScale)];
    }

    /// <summary>Gets the Coverage layer of the map in <paramref name="view"/>, or <see langword="null"/>.</summary>
    /// <param name="view">The map view.</param>
    public static Layer? CoverageLayerOf(MapView? view) =>
        view?.Map is { } map && Registry.CoverageOf(map.URI) is { } uri ? map.GetLayersAsFlattenedList().FirstOrDefault(l => l.URI == uri) : null;

    /// <summary>
    /// Makes <paramref name="map"/>'s Coverage layer match its chart
    /// layers' datasets: reads and writes missing footprints, deletes those
    /// of datasets that are gone, labels each layer's footprints below that
    /// layer's minimum scale, and removes the layer when no dataset is left
    /// (D12).
    /// </summary>
    /// <param name="map">The map.</param>
    /// <param name="create">Whether to create the layer when the map has none: on Add SoundCharts Data, not when the user removed it.</param>
    private async Task SyncCoverageAsync(Map map, bool create)
    {
        await _coverageGate.WaitAsync().ConfigureAwait(false);
        try
        {
            var (state, coverage, keys) = await QueuedTask.Run(() =>
            {
                var current = ReadMap(map);
                var layer = CoverageLayers.Find(map);
                return (current, layer, layer is null ? [] : CoverageLayers.ReadKeys(layer));
            });

            var datasets = state.Composition.Datasets;
            if (datasets.Count == 0)
            {
                if (coverage is not null)
                {
                    await QueuedTask.Run(() => map.RemoveLayer(coverage));
                    AddInLog.Write($"Removed the Coverage layer from {map.Name}: no SoundCharts dataset is left.");
                }

                return;
            }

            if (coverage is null && !create)
            {
                return;
            }

            var plan = CoverageSyncPlan.For(datasets, keys);
            var footprints = await Task.Run(() => ReadFootprints(plan.ToRead)).ConfigureAwait(false);

            FeatureLayer layer;
            Interlocked.Increment(ref _suppressAdded);
            try
            {
                layer = await QueuedTask.Run(() =>
                {
                    var target = coverage ?? CoverageLayers.Create((ILayerContainerEdit?)state.Group ?? map);
                    CoverageLayers.Write(target, footprints, plan.ToDelete);
                    CoverageLayers.UpdateLabels(target, state.Layers.ToDictionary(pair => pair.Key, pair => pair.Value.MinScale));
                    return target;
                });
            }
            finally
            {
                Interlocked.Decrement(ref _suppressAdded);
            }

            if (coverage is null)
            {
                Bind(layer.URI, map, ChartLayerMarker.ForCoverage(), null, null);
                AddInLog.Write($"Created the Coverage layer in {map.Name}.");
            }

            if (!plan.IsEmpty)
            {
                AddInLog.Write($"Coverage in {map.Name}: wrote {footprints.Count} footprint(s), removed {plan.ToDelete.Count}.");
            }

            // A8: the slider needs a time-aware layer; Coverage carries the time layers' first and last times (D29).
            await MapTimeController.Instance.EnableCoverageTimeAsync(map).ConfigureAwait(false);
        }
        finally
        {
            _coverageGate.Release();
        }
    }

    /// <summary>Syncs the Coverage layer of each distinct map in <paramref name="maps"/>, without creating one.</summary>
    private async Task SyncCoverageAsync(IEnumerable<Map> maps)
    {
        foreach (var map in maps.DistinctBy(m => m.URI))
        {
            try
            {
                await SyncCoverageAsync(map, create: false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // One map's coverage mustn't stop the others.
                AddInLog.Write(LogLevel.Error, $"SoundCharts failed updating the Coverage layer of {map.Name}: {ex}");
            }
        }
    }

    private static List<DatasetFootprint> ReadFootprints(IReadOnlyList<ChartLayerDataset> datasets)
    {
        var footprints = new List<DatasetFootprint>();
        foreach (var dataset in datasets)
        {
            var footprint = Footprints.Value.Read(dataset, out var error);
            if (error is not null)
            {
                AddInLog.Write(LogLevel.Warning, $"Coverage: couldn't read {dataset.Name}'s coverage, so its footprint is its bounds: {error}");
            }

            if (footprint is null)
            {
                AddInLog.Write(LogLevel.Warning, $"Coverage: {dataset.Name} has no footprint.");
                continue;
            }

            footprints.Add(footprint);
        }

        return footprints;
    }

    /// <summary>Finds what Zoom to Data zooms to. Runs on the MCT.</summary>
    private static ZoomTarget? FindTarget(Map map, List<ChartLayerDataset> datasets, bool useSelectedFootprints)
    {
        var paths = datasets.Select(d => d.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var shapes = new Dictionary<string, Polygon>(StringComparer.OrdinalIgnoreCase);
        if (CoverageLayers.Find(map) is { } coverage)
        {
            if (useSelectedFootprints && coverage.SelectionCount > 0)
            {
                shapes = CoverageLayers.ReadShapes(coverage, paths, selectedOnly: true);
                if (shapes.Count > 0)
                {
                    paths = shapes.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
                }
            }

            if (shapes.Count == 0)
            {
                shapes = CoverageLayers.ReadShapes(coverage, paths, selectedOnly: false);
            }
        }

        // A dataset whose footprint isn't written yet: the box Add SoundCharts Data found.
        var boxes = datasets
            .Where(d => paths.Contains(d.Path) && !shapes.ContainsKey(d.Path))
            .Select(d => d.Extent)
            .OfType<GeoExtent>()
            .Where(e => e.IsValid)
            .ToList();
        var extents = shapes.Values
            .Select(s => (GeoExtent?)new GeoExtent(s.Extent.XMin, s.Extent.YMin, s.Extent.XMax, s.Extent.YMax))
            .Concat(boxes.Select(b => (GeoExtent?)b));
        if (GeoExtent.Union(extents) is not { } extent)
        {
            return null;
        }

        var box = EnvelopeBuilderEx.CreateEnvelope(extent.West, extent.South, extent.East, extent.North, SpatialReferences.WGS84)
            .Expand(ZoomFloor.Margin, ZoomFloor.Margin, true);

        // The most zoomed-out scale at which one of these layers still draws.
        var minScale = map.GetLayersAsFlattenedList()
            .Where(l => ReadMarker(l) is { Kind: not null } marker && marker.Datasets.Any(d => paths.Contains(d.Path)))
            .Select(l => l.MinScale)
            .Where(s => s > 0)
            .DefaultIfEmpty(0)
            .Max();
        return new ZoomTarget(box, minScale, [.. shapes.Values], boxes);
    }

    /// <summary>What Zoom to Data zooms to.</summary>
    /// <param name="Box">The footprints' extent, with a margin, in WGS-84.</param>
    /// <param name="MinScale">The most zoomed-out minimum scale of the layers drawing the datasets; 0 for none.</param>
    /// <param name="Shapes">The footprint polygons, in WGS-84.</param>
    /// <param name="Boxes">The boxes of datasets with no footprint feature yet.</param>
    private sealed record ZoomTarget(Envelope Box, double MinScale, IReadOnlyList<Polygon> Shapes, IReadOnlyList<GeoExtent> Boxes)
    {
        /// <summary>Gets whether a footprint contains <paramref name="camera"/>'s centre. Runs on the MCT.</summary>
        public bool HasDataAt(Camera camera)
        {
            var centre = MapPointBuilderEx.CreateMapPoint(camera.X, camera.Y, camera.SpatialReference ?? SpatialReferences.WGS84);
            if (GeometryEngine.Instance.Project(centre, SpatialReferences.WGS84) is not MapPoint point)
            {
                return false;
            }

            return Shapes.Any(s => GeometryEngine.Instance.Contains(s, point))
                || Boxes.Any(b => point.X >= b.West && point.X <= b.East && point.Y >= b.South && point.Y <= b.North);
        }
    }
}
