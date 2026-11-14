namespace EncDotNet.SoundCharts.ObjectInfo;

/// <summary>
/// The add-in's shared Object information parts: the identify service and the
/// resolver that finds the datasets under a map.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Resolver"/> is the seam for the chart layer (A3): when SoundCharts
/// layers and their project registry exist (design brief §3.4, D16), A3 sets it
/// to a resolver that reads the registry. Until then no layer is a SoundCharts
/// layer and the tool finds nothing.
/// </para>
/// <para>
/// A Debug build reads <see cref="FixedChartDatasetResolver.EnvironmentVariable"/>
/// at start-up, so the tool can be tried in Pro on any map before A3 lands.
/// Release builds, the ones that ship, don't.
/// </para>
/// </remarks>
internal static class ObjectInfoHost
{
    private static readonly Lazy<ObjectInfoService> LazyService = new(() => new ObjectInfoService());

    /// <summary>Gets the identify service, created on first use.</summary>
    public static ObjectInfoService Service => LazyService.Value;

    /// <summary>Gets or sets the resolver that finds the datasets of a map's SoundCharts layers.</summary>
    public static IChartDatasetResolver Resolver { get; set; } = DefaultResolver();

    private static IChartDatasetResolver DefaultResolver()
    {
#if DEBUG
        var debug = FixedChartDatasetResolver.FromPathList(
            Environment.GetEnvironmentVariable(FixedChartDatasetResolver.EnvironmentVariable));
        if (debug is not null)
        {
            AddInLog.Write($"Object information: Debug build, using {FixedChartDatasetResolver.EnvironmentVariable}.");
            return debug;
        }
#endif
        return NoChartDatasetResolver.Instance;
    }
}
