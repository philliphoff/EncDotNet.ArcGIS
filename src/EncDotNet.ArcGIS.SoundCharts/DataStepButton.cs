using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>‹ Previous data</b> and <b>Next data ›</b> in the Time group (design
/// brief §2.1, §6.6): move map time to the previous or next step of any
/// time-varying SoundCharts layer in the active map, the union of their
/// steps, as the viewer's data steps do. Pro's own <b>Time</b> tab still
/// steps by its interval.
/// </summary>
internal abstract class DataStepButton : Button
{
    /// <summary>Gets whether the button steps forward in time.</summary>
    protected abstract bool Forward { get; }

    /// <summary>Gets the command's name, for the log.</summary>
    protected abstract string Name { get; }

    /// <inheritdoc/>
    protected override void OnUpdate()
    {
        try
        {
            Enabled = MapTimeController.Instance.DataStep(MapView.Active?.Map, Forward) is not null;
        }
        catch (Exception ex)
        {
            // The boundary with Pro: OnUpdate runs often, so log and carry on.
            AddInLog.Write(LogLevel.Error, $"Updating {Name} failed: {ex}");
        }
    }

    /// <inheritdoc/>
    protected override async void OnClick()
    {
        try
        {
            if (MapView.Active is { } view && MapTimeController.Instance.DataStep(view.Map, Forward) is { } step)
            {
                await MapTimeController.SetMapTimeAsync(view, step);
            }
        }
        catch (Exception ex)
        {
            // The boundary with Pro: log rather than let the exception reach Pro.
            AddInLog.Write(LogLevel.Error, $"{Name} failed: {ex}");
        }
    }
}
