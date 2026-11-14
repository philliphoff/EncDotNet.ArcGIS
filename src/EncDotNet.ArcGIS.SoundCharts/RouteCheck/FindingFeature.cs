using EncDotNet.S100.DataModel;

namespace EncDotNet.SoundCharts.RouteCheck;

/// <summary>
/// A route check finding as a feature of the findings layers: its geometry in
/// WGS-84 and the attribute values <see cref="FindingsSchema"/> names.
/// </summary>
/// <param name="Shape">A point or a segment.</param>
/// <param name="Path">The point (one position) or the stretch (two or more), in WGS-84.</param>
/// <param name="Severity">The viewer's group: <b>MUST REVIEW</b>, <b>CHECK</b> or <b>INFO</b>.</param>
/// <param name="SeverityRank">1 for MUST REVIEW, 2 for CHECK, 3 for INFO, for sorting and drawing order.</param>
/// <param name="Rule">The finding's code, such as <c>chart.danger_in_corridor</c>.</param>
/// <param name="Category">The viewer's tag: Route check, Clearance, S-124, Confidence, Weather or Data.</param>
/// <param name="Title">The finding's title, in the viewer's words.</param>
/// <param name="Message">The finding's detail line (where, how much), in the viewer's words.</param>
/// <param name="Features">The chart feature it's about, as <c>{dataset} {feature type} {feature id}</c>, or <c>null</c>.</param>
/// <param name="Leg">The number of the leg's first waypoint (leg 2 → 3 is 2), or <c>null</c>.</param>
/// <param name="DistanceNm">Distance along the route from departure, in nautical miles, or <c>null</c>.</param>
/// <param name="FindingId">The finding's id in the report, stable for the same route and data.</param>
public sealed record FindingFeature(
    FindingShape Shape,
    IReadOnlyList<GeoPosition> Path,
    string Severity,
    int SeverityRank,
    string Rule,
    string Category,
    string Title,
    string Message,
    string? Features,
    int? Leg,
    double? DistanceNm,
    string FindingId);
