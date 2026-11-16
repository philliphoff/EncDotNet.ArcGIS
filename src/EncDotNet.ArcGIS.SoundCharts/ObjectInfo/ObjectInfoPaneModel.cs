namespace EncDotNet.SoundCharts.ObjectInfo;

/// <summary>
/// The <b>SoundCharts Object information</b> pane's state machine, without
/// Pro or WPF: which <see cref="ObjectInfoView"/> it shows and the point it
/// was last asked about. The pane's view model holds one and shows
/// <see cref="View"/>.
/// </summary>
/// <remarks>
/// A pick moves the pane from any state to <see cref="ObjectInfoState.Working"/>
/// (or <see cref="ObjectInfoState.NoChartLayers"/>), then to
/// <see cref="ObjectInfoState.Results"/>, <see cref="ObjectInfoState.NothingHere"/>
/// or <see cref="ObjectInfoState.Failed"/>. Only the pane starts
/// <see cref="ObjectInfoState.Empty"/>.
/// </remarks>
internal sealed class ObjectInfoPaneModel
{
    /// <summary>Gets what the pane shows.</summary>
    public ObjectInfoView View { get; private set; } = ObjectInfoPresenter.Empty;

    /// <summary>
    /// Gets the point the pane was last asked about, which the map highlight
    /// marks; (0, 0) before the first click. A failure keeps the point.
    /// </summary>
    public (double Latitude, double Longitude) Point { get; private set; }

    /// <summary>Gets the text <b>Copy position</b> puts on the clipboard, or <c>null</c> when there's no position to copy.</summary>
    public string? PositionText => View.PositionText;

    /// <summary>Shows that the map has no SoundCharts layers at the click.</summary>
    /// <param name="latitude">The clicked latitude.</param>
    /// <param name="longitude">The clicked longitude.</param>
    /// <returns>The view to show.</returns>
    public ObjectInfoView ShowNoChartLayers(double latitude, double longitude) =>
        Show(ObjectInfoPresenter.NoChartLayers(latitude, longitude), (latitude, longitude));

    /// <summary>Shows that a pick is running.</summary>
    /// <param name="latitude">The clicked latitude.</param>
    /// <param name="longitude">The clicked longitude.</param>
    /// <returns>The view to show.</returns>
    public ObjectInfoView ShowWorking(double latitude, double longitude) =>
        Show(ObjectInfoPresenter.Working(latitude, longitude), (latitude, longitude));

    /// <summary>Shows a pick's result.</summary>
    /// <param name="report">The pick's result.</param>
    /// <returns>The view to show.</returns>
    public ObjectInfoView ShowReport(ObjectInfoReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return Show(ObjectInfoPresenter.Present(report), (report.Latitude, report.Longitude));
    }

    /// <summary>Shows that a pick failed.</summary>
    /// <returns>The view to show.</returns>
    public ObjectInfoView ShowFailed() => Show(ObjectInfoPresenter.Failed, Point);

    private ObjectInfoView Show(ObjectInfoView view, (double, double) point)
    {
        View = view;
        Point = point;
        return view;
    }
}
