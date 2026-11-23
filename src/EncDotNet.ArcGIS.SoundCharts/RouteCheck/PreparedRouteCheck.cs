using EncDotNet.S100.Navigation;

namespace EncDotNet.SoundCharts.RouteCheck;

/// <summary>A route check ready to run: what <see cref="RouteChecker.Check"/> takes, and what it was made from.</summary>
/// <param name="Plan">The passage plan, with the Options page's draft and UKC margin on the legs that had none.</param>
/// <param name="Settings">The check settings: safety contour and depth, and the time to check at.</param>
/// <param name="Parameters">The ship parameters used and where each came from, for the pane.</param>
public sealed record PreparedRouteCheck(PassagePlan Plan, RouteCheckSettings Settings, RouteCheckParameters Parameters);
