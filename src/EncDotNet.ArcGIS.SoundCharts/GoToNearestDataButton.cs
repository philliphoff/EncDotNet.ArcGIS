using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.MapTime;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>Go to Nearest Data</b> on the SoundCharts Layer tab
/// (<c>SoundCharts_GoToNearestData</c>): moves map time to the nearest step
/// of a time layer with no data at map time (design brief §6.3, D31). Its
/// tooltip is the time layers' status, "No data at map time · last {time},
/// {span} earlier" and the like, so out of range is said, never hidden.
/// </summary>
internal sealed class GoToNearestDataButton : Button
{
    protected override void OnUpdate()
    {
        try
        {
            var map = MapView.Active?.Map;
            Enabled = MapTimeController.Instance.NearestData(map) is not null;
            Tooltip = TimeText.Summarize(MapTimeController.Instance.Statuses(map));
            DisabledTooltip = Tooltip;
        }
        catch (Exception ex)
        {
            // The boundary with Pro: OnUpdate runs often, so log and carry on.
            AddInLog.Write(LogLevel.Error, $"Updating Go to Nearest Data failed: {ex}");
        }
    }

    protected override async void OnClick()
    {
        try
        {
            if (MapView.Active is { } view && MapTimeController.Instance.NearestData(view.Map) is { } nearest)
            {
                await MapTimeController.SetMapTimeAsync(view, nearest);
            }
        }
        catch (Exception ex)
        {
            // The boundary with Pro: log rather than let the exception reach Pro.
            AddInLog.Write(LogLevel.Error, $"Go to Nearest Data failed: {ex}");
        }
    }
}
