using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.Library;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>Show Footprint</b> in the Library: draws a dataset's footprint from the
/// Library's index on the active map as a graphic overlay and zooms to it.
/// The dataset needn't be on the map: nothing is loaded. One footprint at a
/// time; it stays until the next one, or the pane closes.
/// </summary>
internal sealed class LibraryFootprintOverlay
{
    private IDisposable? _overlay;

    /// <summary>Shows <paramref name="footprint"/> in <paramref name="view"/> and zooms to it.</summary>
    /// <param name="view">The map view.</param>
    /// <param name="footprint">The footprint, in WGS-84.</param>
    public async Task ShowAsync(MapView view, LibraryFootprint footprint)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(footprint);
        var extent = await QueuedTask.Run(() =>
        {
            ClearOnMct();
            var wgs84 = SpatialReferences.WGS84;
            var builder = new PolygonBuilderEx(wgs84);
            foreach (var ring in footprint.Rings)
            {
                builder.AddPart(ring.Select(p => new Coordinate2D(p.Longitude, p.Latitude)));
            }

            // Ring orientation varies by producer; simplify to Pro's rules.
            var polygon = GeometryEngine.Instance.SimplifyAsFeature(builder.ToGeometry(), true);
            _overlay = view.AddOverlay(polygon, Symbol(footprint.IsBoundingBox).MakeSymbolReference());
            return EnvelopeBuilderEx.CreateEnvelope(footprint.West, footprint.South, footprint.East, footprint.North, wgs84)
                .Expand(1.2, 1.2, true);
        });

        await view.ZoomToAsync(extent, TimeSpan.FromSeconds(0.5));
    }

    /// <summary>Removes the footprint.</summary>
    public Task ClearAsync() => QueuedTask.Run(ClearOnMct);

    private void ClearOnMct()
    {
        _overlay?.Dispose();
        _overlay = null;
    }

    private static CIMSymbol Symbol(bool isBoundingBox)
    {
        // Pro's selection cyan, as Object information's highlight; a bounding box is dashed, being only an approximation.
        var colour = ColorFactory.Instance.CreateRGBColor(0, 255, 255);
        var stroke = SymbolFactory.Instance.ConstructStroke(colour, 2.5, isBoundingBox ? SimpleLineStyle.Dash : SimpleLineStyle.Solid);
        return SymbolFactory.Instance.ConstructPolygonSymbol(
            ColorFactory.Instance.CreateRGBColor(0, 255, 255, 15),
            SimpleFillStyle.Solid,
            stroke);
    }
}
