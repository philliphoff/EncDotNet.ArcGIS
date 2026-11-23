using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.ChartLayers;
using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The scale note on the SoundCharts Layer tab (<c>SoundCharts_ScaleNote</c>,
/// design brief §2.2, D13): "Zoom in to see the chart" while the active map
/// is below the minimum scale of a selected chart layer, and only a dimmed
/// glyph otherwise (<see cref="ScaleNoteState"/>).
/// </summary>
internal sealed class ScaleNoteViewModel : CustomControl
{
    private ScaleNoteState _state = ScaleNoteState.Idle;
    private bool _failureLogged;

    /// <summary>Gets what the note shows.</summary>
    public ScaleNoteState State
    {
        get => _state;
        private set => SetProperty(ref _state, value);
    }

    /// <summary>Follows the map's scale while the tab shows: Pro calls this as the application idles.</summary>
    protected override void OnUpdate()
    {
        try
        {
            var view = MapView.Active;
            var below = view?.Camera is { } camera && ZoomFloor.IsBelow(camera.Scale, ChartLayerManager.Instance.SelectedMinScales(view));
            State = ScaleNoteState.For(below);
        }
        catch (Exception ex)
        {
            // The boundary with Pro, called on every idle: log once, not every time.
            if (!_failureLogged)
            {
                _failureLogged = true;
                AddInLog.Write(LogLevel.Error, $"Updating the SoundCharts scale note failed: {ex}");
            }
        }
    }
}
