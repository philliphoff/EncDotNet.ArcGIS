namespace EncDotNet.SoundCharts.RouteCheck;

/// <summary>The ship parameters a route check used, in metres, and whether each came from the route.</summary>
/// <param name="Draft">The deepest leg draft, or <c>null</c> without one.</param>
/// <param name="DraftFromRoute"><c>true</c> when the route gives a draft; otherwise it's the Options page's.</param>
/// <param name="UkcMargin">The largest required UKC, or <c>null</c> without one.</param>
/// <param name="UkcMarginFromRoute"><c>true</c> when the route gives a required UKC.</param>
/// <param name="SafetyContour">The safety contour.</param>
/// <param name="SafetyContourFromRoute"><c>true</c> when the route gives the safety contour.</param>
/// <param name="SafetyDepth">The safety depth.</param>
/// <param name="SafetyDepthFromRoute"><c>true</c> when the route gives the safety depth.</param>
/// <param name="HasDraft">
/// <c>true</c> when every leg has a draft, which the clearance check needs,
/// as the viewer's check does.
/// </param>
public sealed record RouteCheckParameters(
    double? Draft,
    bool DraftFromRoute,
    double? UkcMargin,
    bool UkcMarginFromRoute,
    double SafetyContour,
    bool SafetyContourFromRoute,
    double SafetyDepth,
    bool SafetyDepthFromRoute,
    bool HasDraft);
