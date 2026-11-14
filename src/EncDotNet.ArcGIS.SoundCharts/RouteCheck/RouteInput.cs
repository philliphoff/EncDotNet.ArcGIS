using EncDotNet.S100.Navigation;

namespace EncDotNet.SoundCharts.RouteCheck;

/// <summary>
/// A route to check: its name, where it came from, and its passage plan with
/// whatever ship parameters the route itself carries.
/// </summary>
/// <param name="Name">The route's name, used for the findings group (<b>Route check · {name}</b>).</param>
/// <param name="Source">Where the route came from.</param>
/// <param name="Plan">
/// The passage plan as the route gives it. Legs keep their own draft and
/// required UKC where the route has them; <see cref="RouteCheckPlanning"/>
/// fills the gaps from the Options page.
/// </param>
public sealed record RouteInput(string Name, RouteSource Source, PassagePlan Plan)
{
    /// <summary>
    /// Gets the route's own safety contour in metres, or <c>null</c> when it
    /// gives none. S-421 and RTZ give it per leg; this is the deepest of them,
    /// since the check takes one value for the whole passage.
    /// </summary>
    public double? SafetyContour { get; init; }

    /// <summary>
    /// Gets the route's own safety depth in metres, or <c>null</c> when it
    /// gives none; the deepest of the legs' values.
    /// </summary>
    public double? SafetyDepth { get; init; }

    /// <summary>Gets the file the route was read from, or <c>null</c> for a feature or sketch.</summary>
    public string? Path { get; init; }
}
