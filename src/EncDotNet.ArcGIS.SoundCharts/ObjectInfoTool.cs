using System.Windows;
using System.Windows.Input;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.ObjectInfo;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The <b>Object information</b> map tool (<c>SoundCharts_ObjectInfoTool</c> in
/// Config.daml): a click lists every feature and coverage value of the visible
/// SoundCharts layers at the point in the <b>SoundCharts Object information</b>
/// pane (design brief D6).
/// </summary>
/// <remarks>
/// The pick runs in-process with EncDotNet.S100, not through WMS
/// GetFeatureInfo, so it lists every feature rather than Explore's top hit.
/// The click is read on the Main CIM Thread; the pick itself runs on the
/// thread pool, so neither Pro's UI nor the MCT waits for it.
/// </remarks>
internal sealed class ObjectInfoTool : MapTool
{
    /// <summary>How near, in screen pixels, a point or curve feature must be to the click to count.</summary>
    private const double TolerancePixels = 6.0;

    private CancellationTokenSource? _pick;

    public ObjectInfoTool()
    {
        IsSketchTool = false;
    }

    protected override Task OnToolActivateAsync(bool hasMapViewChanged)
    {
        ObjectInfoPaneViewModel.Open();
        return Task.CompletedTask;
    }

    protected override void OnToolMouseDown(MapViewMouseButtonEventArgs e)
    {
        // Handling the left button sends the click to HandleMouseDownAsync.
        if (e.ChangedButton == MouseButton.Left)
        {
            e.Handled = true;
        }
    }

    protected override async Task HandleMouseDownAsync(MapViewMouseButtonEventArgs e)
    {
        var view = ActiveMapView;
        if (view is null)
        {
            return;
        }

        // A new click supersedes a pick that is still running.
        using var pick = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _pick, pick);
        try
        {
            previous?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The previous pick finished in the meantime.
        }

        var pane = ObjectInfoPaneViewModel.Open();
        try
        {
            var click = await QueuedTask.Run(() => ReadClick(view, e.ClientPoint));
            if (click is null)
            {
                return;
            }

            var datasets = ObjectInfoHost.Resolver.Resolve(click.Layers);
            if (datasets.Count == 0)
            {
                pane?.ShowNoChartLayers(click.Latitude, click.Longitude);
                return;
            }

            pane?.ShowWorking(click.Latitude, click.Longitude);

            // Design brief §6, D30: coverages are sampled, and navigational
            // warnings checked, at the map instant; without map time each
            // dataset's default step is used.
            var report = await ObjectInfoHost.Service.IdentifyAsync(
                datasets, click.Latitude, click.Longitude, click.RadiusMeters, click.Time, pick.Token);
            pick.Token.ThrowIfCancellationRequested();
            pane?.ShowReport(report, view);
        }
        catch (OperationCanceledException) when (pick.IsCancellationRequested)
        {
            // A later click took over.
        }
        catch (Exception ex)
        {
            // The boundary with Pro: log rather than let the exception reach Pro.
            AddInLog.Write($"Object information failed: {ex}");
            pane?.ShowFailed();
        }
        finally
        {
            Interlocked.CompareExchange(ref _pick, null, pick);
        }
    }

    /// <summary>Reads the click's position and the map's visible layers. Runs on the MCT.</summary>
    private static Click? ReadClick(MapView view, Point clientPoint)
    {
        var mapPoint = view.ClientToMap(clientPoint);
        var nearby = view.ClientToMap(new Point(clientPoint.X + TolerancePixels, clientPoint.Y));
        if (mapPoint is null || nearby is null
            || GeometryEngine.Instance.Project(mapPoint, SpatialReferences.WGS84) is not MapPoint wgs84)
        {
            return null;
        }

        var radius = GeometryEngine.Instance.GeodesicDistance(mapPoint, nearby, LinearUnit.Meters);
        if (!double.IsFinite(radius))
        {
            radius = 0.0;
        }

        var map = view.Map;
        var layers = map.GetLayersAsFlattenedList()
            .Where(l => l.IsVisibleInView(view))
            .Select(l => new ChartLayerReference(l.URI, l.Name, map.URI))
            .ToList();
        return new Click(wgs84.Y, wgs84.X, Math.Clamp(radius, 0.0, 100_000.0), layers, MapTimeController.Instant(view.Time));
    }

    private sealed record Click(double Latitude, double Longitude, double RadiusMeters, IReadOnlyList<ChartLayerReference> Layers, DateTime? Time);
}
