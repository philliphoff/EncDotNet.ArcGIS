using EncDotNet.S100.Navigation;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts.RouteCheck;

/// <summary>
/// Maps route check findings to the features of the <b>Findings (points)</b>
/// and <b>Findings (segments)</b> layers (design brief §3.2, D14).
/// </summary>
/// <remarks>
/// A finding with no place on the route (a datum note, a forecast that ends
/// before arrival) has no feature; it's listed in the pane only.
/// </remarks>
public static class FindingFeatures
{
    private const double MetresPerNauticalMile = 1852.0;

    /// <summary>Maps findings to features, in the report's order (severity, then distance).</summary>
    /// <param name="findings">The report's findings.</param>
    /// <returns>One feature per finding that has a place.</returns>
    public static IReadOnlyList<FindingFeature> From(IEnumerable<RouteFinding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);
        List<FindingFeature> features = [];
        foreach (var finding in findings)
        {
            if (finding.Location is not { Path.Count: > 0 } location)
            {
                continue;
            }

            var shape = location.Path.Count == 1 ? FindingShape.Point : FindingShape.Segment;
            features.Add(new FindingFeature(
                shape,
                location.Path,
                SeverityOf(finding.Group),
                RankOf(finding.Group),
                finding.Code.Value,
                TagOf(finding),
                finding.Title,
                finding.Meta,
                FeatureText(finding.Feature),
                finding.LegIndex is { } leg ? leg + 1 : null,
                (finding.Distance?.TotalMetres ?? location.StartDistanceMeters) / MetresPerNauticalMile,
                finding.Id));
        }

        return features;
    }

    /// <summary>Gets the viewer's group label for a finding group: <b>MUST REVIEW</b>, <b>CHECK</b> or <b>INFO</b>.</summary>
    /// <param name="group">The group.</param>
    /// <returns>The label.</returns>
    public static string SeverityOf(RouteFindingGroup group) => group switch
    {
        RouteFindingGroup.MustReview => Strings.RouteCheck_GroupMustReview,
        RouteFindingGroup.Check => Strings.RouteCheck_GroupCheck,
        _ => Strings.RouteCheck_GroupInfo,
    };

    /// <summary>Gets a group's rank: 1 for MUST REVIEW, 2 for CHECK, 3 for INFO.</summary>
    /// <param name="group">The group.</param>
    /// <returns>The rank.</returns>
    public static int RankOf(RouteFindingGroup group) => group switch
    {
        RouteFindingGroup.MustReview => 1,
        RouteFindingGroup.Check => 2,
        _ => 3,
    };

    /// <summary>
    /// Gets the viewer's tag for a finding (<c>RouteFindingRowViewModel.TagOf</c>):
    /// Clearance, Route check, S-124, Confidence, Weather or Data.
    /// </summary>
    /// <param name="finding">The finding.</param>
    /// <returns>The tag.</returns>
    public static string TagOf(RouteFinding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);
        return finding.Category switch
        {
            RouteFindingCategory.Clearance => Strings.RouteCheck_TagClearance,
            RouteFindingCategory.Chart or RouteFindingCategory.Depth or RouteFindingCategory.Area => Strings.RouteCheck_TagRouteCheck,
            RouteFindingCategory.Warning => Strings.RouteCheck_TagS124,
            RouteFindingCategory.Confidence => Strings.RouteCheck_TagConfidence,
            RouteFindingCategory.Weather => Strings.RouteCheck_TagWeather,
            _ => Strings.RouteCheck_TagData,
        };
    }

    private static string? FeatureText(RouteFindingFeature? feature) =>
        feature is null
            ? null
            : string.Join(' ', new[] { feature.DatasetId, feature.FeatureType, feature.FeatureId }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
