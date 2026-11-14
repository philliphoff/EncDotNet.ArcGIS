using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.ObjectInfo;

namespace EncDotNet.SoundCharts;

/// <summary>
/// Opens the <b>SoundCharts Object information</b> pane at a position, as a
/// click with the <b>Object information</b> tool there would: how the
/// <b>SoundCharts Route check</b> pane links a finding into Object
/// information (design brief D7).
/// </summary>
internal static class ObjectInfoLauncher
{
    /// <summary>How near, in screen pixels, a point or curve feature must be to count; the tool's.</summary>
    private const double TolerancePixels = 6.0;

    /// <summary>Lists everything at a position in the Object information pane.</summary>
    /// <param name="view">The map view.</param>
    /// <param name="latitude">The position's latitude, WGS-84.</param>
    /// <param name="longitude">The position's longitude, WGS-84.</param>
    /// <param name="cancellationToken">Cancels the pick.</param>
    public static async Task ShowAtAsync(MapView view, double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var pane = ObjectInfoPaneViewModel.Open();
        try
        {
            var (radius, layers) = await QueuedTask.Run(() => Read(view, latitude, longitude));
            var datasets = ObjectInfoHost.Resolver.Resolve(layers);
            if (datasets.Count == 0)
            {
                pane?.ShowNoChartLayers(latitude, longitude);
                return;
            }

            pane?.ShowWorking(latitude, longitude);

            // As the tool: each dataset's default step until map time drives
            // time layers (A8).
            var report = await ObjectInfoHost.Service.IdentifyAsync(datasets, latitude, longitude, radius, time: null, cancellationToken);
            pane?.ShowReport(report, view);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            // The boundary with Pro: log rather than let the exception reach Pro.
            AddInLog.Write($"Object information at a route check finding failed: {ex}");
            pane?.ShowFailed();
        }
    }

    /// <summary>Reads the pick radius and the map's visible layers. Runs on the MCT.</summary>
    private static (double RadiusMeters, IReadOnlyList<ChartLayerReference> Layers) Read(MapView view, double latitude, double longitude)
    {
        var radius = 0.0;
        var at = GeometryEngine.Instance.Project(MapPointBuilderEx.CreateMapPoint(longitude, latitude, SpatialReferences.WGS84), view.Map.SpatialReference) as MapPoint;
        if (at is not null)
        {
            var client = view.MapToClient(at);
            var nearby = view.ClientToMap(new System.Windows.Point(client.X + TolerancePixels, client.Y));
            if (nearby is not null)
            {
                radius = GeometryEngine.Instance.GeodesicDistance(at, nearby, LinearUnit.Meters);
            }
        }

        var map = view.Map;
        var layers = map.GetLayersAsFlattenedList()
            .Where(l => l.IsVisibleInView(view))
            .Select(l => new ChartLayerReference(l.URI, l.Name, map.URI))
            .ToList();
        return (double.IsFinite(radius) ? Math.Clamp(radius, 0.0, 100_000.0) : 0.0, layers);
    }
}
