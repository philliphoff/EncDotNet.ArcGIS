using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using EncDotNet.SoundCharts.ChartLayers;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.MapTime;

namespace EncDotNet.SoundCharts;

/// <summary>
/// Makes Pro's map time drive the time-varying SoundCharts layers (design
/// brief §6, D29–D33): on every map time change in the active map view it
/// snaps the instant to each <b>Water level</b> and <b>Surface currents</b>
/// layer's steps and writes the step into the layer's WMTS <c>Time</c>
/// dimension, or hides the layer and says so when there's no data then.
/// </summary>
/// <remarks>
/// <para>
/// Pro 3.7 can't time-enable a CIM WMTS layer, but it fills the tile URL's
/// <c>{Time}</c> from the connection's <c>Dimensions</c>, so the add-in sets
/// that (D29). The slider itself needs a time-aware layer in the map; the
/// <b>Coverage</b> layer is time-enabled for that with
/// <see cref="EnableTimeAsync"/> (D29, P2).
/// </para>
/// <para>
/// One layer has one URL, so it follows the active view's time (P4). Every
/// Pro event handler here is a boundary: failures are logged (D22).
/// </para>
/// </remarks>
internal sealed class MapTimeController
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, Tracked> _layers = new(StringComparer.Ordinal);

    // Maps whose time the add-in has set, or seen the user set: D32 never moves it again.
    private readonly HashSet<string> _settled = new(StringComparer.Ordinal);

    // Maps whose active view had a map time when last seen: the slider shows.
    private readonly HashSet<string> _timeAware = new(StringComparer.Ordinal);
    private bool _started;

    private MapTimeController()
    {
    }

    /// <summary>Gets the add-in's controller.</summary>
    public static MapTimeController Instance { get; } = new();

    /// <summary>Raised, on any thread, when a layer's status changes; the SoundCharts Layer tab reads <see cref="Statuses"/>.</summary>
    public event EventHandler? StatusChanged;

    /// <summary>Subscribes to Pro's map time and active view events.</summary>
    public void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        MapViewTimeChangedEvent.Subscribe(args =>
        {
            if (ReferenceEquals(args.MapView, MapView.Active))
            {
                Guard("following map time", () => UpdateAsync(args.MapView));
            }
        });
        ActiveMapViewChangedEvent.Subscribe(args =>
        {
            if (args.IncomingView is { } view)
            {
                Guard("following map time", () => UpdateAsync(view));
            }
        });
    }

    /// <summary>
    /// Follows map time for <paramref name="layerUri"/>: a <b>Water level</b>
    /// or <b>Surface currents</b> layer with <paramref name="steps"/>. Other
    /// layers, and layers without steps, are forgotten.
    /// </summary>
    /// <param name="map">The layer's map.</param>
    /// <param name="layerUri">The layer's URI.</param>
    /// <param name="kind">The layer's kind.</param>
    /// <param name="steps">The steps its chart server advertises, or <see langword="null"/>.</param>
    public void Track(Map map, string layerUri, ChartLayerKind kind, TimeSteps? steps)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (kind == ChartLayerKind.Chart || steps is null)
        {
            Forget(layerUri);
            return;
        }

        lock (_gate)
        {
            _layers[layerUri] = new Tracked(map, layerUri, ChartLayerKinds.DisplayName(kind), steps);
        }

        if (MapView.Active is { } view && view.Map?.URI == map.URI)
        {
            Guard("following map time", () => UpdateAsync(view));
        }
    }

    /// <summary>Stops following map time for a layer.</summary>
    /// <param name="layerUri">The layer's URI.</param>
    public void Forget(string layerUri)
    {
        bool removed;
        lock (_gate)
        {
            removed = _layers.Remove(layerUri);
        }

        if (removed)
        {
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Forgets every layer and map, when the project closes.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            _layers.Clear();
            _settled.Clear();
            _timeAware.Clear();
        }

        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Gets the union of the time layers' steps in <paramref name="map"/>, or <see langword="null"/>.</summary>
    /// <param name="map">The map.</param>
    public TimeSteps? StepsOf(Map? map)
    {
        if (map is null)
        {
            return null;
        }

        lock (_gate)
        {
            return TimeSteps.Union(_layers.Values.Where(t => t.Map.URI == map.URI).Select(t => t.Steps));
        }
    }

    /// <summary>
    /// Gets each time layer's status in <paramref name="map"/>, as last
    /// applied: its name and status, or a <see langword="null"/> status while
    /// the map has no map time.
    /// </summary>
    /// <param name="map">The map.</param>
    public IReadOnlyList<(string Name, TimeStatus? Status)> Statuses(Map? map)
    {
        if (map is null)
        {
            return [];
        }

        lock (_gate)
        {
            return [.. _layers.Values.Where(t => t.Map.URI == map.URI).OrderBy(t => t.Name).Select(t => (t.Name, t.Status))];
        }
    }

    /// <summary>
    /// Gets whether <paramref name="map"/>'s view had a map time when the
    /// add-in last looked: Pro shows the time slider only then.
    /// </summary>
    /// <param name="map">The map.</param>
    public bool HasMapTime(Map? map)
    {
        if (map is null)
        {
            return false;
        }

        lock (_gate)
        {
            return _timeAware.Contains(map.URI);
        }
    }

    /// <summary>Gets the step nearest map time of any layer with no data then, or <see langword="null"/>.</summary>
    /// <param name="map">The map.</param>
    public DateTime? NearestData(Map? map)
    {
        var statuses = Statuses(map).Select(s => s.Status).OfType<TimeStatus>().Where(s => !s.HasData).ToList();
        return statuses
            .Select(s => s.Nearest)
            .OfType<DateTime>()
            .OrderBy(n => (n - statuses[0].Instant).Duration())
            .Cast<DateTime?>()
            .FirstOrDefault();
    }

    /// <summary>Sets <paramref name="view"/>'s map time to the instant <paramref name="time"/>.</summary>
    /// <param name="view">The map view.</param>
    /// <param name="time">The instant (UTC).</param>
    public static Task SetMapTimeAsync(MapView view, DateTime time)
    {
        ArgumentNullException.ThrowIfNull(view);
        var utc = TimeSteps.Utc(time);
        return QueuedTask.Run(() => view.Time = new TimeRange(utc, utc));
    }

    /// <summary>
    /// Time-enables a feature layer from its start and end time fields, with
    /// the step interval of the map's time layers, so Pro shows the time
    /// slider over the data (D29, P2). The <b>Coverage</b> layer is the one.
    /// </summary>
    /// <param name="layer">The feature layer.</param>
    /// <param name="startField">The field holding each dataset's first time.</param>
    /// <param name="endField">The field holding each dataset's last time.</param>
    /// <returns>Whether Pro accepted the time settings.</returns>
    public async Task<bool> EnableTimeAsync(FeatureLayer layer, string startField, string endField)
    {
        ArgumentNullException.ThrowIfNull(layer);
        var steps = StepsOf(layer.Map);
        return await QueuedTask.Run(() =>
        {
            var parameters = new TimeParameters
            {
                StartTimeFieldName = startField,
                EndTimeFieldName = endField,
            };
            if (steps is not null)
            {
                parameters.StepInterval = new TimeDelta(steps.Interval.TotalMinutes, TimeUnit.Minutes);
                parameters.TimeRange = new TimeRange(steps.First, steps.Last);
            }

            if (!layer.CanSetTime(parameters))
            {
                return false;
            }

            layer.SetTime(parameters);
            return true;
        });
    }

    /// <summary>
    /// Applies <paramref name="view"/>'s map time to its map's time layers:
    /// first the default time (D32) if the map hasn't had one, then each
    /// layer's step or no-data state.
    /// </summary>
    private async Task UpdateAsync(MapView view)
    {
        if (view.Map is not { } map)
        {
            return;
        }

        List<Tracked> layers;
        lock (_gate)
        {
            layers = [.. _layers.Values.Where(t => t.Map.URI == map.URI)];
        }

        if (layers.Count == 0)
        {
            return;
        }

        var time = await QueuedTask.Run(() => view.Time);
        lock (_gate)
        {
            if (time is null)
            {
                _timeAware.Remove(map.URI);
            }
            else
            {
                _timeAware.Add(map.URI);
            }
        }

        if (time is null)
        {
            // No time-aware layer yet, so no slider: nothing drives time.
            SetStatuses(layers, null);
            return;
        }

        var instant = Instant(time);
        if (await DefaultTimeAsync(view, map, time, layers) is { } set)
        {
            instant = set;
        }

        if (instant is null)
        {
            SetStatuses(layers, null);
            return;
        }

        foreach (var layer in layers)
        {
            var status = layer.Steps.Resolve(instant.Value);
            var wanted = LayerTime.For(layer.Steps, status);
            if (wanted != layer.Applied && await QueuedTask.Run(() => Apply(map, layer.LayerUri, wanted)))
            {
                layer.Applied = wanted;
            }

            if (layer.Status?.HasData != false && !status.HasData)
            {
                AddInLog.Write(LogLevel.Info, TimeText.Describe(layer.Name, status));
            }

            layer.Status = status;
        }

        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets the default time (D32) when the map hasn't had one: all time, or
    /// the data's full extent, which is what Pro picks by itself when the
    /// first time-aware layer arrives. Any other time is the user's and
    /// settles the map.
    /// </summary>
    /// <returns>The instant set, or <see langword="null"/> when the map time was left alone.</returns>
    private async Task<DateTime?> DefaultTimeAsync(MapView view, Map map, TimeRange time, List<Tracked> layers)
    {
        lock (_gate)
        {
            if (!_settled.Add(map.URI))
            {
                return null;
            }
        }

        var steps = TimeSteps.Union(layers.Select(l => l.Steps));
        if (steps is null || !IsUnset(time, steps))
        {
            return null;
        }

        var instant = steps.DefaultTime(DateTime.UtcNow);
        await SetMapTimeAsync(view, instant);
        AddInLog.Write(LogLevel.Info, $"Set {map.Name}'s map time to {TimeText.FormatTime(instant)}.");
        return instant;
    }

    private static bool IsUnset(TimeRange time, TimeSteps steps) =>
        (time.Start is null && time.End is null)
        || (time.Start is { } start && time.End is { } end
            && TimeSteps.Utc(start) <= steps.First && TimeSteps.Utc(end) >= steps.Last);

    /// <summary>
    /// Gets the instant (UTC) of a map time range: its end (D30), or its
    /// start when it's open-ended; <see langword="null"/> without map time.
    /// </summary>
    /// <param name="time">The map view's time, or <see langword="null"/>.</param>
    public static DateTime? Instant(TimeRange? time) =>
        time is null ? null :
        time.End is { } end ? TimeSteps.Utc(end) : time.Start is { } start ? TimeSteps.Utc(start) : null;

    /// <summary>Writes a layer's time and visibility extent. Runs on the MCT.</summary>
    private static bool Apply(Map map, string layerUri, LayerTime wanted)
    {
        if (map.FindLayer(layerUri) is not { } layer
            || layer.GetDefinition() is not CIMTiledServiceLayer cim
            || cim.ServiceConnection is not CIMWMTSServiceConnection connection)
        {
            return false;
        }

        if (wanted.TimeSegment is { } segment)
        {
            var dimensions = connection.Dimensions is null
                ? new Dictionary<string, object>(StringComparer.Ordinal)
                : new Dictionary<string, object>(connection.Dimensions, StringComparer.Ordinal);
            dimensions[LayerTime.DimensionName] = segment;
            connection.Dimensions = dimensions;
        }

        cim.UseVisibilityTimeExtent = true;
        cim.VisibilityTimeExtent = new TimeExtent
        {
            StartTime = wanted.VisibleFrom,
            StartTimeSpecified = true,
            EndTime = wanted.VisibleTo,
            EndTimeSpecified = true,
        };
        layer.SetDefinition(cim);
        return true;
    }

    /// <summary>
    /// Carries a layer's time dimension over to a new connection, when the
    /// add-in repoints the layer at a restarted or replaced chart server (D20).
    /// </summary>
    /// <param name="from">The layer's current connection, or <see langword="null"/>.</param>
    /// <param name="to">The new connection.</param>
    public static void KeepTime(CIMServiceConnection? from, CIMWMTSServiceConnection to)
    {
        ArgumentNullException.ThrowIfNull(to);
        if (from is CIMWMTSServiceConnection { Dimensions: { Count: > 0 } dimensions })
        {
            to.Dimensions = new Dictionary<string, object>(dimensions, StringComparer.Ordinal);
        }
    }

    private void SetStatuses(List<Tracked> layers, TimeStatus? status)
    {
        foreach (var layer in layers)
        {
            layer.Status = status;
        }

        StatusChanged?.Invoke(this, EventArgs.Empty);
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

    private sealed class Tracked(Map map, string layerUri, string name, TimeSteps steps)
    {
        public Map Map { get; } = map;

        public string LayerUri { get; } = layerUri;

        public string Name { get; } = name;

        public TimeSteps Steps { get; } = steps;

        public LayerTime? Applied { get; set; }

        public TimeStatus? Status { get; set; }
    }
}
