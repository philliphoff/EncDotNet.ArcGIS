using System.Net.Http;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using EncDotNet.SoundCharts.ChartLayers;
using EncDotNet.SoundCharts.Charts;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Server;
using EncDotNet.SoundCharts.Settings;

namespace EncDotNet.SoundCharts;

/// <summary>
/// Keeps the SoundCharts layers of the open project and their chart servers
/// together (design brief §3, §4.2, D11–D20): creates the
/// <b>SoundCharts · {source}</b> group and its chart layers, holds a server
/// lease per layer, repoints a layer when its server restarts, and finds the
/// layers again by their markers when a project opens or a layer comes back.
/// </summary>
/// <remarks>
/// Every Pro event handler here is a boundary with Pro: failures are logged,
/// never thrown (D22).
/// </remarks>
internal sealed class ChartLayerManager
{
    /// <summary>The DAML state on while the Contents selection includes a SoundCharts layer.</summary>
    public const string SelectedState = "SoundCharts_ChartLayerSelectedState";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    private readonly Lock _gate = new();
    private readonly Dictionary<string, Binding> _bindings = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _addGate = new(1, 1);
    private int _suppressAdded;
    private bool _started;

    private ChartLayerManager()
    {
    }

    /// <summary>Gets the add-in's manager.</summary>
    public static ChartLayerManager Instance { get; } = new();

    private static ChartLayerRegistry Registry => SoundChartsSession.ChartLayers;

    private static ChartServerPool Pool => SoundChartsSession.ChartServers;

    private static string? ServerVersion => Pool.Host.Executable?.Version;

    private static ChartSettings Settings => SoundChartsSession.Settings;

    /// <summary>Subscribes to Pro's project and layer events, and connects the layers of a project already open.</summary>
    public void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        ProjectOpenedEvent.Subscribe(_ => Guard("connecting the project's SoundCharts layers", ConnectProjectAsync));
        ProjectClosedEvent.Subscribe(_ => Guard("releasing the project's chart servers", ReleaseAllAsync));
        LayersAddedEvent.Subscribe(args => Guard("connecting added SoundCharts layers", () => OnLayersAddedAsync([.. args.Layers])));
        LayersRemovedEvent.Subscribe(_ => Guard("releasing removed SoundCharts layers", SweepRemovedAsync));
        TOCSelectionChangedEvent.Subscribe(args => UpdateSelectedState(args.MapView));
        ActiveMapViewChangedEvent.Subscribe(args => UpdateSelectedState(args.IncomingView));
        Settings.Changed += OnSettingsChanged;

        if (Project.Current is not null)
        {
            Guard("connecting the project's SoundCharts layers", ConnectProjectAsync);
        }
    }

    /// <summary>
    /// Adds <paramref name="inspections"/> to the SoundCharts group of
    /// <paramref name="map"/>, creating the group and its layers or joining
    /// the existing ones (D11), and starting a chart server for each changed
    /// layer before pointing the layer at it.
    /// </summary>
    /// <param name="map">The map.</param>
    /// <param name="inspections">What Add SoundCharts Data found.</param>
    /// <returns>What was added, and any layer whose server didn't start.</returns>
    public async Task<AddOutcome> AddAsync(Map map, IReadOnlyList<DatasetInspection> inspections)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(inspections);

        await _addGate.WaitAsync().ConfigureAwait(false);
        try
        {
            var current = await QueuedTask.Run(() => ReadMap(map));
            var after = inspections.Aggregate(current.Composition, (composition, i) => composition.With(i.Input, i.Datasets));
            var changed = after.ChangedSince(current.Composition);
            var failures = new List<string>();
            var prepared = new Dictionary<ChartLayerKind, Prepared>();

            foreach (var kind in changed)
            {
                var marker = after.Layers[kind];
                var name = ChartLayerKinds.DisplayName(kind);
                if (await PrepareAsync(marker).ConfigureAwait(false) is { } ready)
                {
                    prepared[kind] = ready;
                }
                else
                {
                    var server = Pool.Find(ServerSet(marker));
                    failures.Add(string.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        Resources.Strings.AddData_ServerFailed,
                        name,
                        server?.FailureReason ?? "it didn't answer."));
                }
            }

            if (prepared.Count == 0)
            {
                return new AddOutcome(after.GroupName, 0, current.Composition.Layers.Count == 0, failures);
            }

            List<(Layer Layer, Prepared Prepared)> bound;
            Interlocked.Increment(ref _suppressAdded);
            try
            {
                bound = await QueuedTask.Run(() => Apply(map, current, after, prepared));
            }
            finally
            {
                Interlocked.Decrement(ref _suppressAdded);
            }

            foreach (var (layer, ready) in bound)
            {
                Bind(layer.URI, map, ready.Definition.Marker, ready.Lease, ready.Definition.CapabilitiesUri);
            }

            // A prepared layer that wasn't placed (the map changed meanwhile) gives its server back.
            foreach (var ready in prepared.Values.Where(p => !bound.Any(b => ReferenceEquals(b.Prepared, p))))
            {
                await ready.Lease.DisposeAsync().ConfigureAwait(false);
            }

            var added = prepared.Keys.Sum(kind => inspections.SelectMany(i => i.Datasets).Count(d => d.Kind == kind));
            AddInLog.Write($"Added {added} dataset(s) to {after.GroupName} in {map.Name}.");
            return new AddOutcome(after.GroupName, added, current.Composition.Layers.Count == 0, failures);
        }
        finally
        {
            _addGate.Release();
        }
    }

    /// <summary>Gets the SoundCharts chart layers selected in Contents; a selected group gives all its map's chart layers.</summary>
    /// <param name="view">The map view.</param>
    public IReadOnlyList<ChartLayerMarker> SelectedChartLayers(MapView? view)
    {
        if (view?.Map is not { } map)
        {
            return [];
        }

        var markers = new List<ChartLayerMarker>();
        foreach (var layer in view.GetSelectedLayers())
        {
            switch (Registry.Find(layer.URI))
            {
                case { IsGroup: true }:
                    markers.AddRange(Registry.LayersOf(map.URI).Select(pair => pair.Value));
                    break;
                case { } marker:
                    markers.Add(marker);
                    break;
            }
        }

        return [.. markers.Distinct()];
    }

    /// <summary>Gets the chart servers of the SoundCharts layers selected in Contents.</summary>
    /// <param name="view">The map view.</param>
    public IReadOnlyList<ChartServer> SelectedServers(MapView? view)
    {
        if (view is null)
        {
            return [];
        }

        var uris = new HashSet<string>(StringComparer.Ordinal);
        foreach (var layer in view.GetSelectedLayers())
        {
            if (Registry.Find(layer.URI) is { IsGroup: true } && view.Map is { } map)
            {
                uris.UnionWith(Registry.LayersOf(map.URI).Select(pair => pair.Key));
            }
            else
            {
                uris.Add(layer.URI);
            }
        }

        lock (_gate)
        {
            return [.. uris
                .Select(uri => _bindings.TryGetValue(uri, out var binding) ? binding.Lease?.Server : null)
                .OfType<ChartServer>()
                .Distinct()];
        }
    }

    /// <summary>
    /// Zooms <paramref name="view"/> to the footprints of the selected
    /// layers' own datasets (§2.2), not the server's union bounds.
    /// </summary>
    /// <param name="view">The map view.</param>
    /// <param name="layers">The selected chart layers' markers.</param>
    /// <returns>Whether there was a footprint to zoom to.</returns>
    public static Task<bool> ZoomToDataAsync(MapView view, IReadOnlyList<ChartLayerMarker> layers) =>
        ZoomToAsync(view, layers.SelectMany(l => l.Datasets));

    /// <summary>
    /// Zooms <paramref name="view"/> to the union of <paramref name="datasets"/>'
    /// footprints, then in past the minimum scale of the layers drawing them
    /// if the footprint alone would leave them out of scale (D13): a small
    /// cell's footprint can sit just beyond its chart's lowest zoom.
    /// </summary>
    /// <param name="view">The map view.</param>
    /// <param name="datasets">The datasets.</param>
    /// <returns>Whether there was a footprint to zoom to.</returns>
    public static async Task<bool> ZoomToAsync(MapView view, IEnumerable<ChartLayerDataset> datasets)
    {
        ArgumentNullException.ThrowIfNull(view);
        var list = datasets.ToList();
        if (GeoExtent.Union(list.Select(d => d.Extent)) is not { } extent)
        {
            return false;
        }

        var paths = list.Select(d => d.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var (envelope, minScale) = await QueuedTask.Run(() =>
        {
            var box = EnvelopeBuilderEx.CreateEnvelope(extent.West, extent.South, extent.East, extent.North, SpatialReferences.WGS84)
                .Expand(1.1, 1.1, true);

            // The most zoomed-out scale at which one of these layers still draws.
            var scale = view.Map.GetLayersAsFlattenedList()
                .Where(l => ReadMarker(l) is { IsGroup: false } marker && marker.Datasets.Any(d => paths.Contains(d.Path)))
                .Select(l => l.MinScale)
                .Where(s => s > 0)
                .DefaultIfEmpty(0)
                .Max();
            return (box, scale);
        });

        if (!await view.ZoomToAsync(envelope, TimeSpan.Zero))
        {
            return false;
        }

        if (minScale > 0 && view.Camera is { } camera && camera.Scale > minScale)
        {
            camera.Scale = minScale * 0.95;
            await view.ZoomToAsync(camera, TimeSpan.Zero);
        }

        return true;
    }

    private static ChartLayerMarker? ReadMarker(Layer layer)
    {
        var properties = layer.GetDefinition()?.CustomProperties;
        var value = properties?.FirstOrDefault(p => p.Key == ChartLayerMarker.PropertyKey)?.Value;
        return ChartLayerMarker.TryParse(value);
    }

    /// <summary>
    /// The dataset set a layer's server serves: the marker's datasets that
    /// are still on disk, with the project's start-up options (A5).
    /// </summary>
    private static DatasetSet ServerSet(ChartLayerMarker marker)
    {
        var present = marker.Datasets.Where(d => d.Exists).ToList();
        var set = present.Count == 0 ? marker.ToDatasetSet() : ChartLayerMarker.ForLayer(marker.Kind!.Value, present).ToDatasetSet();
        return set.WithOptions(Settings.Effective.RenderOptions);
    }

    /// <summary>Reads a map's SoundCharts layers. Runs on the MCT.</summary>
    private static MapState ReadMap(Map map)
    {
        GroupLayer? group = null;
        ChartLayerMarker? groupMarker = null;
        var layers = new Dictionary<ChartLayerKind, (Layer Layer, ChartLayerMarker Marker)>();
        foreach (var layer in map.GetLayersAsFlattenedList())
        {
            switch (ReadMarker(layer))
            {
                case { IsGroup: true } marker when group is null && layer is GroupLayer g:
                    group = g;
                    groupMarker = marker;
                    break;
                case { IsGroup: false, Kind: { } kind } marker when !layers.ContainsKey(kind):
                    layers[kind] = (layer, marker);
                    break;
            }
        }

        var composition = ChartComposition.From(groupMarker, layers.Values.Select(v => v.Marker));
        return new MapState(group, composition, layers.ToDictionary(pair => pair.Key, pair => pair.Value.Layer));
    }

    /// <summary>Starts (or shares) the server for a layer and reads its minimum zoom; <see langword="null"/> when it didn't start.</summary>
    private static async Task<Prepared?> PrepareAsync(ChartLayerMarker marker)
    {
        ChartServerLease lease;
        try
        {
            lease = await Pool.AcquireAsync(ServerSet(marker)).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException or ArgumentException)
        {
            AddInLog.Write(LogLevel.Error, $"The chart server for {ChartLayerKinds.DisplayName(marker.Kind!.Value)} can't start: {ex.Message}");
            return null;
        }

        if (lease.Server.Endpoint is not { } endpoint)
        {
            await lease.DisposeAsync().ConfigureAwait(false);
            return null;
        }

        var minScale = TileJsonZoom.MinScale(await ReadMinZoomAsync(endpoint, lease.Server.LogSource).ConfigureAwait(false));
        var definition = new ChartLayerDefinition(
            marker, endpoint.WmtsCapabilitiesUri, Style: Settings.Effective.Style, MinScale: minScale, ServerVersion: ServerVersion);
        return new Prepared(lease, definition);
    }

    private static async Task<int?> ReadMinZoomAsync(ServerEndpoint endpoint, string logSource)
    {
        try
        {
            var json = await Http.GetStringAsync(endpoint.TileJsonUri).ConfigureAwait(false);
            var minZoom = TileJsonZoom.TryReadMinZoom(json);
            SoundChartsSession.Log.Write(LogLevel.Debug, logSource, $"Minimum zoom {minZoom?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "unknown"}.");
            return minZoom;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            SoundChartsSession.Log.Write(LogLevel.Warning, logSource, $"Couldn't read the minimum zoom: {ex.Message}");
            return null;
        }
    }

    /// <summary>Creates or updates the group and the prepared layers. Runs on the MCT.</summary>
    private static List<(Layer Layer, Prepared Prepared)> Apply(
        Map map, MapState current, ChartComposition after, Dictionary<ChartLayerKind, Prepared> prepared)
    {
        var bound = new List<(Layer, Prepared)>();
        var toCreate = prepared.Where(p => !current.Layers.ContainsKey(p.Key)).ToDictionary(p => p.Key, p => p.Value);
        var group = current.Group;

        if (group is null && toCreate.Count > 0)
        {
            // A new chart: the group and its layers come from one layer document.
            var json = ChartLayerDocument.ForGroup(after.GroupName, after.Group, [.. toCreate.Values.Select(p => p.Definition)]);
            var document = CIMLayerDocument.FromJson(json);
            group = LayerFactory.Instance.CreateLayer<GroupLayer>(new LayerCreationParams(document), map);
            foreach (var child in group.Layers)
            {
                if (ReadMarker(child) is { Kind: { } kind } && toCreate.Remove(kind, out var ready))
                {
                    bound.Add((child, ready));
                }
            }

            Registry.Set(group.URI, map.URI, after.Group);
        }
        else if (group is not null)
        {
            UpdateGroup(group, current.Composition, after);
            Registry.Set(group.URI, map.URI, after.Group);
        }

        // Layers new to an existing group: one layer document each.
        foreach (var (kind, ready) in toCreate)
        {
            var document = CIMLayerDocument.FromJson(ChartLayerDocument.ForLayer(ready.Definition));
            ILayerContainerEdit container = group is not null ? group : map;
            var layer = LayerFactory.Instance.CreateLayer<Layer>(new LayerCreationParams(document), container);
            bound.Add((layer, ready));
        }

        // Layers that already exist: new datasets, new server.
        foreach (var (kind, ready) in prepared)
        {
            if (current.Layers.TryGetValue(kind, out var layer) && UpdateLayer(layer, ready.Definition))
            {
                bound.Add((layer, ready));
            }
        }

        if (group is not null)
        {
            OrderChildren(group);
        }

        return bound;
    }

    private static void UpdateGroup(GroupLayer group, ChartComposition before, ChartComposition after)
    {
        if (group.GetDefinition() is not CIMGroupLayer definition)
        {
            return;
        }

        // Keep a name the user chose; follow the source otherwise.
        if (string.Equals(definition.Name, before.GroupName, StringComparison.Ordinal))
        {
            definition.Name = after.GroupName;
        }

        definition.CustomProperties = Marker(after.Group);
        definition.Description = ChartCredits.Description(after.Datasets.ToList());
        group.SetDefinition(definition);
    }

    /// <summary>Points an existing chart layer at a new server and dataset list. Runs on the MCT.</summary>
    private static bool UpdateLayer(Layer layer, ChartLayerDefinition definition)
    {
        if (layer.GetDefinition() is not CIMTiledServiceLayer cim)
        {
            return false;
        }

        cim.ServiceConnection = Connection(definition.CapabilitiesUri, definition.Style);
        cim.CustomProperties = Marker(definition.Marker);
        cim.Description = ChartCredits.Description(definition.Marker.Datasets);
        cim.Attribution = ChartCredits.Attribution(definition.Marker.Datasets, definition.ServerVersion);
        if (definition.MinScale > 0)
        {
            cim.MinScale = definition.MinScale;
        }

        layer.SetDefinition(cim);
        return true;
    }

    private static CIMWMTSServiceConnection Connection(Uri capabilitiesUri, string style) => new()
    {
        ServerConnection = new CIMInternetServerConnection
        {
            URL = capabilitiesUri.AbsoluteUri,
            Anonymous = true,
            HideUserProperty = true,
        },
        LayerName = ServerEndpoint.WmtsLayer,
        Style = style,
        TileMatrixSet = ServerEndpoint.WmtsTileMatrixSet,
        ImageFormat = "image/png",
    };

    private static CIMStringMap[] Marker(ChartLayerMarker marker) =>
        [new CIMStringMap { Key = ChartLayerMarker.PropertyKey, Value = marker.ToJson() }];

    /// <summary>Orders the group's chart layers top first (§3.1), leaving any other layer where it is.</summary>
    private static void OrderChildren(GroupLayer group)
    {
        var index = 0;
        foreach (var kind in ChartLayerKinds.TopFirst)
        {
            var layer = group.Layers.FirstOrDefault(l => ReadMarker(l)?.Kind == kind);
            if (layer is not null)
            {
                group.MoveLayer(layer, index++);
            }
        }
    }

    /// <summary>
    /// Records that <paramref name="layerUri"/> draws from
    /// <paramref name="lease"/>'s server, follows its restarts, and gives up
    /// the layer's previous server (D20: the new one started first).
    /// </summary>
    private void Bind(string layerUri, Map map, ChartLayerMarker marker, ChartServerLease? lease, Uri? pointedAt)
    {
        var binding = new Binding(layerUri, map, lease);
        if (lease is not null)
        {
            binding.Handler = (_, endpoint) => Guard("repointing a SoundCharts layer", () => RepointAsync(binding, endpoint));
            lease.Server.EndpointChanged += binding.Handler;
        }

        Binding? previous;
        lock (_gate)
        {
            _bindings.Remove(layerUri, out previous);
            _bindings[layerUri] = binding;
        }

        Registry.Set(layerUri, map.URI, marker);
        if (previous is not null)
        {
            Guard("releasing a chart server", previous.ReleaseAsync);
        }

        // A restart between reading the endpoint and subscribing would otherwise go unnoticed.
        if (lease?.Server.Endpoint is { } now && pointedAt is not null && now.WmtsCapabilitiesUri != pointedAt)
        {
            Guard("repointing a SoundCharts layer", () => RepointAsync(binding, now));
        }
    }

    private async Task RepointAsync(Binding binding, ServerEndpoint endpoint)
    {
        lock (_gate)
        {
            if (!_bindings.TryGetValue(binding.LayerUri, out var current) || !ReferenceEquals(current, binding))
            {
                return;
            }
        }

        var repointed = await QueuedTask.Run(() =>
        {
            if (binding.Map.FindLayer(binding.LayerUri) is not { } layer || layer.GetDefinition() is not CIMTiledServiceLayer cim)
            {
                return false;
            }

            cim.ServiceConnection = Connection(endpoint.WmtsCapabilitiesUri, Settings.Effective.Style);
            layer.SetDefinition(cim);
            return true;
        });

        if (repointed && binding.Lease is { } lease)
        {
            SoundChartsSession.Log.Write(LogLevel.Info, lease.Server.LogSource, $"Repointed the layer to {endpoint}.");
        }
    }

    private void OnSettingsChanged(object? sender, ChartSettingsChangedEventArgs args)
    {
        if (args.Change == ChartSettingsChange.None)
        {
            return;
        }

        Guard("applying the mariner settings", ApplySettingsAsync);
    }

    /// <summary>
    /// Applies the current settings to every SoundCharts layer (§5.5): a
    /// palette change sets each layer's WMTS style; a start-up option change
    /// starts servers with the new options, repoints the layers to them
    /// (new URLs, so Pro's tile cache can't show old tiles) and releases the
    /// old servers. A layer whose new server doesn't start keeps its old one.
    /// </summary>
    /// <remarks>
    /// Changes in quick succession each queue a run; every run reads the
    /// latest settings, so the later ones find nothing left to do.
    /// </remarks>
    private async Task ApplySettingsAsync()
    {
        await _addGate.WaitAsync().ConfigureAwait(false);
        try
        {
            List<Binding> bound;
            lock (_gate)
            {
                bound = [.. _bindings.Values.Where(b => b.Lease is not null)];
            }

            var style = Settings.Effective.Style;
            var restarted = 0;
            var restyled = 0;
            foreach (var binding in bound)
            {
                try
                {
                    switch (await ApplySettingsAsync(binding, style).ConfigureAwait(false))
                    {
                        case ChartSettingsChange.Restart:
                            restarted++;
                            break;
                        case ChartSettingsChange.Restyle:
                            restyled++;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // One layer (say, of a map closing) mustn't stop the rest.
                    AddInLog.Write(LogLevel.Warning, $"Mariner settings didn't apply to a SoundCharts layer: {ex.Message}");
                }
            }

            if (restarted + restyled > 0)
            {
                AddInLog.Write($"Mariner settings applied: palette {style}, {restarted} layer(s) on new chart servers, {restyled} restyled.");
            }
        }
        finally
        {
            _addGate.Release();
        }
    }

    /// <summary>Applies the current settings to one bound layer.</summary>
    /// <returns>What it took: a new server, a new style, or nothing.</returns>
    private async Task<ChartSettingsChange> ApplySettingsAsync(Binding binding, string style)
    {
        var found = await QueuedTask.Run(() =>
            binding.Map.FindLayer(binding.LayerUri) is { } layer && ReadMarker(layer) is { IsGroup: false } marker
                ? (Layer: layer, Marker: marker)
                : ((Layer Layer, ChartLayerMarker Marker)?)null);
        if (found is not { } item)
        {
            return ChartSettingsChange.None;
        }

        if (binding.Lease?.Server.Datasets.Equals(ServerSet(item.Marker)) == false)
        {
            await ConnectAsync(binding.Map, item.Layer, item.Marker).ConfigureAwait(false);
            return ChartSettingsChange.Restart;
        }

        return await QueuedTask.Run(() => Restyle(item.Layer, style)) ? ChartSettingsChange.Restyle : ChartSettingsChange.None;
    }

    /// <summary>Sets a chart layer's WMTS style. Runs on the MCT.</summary>
    /// <returns>Whether the style changed.</returns>
    private static bool Restyle(Layer layer, string style)
    {
        if (layer.GetDefinition() is not CIMTiledServiceLayer { ServiceConnection: CIMWMTSServiceConnection connection } cim
            || string.Equals(connection.Style, style, StringComparison.Ordinal)
            || connection.ServerConnection is not CIMInternetServerConnection { URL: { } url }
            || !Uri.TryCreate(url, UriKind.Absolute, out var capabilities))
        {
            return false;
        }

        // A new connection, as a repoint makes, so Pro reconnects rather than patching the old one.
        cim.ServiceConnection = Connection(capabilities, style);
        layer.SetDefinition(cim);
        return true;
    }

    /// <summary>Finds the SoundCharts layers of every map in the project and starts their servers.</summary>
    private async Task ConnectProjectAsync()
    {
        var found = await QueuedTask.Run(() =>
        {
            var layers = new List<(Map Map, Layer Layer, ChartLayerMarker Marker)>();
            foreach (var item in Project.Current.GetItems<MapProjectItem>())
            {
                if (item.GetMap() is not { } map)
                {
                    continue;
                }

                foreach (var layer in map.GetLayersAsFlattenedList())
                {
                    if (ReadMarker(layer) is { } marker)
                    {
                        layers.Add((map, layer, marker));
                    }
                }
            }

            return layers;
        });

        foreach (var (map, layer, marker) in found)
        {
            await ConnectAsync(map, layer, marker).ConfigureAwait(false);
        }
    }

    private async Task OnLayersAddedAsync(IReadOnlyList<Layer> added)
    {
        if (Volatile.Read(ref _suppressAdded) > 0)
        {
            return;
        }

        var found = await QueuedTask.Run(() =>
        {
            var layers = new List<(Map Map, Layer Layer, ChartLayerMarker Marker)>();
            foreach (var layer in added)
            {
                IEnumerable<Layer> all = layer is GroupLayer group ? [group, .. group.GetLayersAsFlattenedList()] : [layer];
                foreach (var l in all)
                {
                    if (l.Map is { } map && ReadMarker(l) is { } marker)
                    {
                        layers.Add((map, l, marker));
                    }
                }
            }

            return layers;
        });

        foreach (var (map, layer, marker) in found)
        {
            await ConnectAsync(map, layer, marker).ConfigureAwait(false);
        }
    }

    /// <summary>Registers a layer found by its marker and, for a chart layer, starts its server and repoints it.</summary>
    private async Task ConnectAsync(Map map, Layer layer, ChartLayerMarker marker)
    {
        var uri = layer.URI;
        lock (_gate)
        {
            if (_bindings.TryGetValue(uri, out var existing) && existing.Lease?.Server.Datasets.Equals(ServerSet(marker)) != false)
            {
                return;
            }
        }

        if (marker.IsGroup)
        {
            Bind(uri, map, marker, null, null);
            return;
        }

        var missing = marker.Datasets.Where(d => !d.Exists).ToList();
        if (missing.Count > 0)
        {
            AddInLog.Write(LogLevel.Warning, $"{layer.Name}: {missing.Count} dataset(s) are no longer on disk, for example {missing[0].Path}.");
        }

        if (missing.Count == marker.Datasets.Count)
        {
            Registry.Set(uri, map.URI, marker);
            return;
        }

        if (await PrepareAsync(marker).ConfigureAwait(false) is not { } ready)
        {
            Registry.Set(uri, map.URI, marker);
            return;
        }

        var updated = await QueuedTask.Run(() => UpdateLayer(layer, ready.Definition));
        if (!updated)
        {
            await ready.Lease.DisposeAsync().ConfigureAwait(false);
            return;
        }

        Bind(uri, map, marker, ready.Lease, ready.Definition.CapabilitiesUri);
    }

    /// <summary>Releases the servers of layers no longer in their map.</summary>
    private async Task SweepRemovedAsync()
    {
        List<Binding> all;
        lock (_gate)
        {
            all = [.. _bindings.Values];
        }

        var gone = await QueuedTask.Run(() => all.Where(b => b.Map.FindLayer(b.LayerUri) is null).ToList());
        foreach (var binding in gone)
        {
            lock (_gate)
            {
                if (!_bindings.TryGetValue(binding.LayerUri, out var current) || !ReferenceEquals(current, binding))
                {
                    continue;
                }

                _bindings.Remove(binding.LayerUri);
            }

            Registry.Remove(binding.LayerUri);
            await binding.ReleaseAsync().ConfigureAwait(false);
        }

        UpdateSelectedState(MapView.Active);
    }

    private async Task ReleaseAllAsync()
    {
        List<Binding> all;
        lock (_gate)
        {
            all = [.. _bindings.Values];
            _bindings.Clear();
        }

        Registry.Clear();
        foreach (var binding in all)
        {
            await binding.ReleaseAsync().ConfigureAwait(false);
        }
    }

    private void UpdateSelectedState(MapView? view)
    {
        try
        {
            var selected = view?.GetSelectedLayers().Any(l => Registry.Find(l.URI) is not null) == true;
            if (selected)
            {
                FrameworkApplication.State.Activate(SelectedState);
            }
            else
            {
                FrameworkApplication.State.Deactivate(SelectedState);
            }
        }
        catch (Exception ex)
        {
            AddInLog.Write(LogLevel.Error, $"Updating the SoundCharts Layer tab failed: {ex}");
        }
    }

    /// <summary>Runs <paramref name="work"/> without letting a failure reach Pro (D22).</summary>
    private static void Guard(string what, Func<Task> work)
    {
        _ = RunGuardedAsync(what, work);
    }

    private static async Task RunGuardedAsync(string what, Func<Task> work)
    {
        try
        {
            await work().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AddInLog.Write(LogLevel.Error, $"SoundCharts failed {what}: {ex}");
        }
    }

    /// <summary>What <see cref="AddAsync"/> did.</summary>
    /// <param name="GroupName">The group's name.</param>
    /// <param name="DatasetCount">The number of datasets now drawn from what was added.</param>
    /// <param name="IsNewChart">Whether the map had no SoundCharts layers before.</param>
    /// <param name="Failures">A message for each layer whose server didn't start.</param>
    public sealed record AddOutcome(string GroupName, int DatasetCount, bool IsNewChart, IReadOnlyList<string> Failures);

    private sealed record MapState(GroupLayer? Group, ChartComposition Composition, IReadOnlyDictionary<ChartLayerKind, Layer> Layers);

    private sealed record Prepared(ChartServerLease Lease, ChartLayerDefinition Definition);

    private sealed class Binding(string layerUri, Map map, ChartServerLease? lease)
    {
        public string LayerUri { get; } = layerUri;

        public Map Map { get; } = map;

        public ChartServerLease? Lease { get; } = lease;

        public EventHandler<ServerEndpoint>? Handler { get; set; }

        public async Task ReleaseAsync()
        {
            if (Lease is null)
            {
                return;
            }

            if (Handler is not null)
            {
                Lease.Server.EndpointChanged -= Handler;
            }

            await Lease.DisposeAsync().ConfigureAwait(false);
        }
    }
}
