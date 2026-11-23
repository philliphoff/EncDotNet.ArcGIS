namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>
/// What a SoundCharts WMTS layer is made from: its marker and the chart
/// server endpoint it draws from. <see cref="ChartLayerDocument"/> turns it
/// into CIM.
/// </summary>
/// <param name="Marker">The layer's marker; its kind names the layer.</param>
/// <param name="CapabilitiesUri">The chart server's WMTS capabilities URL.</param>
/// <param name="Style">The WMTS style, a palette: <c>day</c>, <c>dusk</c> or <c>night</c>.</param>
/// <param name="MinScale">
/// The layer's minimum scale, the scale denominator of the server's minimum
/// zoom (D13), or 0 for none.
/// </param>
/// <param name="ServerVersion">The <c>s100</c> version for the credits, or <see langword="null"/>.</param>
public sealed record ChartLayerDefinition(
    ChartLayerMarker Marker,
    Uri CapabilitiesUri,
    string Style = ChartLayerDefinition.DefaultStyle,
    double MinScale = 0,
    string? ServerVersion = null)
{
    /// <summary>The Day palette's style; the chart layer manager passes the project's (A5).</summary>
    public const string DefaultStyle = "day";

    /// <summary>Gets the layer's kind.</summary>
    public ChartLayerKind Kind => Marker.Kind ?? throw new InvalidOperationException("A chart layer needs a layer marker, not a group marker.");

    /// <summary>Gets the layer's name in Contents.</summary>
    public string Name => ChartLayerKinds.DisplayName(Kind);
}
