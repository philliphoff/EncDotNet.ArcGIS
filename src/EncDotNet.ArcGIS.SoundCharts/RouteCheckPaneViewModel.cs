using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using EncDotNet.S100.DataModel;
using EncDotNet.SoundCharts.ObjectInfo;
using EncDotNet.SoundCharts.Resources;
using EncDotNet.SoundCharts.RouteCheck;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The <b>SoundCharts Route check</b> pane (<c>SoundCharts_RouteCheckPane</c>
/// in Config.daml, design brief D7): checks a selected line, a drawn route or
/// an S-421 or RTZ file against the map's SoundCharts layers, shows the
/// verdict and the findings grouped <b>MUST REVIEW</b>, <b>CHECK</b>,
/// <b>INFO</b>, writes them to the <b>Route check · {route}</b> layers, zooms
/// to a selected finding and links it into Object information.
/// </summary>
/// <remarks>
/// Logic lives in <see cref="RouteCheckService"/> and
/// <see cref="RouteCheckPresenter"/>; this class reads the map, runs them and
/// shows the result.
/// </remarks>
internal sealed class RouteCheckPaneViewModel : DockPane
{
    private const string PaneId = "SoundCharts_RouteCheckPane";
    private const string SketchToolId = "SoundCharts_RouteSketchTool";

    /// <summary>How far around a point finding to zoom, in degrees (about 500 m).</summary>
    private const double PointZoomDegrees = 0.005;

    private RouteCheckView _view = RouteCheckPresenter.Empty;
    private RouteInput? _route;
    private RouteFindingItem? _selectedItem;
    private string? _message;
    private bool _isBusy;
    private CancellationTokenSource? _check;
    private IDisposable? _overlay;

    public RouteCheckPaneViewModel()
    {
        CheckSelectedLineCommand = new RelayCommand(CheckSelectedLineAsync, () => !_isBusy && MapView.Active is not null, true, false);
        DrawRouteCommand = new RelayCommand(DrawRouteAsync, () => MapView.Active is not null, true, false);
        OpenRouteCommand = new RelayCommand(OpenRouteAsync, () => !_isBusy, true, false);
        CheckAgainCommand = new RelayCommand(CheckAgainAsync, () => !_isBusy && _route is not null, true, false);
        ObjectInfoCommand = new RelayCommand(ShowObjectInfoAsync, item => item is RouteFindingItem { Position: not null }, true, false);
    }

    /// <summary>Gets the route's name, or what the pane is waiting for.</summary>
    public string Heading => _view.Heading;

    /// <summary>Gets the ship parameters the check used.</summary>
    public string? Parameters => _view.Parameters;

    /// <summary>Gets the verdict.</summary>
    public string? Title => _view.Title;

    /// <summary>Gets the line under the verdict.</summary>
    public string? Subtitle => _view.Subtitle;

    /// <summary>Gets the verdict's tone, for the view's colours.</summary>
    public RouteCheckTone Tone => _view.Tone;

    /// <summary>Gets the clearance check's lower-confidence notes.</summary>
    public string? Confidence => _view.Confidence;

    /// <summary>Gets the footer: "Not for navigation · computed from …".</summary>
    public string? Footer => _view.Footer;

    /// <summary>Gets whether a check ran and found nothing.</summary>
    public bool IsEmptyResult => _view.IsEmpty;

    /// <summary>Gets the summary rows.</summary>
    public ObservableCollection<RouteCheckSummaryRow> Rows { get; } = [];

    /// <summary>Gets the findings, for a list grouped by <see cref="RouteFindingItem.Group"/>.</summary>
    public ObservableCollection<RouteFindingItem> Items { get; } = [];

    /// <summary>Gets one line per dataset that couldn't be read.</summary>
    public ObservableCollection<string> Problems { get; } = [];

    /// <summary>Gets a message about the last action ("Select a line feature in the map first."), or <c>null</c>.</summary>
    public string? Message
    {
        get => _message;
        private set => SetProperty(ref _message, value);
    }

    /// <summary>Gets whether a check is running.</summary>
    public bool IsChecking
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    /// <summary>Gets or sets the selected finding: the map zooms to it and highlights it.</summary>
    public RouteFindingItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            SetProperty(ref _selectedItem, value);
            if (value is not null)
            {
                _ = ZoomToAsync(value);
            }
        }
    }

    /// <summary>Gets the command that checks the line feature selected in the map.</summary>
    public ICommand CheckSelectedLineCommand { get; }

    /// <summary>Gets the command that starts <b>Draw Route</b>.</summary>
    public ICommand DrawRouteCommand { get; }

    /// <summary>Gets the command that checks an S-421 or RTZ file.</summary>
    public ICommand OpenRouteCommand { get; }

    /// <summary>Gets the command that checks the last route again.</summary>
    public ICommand CheckAgainCommand { get; }

    /// <summary>Gets the command that opens Object information at a finding.</summary>
    public ICommand ObjectInfoCommand { get; }

    /// <summary>Opens and activates the pane; <c>null</c> when Pro can't find it.</summary>
    public static RouteCheckPaneViewModel? Open()
    {
        var pane = FrameworkApplication.DockPaneManager.Find(PaneId) as RouteCheckPaneViewModel;
        pane?.Activate(false);
        return pane;
    }

    /// <summary>Checks a line drawn with <b>Draw Route</b>.</summary>
    /// <param name="line">The sketch, in the map's spatial reference.</param>
    public async Task CheckSketchAsync(Polyline line)
    {
        var vertices = await QueuedTask.Run(() => Vertices(line));
        await RunAsync(() => RouteLines.FromLine(Strings.RouteCheck_SketchName, RouteSource.Sketch, vertices));
    }

    protected override void OnShow(bool isVisible)
    {
        // The highlight belongs to the pane: hiding the pane clears it.
        if (!isVisible)
        {
            _ = ClearHighlightAsync();
        }
    }

    private async Task CheckSelectedLineAsync()
    {
        var view = MapView.Active;
        if (view is null)
        {
            Message = Strings.RouteCheck_NoMap;
            return;
        }

        try
        {
            var selected = await QueuedTask.Run(() => SelectedLine(view.Map));
            if (selected is null)
            {
                Message = Strings.RouteCheck_NoLineSelected;
                return;
            }

            await RunAsync(() => RouteLines.FromLine(selected.Value.Name, RouteSource.Feature, selected.Value.Vertices));
        }
        catch (Exception ex)
        {
            Fail(null, ex);
        }
    }

    private async Task DrawRouteAsync()
    {
        try
        {
            await FrameworkApplication.SetCurrentToolAsync(SketchToolId);
        }
        catch (Exception ex)
        {
            AddInLog.Write($"Starting Draw Route failed: {ex}");
        }
    }

    private async Task OpenRouteAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = Strings.RouteCheck_OpenRouteTitle,
            Filter = Strings.RouteCheck_OpenRouteFilter,
            CheckFileExists = true,
            Multiselect = false,
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var path = dialog.FileName;
        await RunAsync(() => RouteFiles.Read(path));
    }

    private Task CheckAgainAsync()
    {
        if (_route is not { } route)
        {
            return Task.CompletedTask;
        }

        // A file is read again: it may have been edited since.
        return route.Path is { } path ? RunAsync(() => RouteFiles.Read(path)) : RunAsync(() => route);
    }

    private async Task ShowObjectInfoAsync(object? parameter)
    {
        if (parameter is RouteFindingItem { Position: { } position } && MapView.Active is { } view)
        {
            await ObjectInfoLauncher.ShowAtAsync(view, position.Latitude, position.Longitude);
        }
    }

    /// <summary>Reads a route, checks it against the active map's SoundCharts layers, shows and writes the findings.</summary>
    private async Task RunAsync(Func<RouteInput> read)
    {
        var view = MapView.Active;
        if (view is null)
        {
            Message = Strings.RouteCheck_NoMap;
            return;
        }

        using var check = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _check, check);
        try
        {
            previous?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The previous check finished in the meantime.
        }

        Message = null;
        IsChecking = true;
        RouteInput? route = null;
        try
        {
            route = await Task.Run(read, check.Token);
            _route = route;
            Show(RouteCheckPresenter.Working(route.Name));
            await ClearHighlightAsync();

            var map = view.Map;
            var layers = await QueuedTask.Run(() => map.GetLayersAsFlattenedList()
                .Select(l => new ChartLayerReference(l.URI, l.Name, map.URI))
                .ToList());
            var datasets = ObjectInfoHost.Resolver.Resolve(layers);
            var outcome = await RouteCheckHost.Service.CheckAsync(route, datasets, RouteCheckHost.Ship(), DateTimeOffset.UtcNow, check.Token);
            check.Token.ThrowIfCancellationRequested();

            var result = RouteCheckPresenter.Present(outcome);
            Show(result);
            AddInLog.Write(string.Create(
                CultureInfo.InvariantCulture,
                $"Route check of {route.Name} ({route.Source}): {outcome.Report.Verdict.Kind}, {outcome.Report.Counts.MustReview} must review, {outcome.Report.Counts.Check} check, {outcome.Report.Counts.Info} info, against {datasets.Count} datasets."));

            await WriteLayersAsync(map, route.Name, outcome);
        }
        catch (OperationCanceledException) when (check.IsCancellationRequested)
        {
            // A later check took over.
        }
        catch (Exception ex)
        {
            Fail(route?.Name, ex);
        }
        finally
        {
            Interlocked.CompareExchange(ref _check, null, check);
            IsChecking = _check is not null;
        }
    }

    private async Task WriteLayersAsync(Map map, string routeName, RouteCheckOutcome outcome)
    {
        try
        {
            var features = FindingFeatures.From(outcome.Report.Findings);
            await QueuedTask.Run(() => FindingsLayers.Write(map, routeName, features, DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            // The pane still shows the findings; only the layers are missing.
            AddInLog.Write($"Writing the route check findings of {routeName} failed: {ex}");
            Message = string.Format(CultureInfo.CurrentCulture, Strings.RouteCheck_FindingsWriteFailedFormat, ex.Message);
        }
    }

    private void Fail(string? routeName, Exception ex)
    {
        if (ex is not RouteFileException)
        {
            AddInLog.Write($"Route check failed: {ex}");
        }

        Show(RouteCheckPresenter.Failed(routeName, ex.Message));
    }

    private void Show(RouteCheckView view)
    {
        RunOnUiThread(() =>
        {
            _view = view;
            Replace(Rows, view.Rows);
            Replace(Items, view.Items);
            Replace(Problems, view.Problems);
            _selectedItem = null;
            NotifyPropertyChanged(nameof(SelectedItem));
            NotifyPropertyChanged(nameof(Heading));
            NotifyPropertyChanged(nameof(Parameters));
            NotifyPropertyChanged(nameof(Title));
            NotifyPropertyChanged(nameof(Subtitle));
            NotifyPropertyChanged(nameof(Tone));
            NotifyPropertyChanged(nameof(Confidence));
            NotifyPropertyChanged(nameof(Footer));
            NotifyPropertyChanged(nameof(IsEmptyResult));
        });
    }

    private async Task ZoomToAsync(RouteFindingItem item)
    {
        if (!item.HasLocation || MapView.Active is not { } view)
        {
            return;
        }

        try
        {
            var shape = await QueuedTask.Run(() =>
            {
                var geometry = ToMap(item.Path, view.Map.SpatialReference);
                ClearHighlightOnMct();
                _overlay = view.AddOverlay(geometry, HighlightSymbol(geometry).MakeSymbolReference());
                return geometry;
            });
            var extent = item.Path.Count == 1
                ? Envelope(item.Path[0], view.Map.SpatialReference)
                : shape.Extent.Expand(1.2, 1.2, true);
            await view.ZoomToAsync(extent, TimeSpan.FromMilliseconds(400));
        }
        catch (Exception ex)
        {
            // The boundary with Pro: zooming is a convenience.
            AddInLog.Write($"Zooming to a route check finding failed: {ex}");
        }
    }

    private Task ClearHighlightAsync() => QueuedTask.Run(ClearHighlightOnMct);

    private void ClearHighlightOnMct()
    {
        _overlay?.Dispose();
        _overlay = null;
    }

    /// <summary>The first selected line feature of the map, as a route name and WGS-84 vertices. Runs on the MCT.</summary>
    private static (string Name, IReadOnlyList<GeoPosition> Vertices)? SelectedLine(Map map)
    {
        foreach (var layer in map.GetLayersAsFlattenedList().OfType<FeatureLayer>())
        {
            if (layer.ShapeType != esriGeometryType.esriGeometryPolyline)
            {
                continue;
            }

            using var selection = layer.GetSelection();
            var ids = selection.GetObjectIDs();
            if (ids.Count == 0)
            {
                continue;
            }

            using var cursor = layer.Search(new QueryFilter { ObjectIDs = [ids[0]] });
            if (cursor.MoveNext() && cursor.Current is Feature feature && feature.GetShape() is Polyline { IsEmpty: false } line)
            {
                using (feature)
                {
                    var name = string.Format(CultureInfo.CurrentCulture, Strings.RouteCheck_FeatureNameFormat, layer.Name, ids[0]);
                    return (name, Vertices(line));
                }
            }
        }

        return null;
    }

    private static IReadOnlyList<GeoPosition> Vertices(Polyline line)
    {
        var wgs84 = GeometryEngine.Instance.Project(line, SpatialReferences.WGS84) as Polyline ?? line;
        return [.. wgs84.Points.Select(p => new GeoPosition(p.Y, p.X))];
    }

    private static Geometry ToMap(IReadOnlyList<GeoPosition> path, SpatialReference spatialReference)
    {
        Geometry geometry = path.Count == 1
            ? MapPointBuilderEx.CreateMapPoint(path[0].Longitude, path[0].Latitude, SpatialReferences.WGS84)
            : PolylineBuilderEx.CreatePolyline(path.Select(p => new Coordinate2D(p.Longitude, p.Latitude)), SpatialReferences.WGS84);
        return GeometryEngine.Instance.Project(geometry, spatialReference);
    }

    private static Envelope Envelope(GeoPosition point, SpatialReference spatialReference)
    {
        var box = EnvelopeBuilderEx.CreateEnvelope(
            new Coordinate2D(point.Longitude - PointZoomDegrees, point.Latitude - PointZoomDegrees),
            new Coordinate2D(point.Longitude + PointZoomDegrees, point.Latitude + PointZoomDegrees),
            SpatialReferences.WGS84);
        return (Envelope)GeometryEngine.Instance.Project(box, spatialReference);
    }

    private static CIMSymbol HighlightSymbol(Geometry shape)
    {
        var colour = ColorFactory.Instance.CreateRGBColor(0, 255, 255);
        return shape is Polyline
            ? SymbolFactory.Instance.ConstructLineSymbol(colour, 6.0)
            : SymbolFactory.Instance.ConstructPointSymbol(colour, 18.0, SimpleMarkerStyle.Circle);
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = FrameworkApplication.Current.Dispatcher;
        if (dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.Invoke(action);
        }
    }
}
