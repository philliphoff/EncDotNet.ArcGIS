using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>What each <see cref="ChartLayerKind"/> holds, is called and where it sits.</summary>
public static class ChartLayerKinds
{
    /// <summary>
    /// Gets the kinds in drawing order, top first: the time layers are an
    /// overlay, so they sit above <b>Chart</b> (design brief §3.1).
    /// </summary>
    public static IReadOnlyList<ChartLayerKind> TopFirst { get; } =
        [ChartLayerKind.SurfaceCurrents, ChartLayerKind.WaterLevel, ChartLayerKind.Chart];

    /// <summary>Gets the layer a dataset of <paramref name="product"/> belongs to.</summary>
    /// <param name="product">The product specification, such as <c>S-104</c>.</param>
    public static ChartLayerKind ForProduct(string product) => product switch
    {
        "S-104" => ChartLayerKind.WaterLevel,
        "S-111" => ChartLayerKind.SurfaceCurrents,
        _ => ChartLayerKind.Chart,
    };

    /// <summary>Gets the layer's name in Contents: <b>Chart</b>, <b>Water level</b> or <b>Surface currents</b>.</summary>
    /// <param name="kind">The kind.</param>
    public static string DisplayName(ChartLayerKind kind) => kind switch
    {
        ChartLayerKind.WaterLevel => Strings.Layer_WaterLevel,
        ChartLayerKind.SurfaceCurrents => Strings.Layer_SurfaceCurrents,
        _ => Strings.Layer_Chart,
    };

    /// <summary>Gets the kind's identifier in the layer marker: <c>chart</c>, <c>water-level</c> or <c>surface-currents</c>.</summary>
    /// <param name="kind">The kind.</param>
    public static string Id(ChartLayerKind kind) => kind switch
    {
        ChartLayerKind.WaterLevel => "water-level",
        ChartLayerKind.SurfaceCurrents => "surface-currents",
        _ => "chart",
    };

    /// <summary>Gets the kind with identifier <paramref name="id"/>, or <see langword="null"/>.</summary>
    /// <param name="id">The identifier from <see cref="Id"/>.</param>
    public static ChartLayerKind? FromId(string? id) => id switch
    {
        "chart" => ChartLayerKind.Chart,
        "water-level" => ChartLayerKind.WaterLevel,
        "surface-currents" => ChartLayerKind.SurfaceCurrents,
        _ => null,
    };
}
