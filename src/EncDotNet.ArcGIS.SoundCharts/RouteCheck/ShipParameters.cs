namespace EncDotNet.SoundCharts.RouteCheck;

/// <summary>
/// The ship parameters from the <b>SoundCharts</b> project Options page
/// (design brief §5.2, D24): what a route check uses where the route gives
/// none of its own. Depths are in metres.
/// </summary>
/// <param name="Draft">The draft, or <c>null</c> when none is set. Without a draft the clearance (UKC) check doesn't run.</param>
/// <param name="UkcMargin">The required under-keel clearance, or <c>null</c> when none is set (checked against 0 m).</param>
/// <param name="SafetyContour">The mariner's safety contour.</param>
/// <param name="SafetyDepth">The mariner's safety depth.</param>
public sealed record ShipParameters(double? Draft, double? UkcMargin, double SafetyContour, double SafetyDepth)
{
    /// <summary>
    /// Gets the parameters a project starts with: the viewer's safety contour
    /// and depth (30 m), and no draft or UKC margin.
    /// </summary>
    public static ShipParameters Default { get; } = new(null, null, 30.0, 30.0);
}
