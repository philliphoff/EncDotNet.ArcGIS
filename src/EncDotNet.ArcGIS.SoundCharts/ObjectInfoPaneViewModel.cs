using System.Collections.ObjectModel;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.ObjectInfo;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The <b>SoundCharts Object information</b> pane
/// (<c>SoundCharts_ObjectInfoPane</c> in Config.daml): every feature and
/// coverage value at the clicked point, grouped by dataset, with the selected
/// feature's attributes and associations. Selecting a feature highlights it on
/// the map.
/// </summary>
internal sealed class ObjectInfoPaneViewModel : DockPane
{
    private const string PaneId = "SoundCharts_ObjectInfoPane";

    private readonly ObjectInfoHighlight _highlight = new();
    private ObjectInfoView _view = ObjectInfoPresenter.Empty;
    private ObjectInfoItem? _selectedItem;
    private MapView? _mapView;
    private (double Latitude, double Longitude) _point;

    /// <summary>Gets the heading: the feature count, or what the pane is waiting for.</summary>
    public string Heading => _view.Heading;

    /// <summary>Gets the line under the heading, or <c>null</c>.</summary>
    public string? Description => _view.Description;

    /// <summary>Gets the clicked position, or <c>null</c>.</summary>
    public string? Location => _view.Location;

    /// <summary>Gets which time water levels and currents were sampled at, or <c>null</c>.</summary>
    public string? TimeNote => _view.TimeNote;

    /// <summary>Gets the features and coverage values, for a list grouped by <see cref="ObjectInfoItem.Group"/>.</summary>
    public ObservableCollection<ObjectInfoItem> Items { get; } = [];

    /// <summary>Gets one line per dataset that couldn't be read.</summary>
    public ObservableCollection<string> Problems { get; } = [];

    /// <summary>Gets or sets the selected feature, which the map highlights.</summary>
    public ObjectInfoItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            SetProperty(ref _selectedItem, value);
            NotifyPropertyChanged(nameof(HasSelection));
            _ = HighlightAsync(value);
        }
    }

    /// <summary>Gets whether a feature is selected.</summary>
    public bool HasSelection => _selectedItem is not null;

    /// <summary>Gets the pane's not-for-navigation strip text.</summary>
    public static string NotForNavigation => Strings.ObjectInfo_NotForNavigation;

    /// <summary>Opens and activates the pane; <c>null</c> when Pro can't find it.</summary>
    public static ObjectInfoPaneViewModel? Open()
    {
        var pane = FrameworkApplication.DockPaneManager.Find(PaneId) as ObjectInfoPaneViewModel;
        pane?.Activate(false);
        return pane;
    }

    /// <summary>Shows that the map has no SoundCharts layers at the click.</summary>
    public void ShowNoChartLayers(double latitude, double longitude) =>
        Show(ObjectInfoPresenter.NoChartLayers(latitude, longitude), mapView: null, (latitude, longitude));

    /// <summary>Shows that a pick is running.</summary>
    public void ShowWorking(double latitude, double longitude) =>
        Show(ObjectInfoPresenter.Working(latitude, longitude), mapView: null, (latitude, longitude));

    /// <summary>Shows a pick's result, and highlights its first feature in <paramref name="mapView"/>.</summary>
    public void ShowReport(ObjectInfoReport report, MapView mapView) =>
        Show(ObjectInfoPresenter.Present(report), mapView, (report.Latitude, report.Longitude));

    /// <summary>Shows that a pick failed.</summary>
    public void ShowFailed() => Show(ObjectInfoPresenter.Failed, mapView: null, _point);

    protected override void OnShow(bool isVisible)
    {
        // The highlight belongs to the pane: hiding the pane clears it.
        if (!isVisible)
        {
            _ = HighlightAsync(null);
        }
        else if (_selectedItem is not null)
        {
            _ = HighlightAsync(_selectedItem);
        }
    }

    private void Show(ObjectInfoView view, MapView? mapView, (double, double) point)
    {
        RunOnUiThread(() =>
        {
            _view = view;
            _mapView = mapView;
            _point = point;
            Items.Clear();
            foreach (var item in view.Items)
            {
                Items.Add(item);
            }

            Problems.Clear();
            foreach (var problem in view.Problems)
            {
                Problems.Add(problem);
            }

            NotifyPropertyChanged(nameof(Heading));
            NotifyPropertyChanged(nameof(Description));
            NotifyPropertyChanged(nameof(Location));
            NotifyPropertyChanged(nameof(TimeNote));
            SelectedItem = Items.FirstOrDefault();
        });
    }

    private async Task HighlightAsync(ObjectInfoItem? item)
    {
        try
        {
            if (item is null || _mapView is not { } view)
            {
                await _highlight.ClearAsync();
                return;
            }

            await _highlight.ShowAsync(view, item.Feature.Geometry, _point.Latitude, _point.Longitude);
        }
        catch (Exception ex)
        {
            // The boundary with Pro: a highlight failure is cosmetic.
            AddInLog.Write($"Object information highlight failed: {ex}");
        }
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = FrameworkApplication.Current.Dispatcher;
        if (dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.Invoke(action);
        }
    }
}
