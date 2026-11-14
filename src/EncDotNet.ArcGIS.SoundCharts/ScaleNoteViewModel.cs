using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.ChartLayers;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The scale note on the SoundCharts Layer tab (<c>SoundCharts_ScaleNote</c>,
/// design brief §2.2, D13): "Zoom in to see the chart" while the active map
/// is below the minimum scale of a selected chart layer, and nothing
/// otherwise.
/// </summary>
internal sealed class ScaleNoteViewModel : CustomControl
{
    private string _text = string.Empty;
    private bool _failureLogged;

    /// <summary>Gets the note, or an empty string when the chart is in scale.</summary>
    public string Text
    {
        get => _text;
        private set => SetProperty(ref _text, value);
    }

    /// <summary>Follows the map's scale while the tab shows: Pro calls this as the application idles.</summary>
    protected override void OnUpdate()
    {
        try
        {
            var view = MapView.Active;
            var below = view?.Camera is { } camera && ZoomFloor.IsBelow(camera.Scale, ChartLayerManager.Instance.SelectedMinScales(view));
            Text = below ? Strings.ScaleNote_ZoomIn : string.Empty;
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
