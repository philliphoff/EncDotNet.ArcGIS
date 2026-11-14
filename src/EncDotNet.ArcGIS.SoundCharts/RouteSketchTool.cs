using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>Draw Route</b> (<c>SoundCharts_RouteSketchTool</c> in Config.daml): a
/// line sketch on the map; finishing it checks the line as a route in the
/// <b>SoundCharts Route check</b> pane.
/// </summary>
internal sealed class RouteSketchTool : MapTool
{
    public RouteSketchTool()
    {
        IsSketchTool = true;
        SketchType = SketchGeometryType.Line;
        SketchOutputMode = SketchOutputMode.Map;
    }

    protected override async Task<bool> OnSketchCompleteAsync(Geometry geometry)
    {
        try
        {
            if (geometry is Polyline { IsEmpty: false } line && RouteCheckPaneViewModel.Open() is { } pane)
            {
                await pane.CheckSketchAsync(line);
            }
        }
        catch (Exception ex)
        {
            // The boundary with Pro: log rather than let the exception reach Pro.
            AddInLog.Write($"Checking a drawn route failed: {ex}");
        }

        return true;
    }
}
