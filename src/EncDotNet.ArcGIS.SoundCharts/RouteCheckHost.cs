using EncDotNet.SoundCharts.RouteCheck;

namespace EncDotNet.SoundCharts;

/// <summary>The add-in's shared route check parts: the check service and where ship parameters come from.</summary>
internal static class RouteCheckHost
{
    private static readonly Lazy<RouteCheckService> LazyService = new(() => new RouteCheckService());

    /// <summary>Gets the route check service, created on first use.</summary>
    public static RouteCheckService Service => LazyService.Value;

    /// <summary>
    /// Gets or sets where the ship parameters come from: the project's
    /// <b>SoundCharts</b> Options page (design brief §5.2, D24).
    /// </summary>
    public static Func<ShipParameters> Ship { get; set; } = () => ShipParameters.Default;
}
