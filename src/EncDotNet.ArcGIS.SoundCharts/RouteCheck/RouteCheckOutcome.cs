using EncDotNet.S100.Navigation;

namespace EncDotNet.SoundCharts.RouteCheck;

/// <summary>A finished route check.</summary>
/// <param name="Route">The route as it was read.</param>
/// <param name="Prepared">The plan and settings the check ran with.</param>
/// <param name="Report">EncDotNet.S100's report: verdict, findings and the clearance check.</param>
/// <param name="Products">The products the footer names ("S-101, S-102").</param>
/// <param name="Problems">One line per dataset that couldn't be read.</param>
public sealed record RouteCheckOutcome(
    RouteInput Route,
    PreparedRouteCheck Prepared,
    RouteCheckReport Report,
    IReadOnlyList<string> Products,
    IReadOnlyList<string> Problems);
