using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using EncDotNet.S100.Datasets.Pipelines;

namespace EncDotNet.SoundCharts;

/// <summary>
/// Draws the selected Object information feature on the map as a graphic
/// overlay, the way Pro flashes a selection. One highlight at a time.
/// </summary>
internal sealed class ObjectInfoHighlight
{
    private IDisposable? _overlay;

    /// <summary>
    /// Highlights <paramref name="geometry"/> in <paramref name="view"/>, or the
    /// point itself when the feature has no geometry (a coverage value).
    /// </summary>
    /// <param name="view">The map view the pick was made in.</param>
    /// <param name="geometry">The feature's geometry, WGS-84, or <c>null</c>.</param>
    /// <param name="latitude">The picked latitude.</param>
    /// <param name="longitude">The picked longitude.</param>
    public Task ShowAsync(MapView view, S100FeatureGeometry? geometry, double latitude, double longitude) =>
        QueuedTask.Run(() =>
        {
            ClearOnMct();
            var shape = geometry is { HasGeometry: true } g
                ? ToGeometry(g)
                : MapPointBuilderEx.CreateMapPoint(longitude, latitude, SpatialReferences.WGS84);
            _overlay = view.AddOverlay(shape, SymbolFor(shape).MakeSymbolReference());
        });

    /// <summary>Removes the highlight.</summary>
    public Task ClearAsync() => QueuedTask.Run(ClearOnMct);

    private void ClearOnMct()
    {
        _overlay?.Dispose();
        _overlay = null;
    }

    private static Geometry ToGeometry(S100FeatureGeometry geometry)
    {
        var wgs84 = SpatialReferences.WGS84;
        if (geometry.ExteriorRing.Count > 0)
        {
            var polygon = new PolygonBuilderEx(wgs84);
            polygon.AddPart(geometry.ExteriorRing.Select(ToCoordinate));
            foreach (var hole in geometry.InteriorRings.Where(r => r.Count > 0))
            {
                polygon.AddPart(hole.Select(ToCoordinate));
            }

            // Ring orientation varies by producer; simplify to Pro's rules.
            return GeometryEngine.Instance.SimplifyAsFeature(polygon.ToGeometry(), true);
        }

        if (geometry.Curves.Count > 0)
        {
            var polyline = new PolylineBuilderEx(wgs84);
            foreach (var curve in geometry.Curves.Where(c => c.Count > 0))
            {
                polyline.AddPart(curve.Select(ToCoordinate));
            }

            return polyline.ToGeometry();
        }

        return geometry.Points.Count == 1
            ? MapPointBuilderEx.CreateMapPoint(ToCoordinate(geometry.Points[0]), wgs84)
            : MultipointBuilderEx.CreateMultipoint(geometry.Points.Select(ToCoordinate), wgs84);
    }

    private static Coordinate2D ToCoordinate(S100.DataModel.GeoPosition position) =>
        new(position.Longitude, position.Latitude);

    private static CIMSymbol SymbolFor(Geometry shape)
    {
        // Pro's selection cyan, so the highlight reads as "this one".
        var colour = ColorFactory.Instance.CreateRGBColor(0, 255, 255);
        return shape switch
        {
            Polygon => SymbolFactory.Instance.ConstructPolygonSymbol(
                ColorFactory.Instance.CreateRGBColor(0, 255, 255, 20),
                SimpleFillStyle.Solid,
                SymbolFactory.Instance.ConstructStroke(colour, 3.0)),
            Polyline => SymbolFactory.Instance.ConstructLineSymbol(colour, 4.0),
            _ => SymbolFactory.Instance.ConstructPointSymbol(colour, 14.0, SimpleMarkerStyle.Circle),
        };
    }
}
