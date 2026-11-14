namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>Where a dataset's footprint came from.</summary>
public enum FootprintSource
{
    /// <summary>
    /// The dataset's declared coverage: its <c>DataCoverage</c> surfaces
    /// (S-101, S-1xx GML) or <c>M_COVR</c> (S-57), less any marked "no
    /// coverage available".
    /// </summary>
    Coverage,

    /// <summary>
    /// The dataset's bounding box: an HDF5 grid's bounds, or the box of a
    /// dataset that declares no coverage surface.
    /// </summary>
    Bounds,
}
