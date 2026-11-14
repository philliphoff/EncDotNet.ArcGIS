using EncDotNet.S100.DataModel;

namespace EncDotNet.SoundCharts.RouteCheck;

/// <summary>A finding as the <b>SoundCharts Route check</b> pane lists it.</summary>
/// <param name="Id">The finding's id in the report.</param>
/// <param name="Group">The group heading: <b>MUST REVIEW</b>, <b>CHECK</b> or <b>INFO</b>.</param>
/// <param name="GroupRank">1, 2 or 3, the order of the groups.</param>
/// <param name="Title">The finding's title.</param>
/// <param name="Meta">The finding's detail line.</param>
/// <param name="Tag">The viewer's tag: Route check, Clearance, S-124 and so on.</param>
/// <param name="Tone">The row's tone.</param>
/// <param name="Path">Where the finding is, in WGS-84: one position or a stretch; empty when it has no place.</param>
public sealed record RouteFindingItem(
    string Id,
    string Group,
    int GroupRank,
    string Title,
    string Meta,
    string Tag,
    RouteCheckTone Tone,
    IReadOnlyList<GeoPosition> Path)
{
    /// <summary>Gets whether the finding has a place on the route to zoom to.</summary>
    public bool HasLocation => Path.Count > 0;

    /// <summary>
    /// Gets where Object information looks for the finding: the place, or the
    /// middle vertex of a stretch; <c>null</c> without a place.
    /// </summary>
    public GeoPosition? Position => Path.Count == 0 ? null : Path[Path.Count / 2];
}
