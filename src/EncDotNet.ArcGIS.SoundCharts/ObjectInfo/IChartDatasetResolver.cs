namespace EncDotNet.SoundCharts.ObjectInfo;

/// <summary>
/// Answers "which datasets are under this map?" for Object information: from
/// the visible layers of a map to the datasets their SoundCharts layers draw.
/// </summary>
/// <remarks>
/// <para>
/// A layer is a SoundCharts layer because the project's registry says so, not
/// because of its name or URL (design brief §3.4, D16). The registry and the
/// dataset set belong to the chart layer (A3) and the chart server (A2), so
/// Object information asks through this interface rather than keeping a
/// dataset model of its own.
/// </para>
/// <para>
/// Implementations ignore layers that aren't SoundCharts layers, and return
/// each dataset once, in the order the chart draws them.
/// </para>
/// </remarks>
public interface IChartDatasetResolver
{
    /// <summary>
    /// Returns the datasets drawn by the SoundCharts layers among
    /// <paramref name="layers"/>.
    /// </summary>
    /// <param name="layers">The visible layers of the map, top first.</param>
    /// <returns>The datasets, or an empty list when none of the layers is a SoundCharts layer.</returns>
    IReadOnlyList<ChartDataset> Resolve(IReadOnlyList<ChartLayerReference> layers);
}
