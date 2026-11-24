using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>What each <see cref="ChartLayerKind"/> holds, is called and where it sits.</summary>
public static class ChartLayerKinds
{
    /// <summary>
    /// Gets the kinds in drawing order, top first: the time layers are an
    /// overlay, so they sit above <b>Chart</b>; navigational warnings sit
    /// above <b>Weather</b>'s areas, and both under the water level and
    /// current symbols (design brief §3.1).
    /// </summary>
    public static IReadOnlyList<ChartLayerKind> TopFirst { get; } =
    [
        ChartLayerKind.SurfaceCurrents,
        ChartLayerKind.WaterLevel,
        ChartLayerKind.NavigationalWarnings,
        ChartLayerKind.Weather,
        ChartLayerKind.Chart,
    ];

    /// <summary>
    /// Gets the layer a dataset belongs to: <b>Water level</b> for S-104,
    /// <b>Surface currents</b> for S-111, <b>Navigational warnings</b> for
    /// S-124, <b>Weather</b> for any other dataset that varies with time, and
    /// <b>Chart</b> for the rest.
    /// </summary>
    /// <param name="product">The product specification, such as <c>S-104</c> or <c>TC</c>.</param>
    /// <param name="timeVarying">
    /// Whether the dataset has more than one time step, as
    /// <see cref="DatasetInspector"/> found it.
    /// </param>
    public static ChartLayerKind ForProduct(string product, bool timeVarying = false) => product switch
    {
        "S-104" => ChartLayerKind.WaterLevel,
        "S-111" => ChartLayerKind.SurfaceCurrents,
        "S-124" => ChartLayerKind.NavigationalWarnings,
        _ when timeVarying => ChartLayerKind.Weather,
        _ => ChartLayerKind.Chart,
    };

    /// <summary>
    /// Gets whether layers of <paramref name="kind"/> step through time steps
    /// with map time (design brief §6): <b>Water level</b>, <b>Surface
    /// currents</b> and <b>Weather</b>. <b>Navigational warnings</b> follow
    /// map time too, by each warning's validity rather than by steps
    /// (<see cref="FollowsMapTime"/>).
    /// </summary>
    /// <param name="kind">The kind.</param>
    public static bool IsTimeLayer(ChartLayerKind kind) => kind is not (ChartLayerKind.Chart or ChartLayerKind.NavigationalWarnings);

    /// <summary>Gets whether layers of <paramref name="kind"/> change with map time: every kind but <b>Chart</b>.</summary>
    /// <param name="kind">The kind.</param>
    public static bool FollowsMapTime(ChartLayerKind kind) => kind != ChartLayerKind.Chart;

    /// <summary>
    /// Gets the layer's name in Contents: <b>Chart</b>, <b>Water level</b>,
    /// <b>Surface currents</b>, <b>Weather</b> or <b>Navigational warnings</b>.
    /// </summary>
    /// <param name="kind">The kind.</param>
    public static string DisplayName(ChartLayerKind kind) => kind switch
    {
        ChartLayerKind.WaterLevel => Strings.Layer_WaterLevel,
        ChartLayerKind.SurfaceCurrents => Strings.Layer_SurfaceCurrents,
        ChartLayerKind.Weather => Strings.Layer_Weather,
        ChartLayerKind.NavigationalWarnings => Strings.Layer_NavigationalWarnings,
        _ => Strings.Layer_Chart,
    };

    /// <summary>
    /// Gets the kind's identifier in the layer marker: <c>chart</c>,
    /// <c>water-level</c>, <c>surface-currents</c>, <c>weather</c> or
    /// <c>navigational-warnings</c>.
    /// </summary>
    /// <param name="kind">The kind.</param>
    public static string Id(ChartLayerKind kind) => kind switch
    {
        ChartLayerKind.WaterLevel => "water-level",
        ChartLayerKind.SurfaceCurrents => "surface-currents",
        ChartLayerKind.Weather => "weather",
        ChartLayerKind.NavigationalWarnings => "navigational-warnings",
        _ => "chart",
    };

    /// <summary>Gets the kind with identifier <paramref name="id"/>, or <see langword="null"/>.</summary>
    /// <param name="id">The identifier from <see cref="Id"/>.</param>
    public static ChartLayerKind? FromId(string? id) => id switch
    {
        "chart" => ChartLayerKind.Chart,
        "water-level" => ChartLayerKind.WaterLevel,
        "surface-currents" => ChartLayerKind.SurfaceCurrents,
        "weather" => ChartLayerKind.Weather,
        "navigational-warnings" => ChartLayerKind.NavigationalWarnings,
        _ => null,
    };
}
