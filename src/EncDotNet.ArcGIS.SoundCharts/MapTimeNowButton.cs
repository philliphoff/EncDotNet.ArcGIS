using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>Now</b> on the SoundCharts Layer tab (<c>SoundCharts_MapTimeNow</c>):
/// sets map time to now, once (design brief §6.4, D32). There's no live
/// mode that keeps up with the clock (P5).
/// </summary>
internal sealed class MapTimeNowButton : Button
{
    protected override void OnUpdate()
    {
        try
        {
            // Map time exists only once the map has a time-aware layer.
            Enabled = MapTimeController.Instance.HasMapTime(MapView.Active?.Map);
        }
        catch (Exception ex)
        {
            // The boundary with Pro: OnUpdate runs often, so log and carry on.
            AddInLog.Write(LogLevel.Error, $"Updating Now failed: {ex}");
        }
    }

    protected override async void OnClick()
    {
        try
        {
            if (MapView.Active is { } view)
            {
                await MapTimeController.SetMapTimeAsync(view, DateTime.UtcNow);
            }
        }
        catch (Exception ex)
        {
            // The boundary with Pro: log rather than let the exception reach Pro.
            AddInLog.Write(LogLevel.Error, $"Setting map time to now failed: {ex}");
        }
    }
}
