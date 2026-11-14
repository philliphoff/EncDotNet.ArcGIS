namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>
/// A dataset's footprint, one feature of the <b>Coverage</b> layer (design
/// brief §3.2): where the dataset has data, and what it is.
/// </summary>
/// <param name="Dataset">The dataset.</param>
/// <param name="Polygons">The covered areas, at least one.</param>
/// <param name="Source">Whether the polygons are the declared coverage or the bounds.</param>
/// <param name="IssueDate">The dataset's issue date, when it says one.</param>
/// <param name="FirstTime">For a time-varying dataset, its first time step (UTC).</param>
/// <param name="LastTime">For a time-varying dataset, its last time step (UTC).</param>
public sealed record DatasetFootprint(
    ChartLayerDataset Dataset,
    IReadOnlyList<FootprintPolygon> Polygons,
    FootprintSource Source,
    DateOnly? IssueDate = null,
    DateTime? FirstTime = null,
    DateTime? LastTime = null)
{
    /// <summary>Gets the footprint's bounding box.</summary>
    public GeoExtent Extent => GeoExtent.Union(Polygons.Select(p => (GeoExtent?)p.Extent)) ?? default;

    /// <summary>Gets whether a WGS-84 point is in one of the footprint's polygons.</summary>
    /// <param name="latitude">The latitude in degrees.</param>
    /// <param name="longitude">The longitude in degrees.</param>
    public bool Contains(double latitude, double longitude) => Polygons.Any(p => p.Contains(latitude, longitude));
}
