using EncDotNet.S100.Navigation;
using EncDotNet.S100.Quantities;

namespace EncDotNet.SoundCharts.RouteCheck;

/// <summary>
/// Puts a route and the Options page's ship parameters together into what
/// <see cref="RouteChecker"/> takes: a passage plan and check settings.
/// </summary>
/// <remarks>
/// The route's own values win, as in the viewer, where a route's legs carry
/// their draft and UKC and the mariner settings fill in the rest: a leg's
/// draft and required UKC come from the route where it gives them and from
/// the Options page otherwise; the safety contour and depth likewise.
/// </remarks>
public static class RouteCheckPlanning
{
    /// <summary>Prepares a check of <paramref name="route"/>.</summary>
    /// <param name="route">The route.</param>
    /// <param name="ship">The Options page's ship parameters.</param>
    /// <param name="now">The time to check at when the route has no departure time.</param>
    /// <returns>The plan, the settings, and where each parameter came from.</returns>
    public static PreparedRouteCheck Prepare(RouteInput route, ShipParameters ship, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(ship);

        var draft = ship.Draft is { } d && double.IsFinite(d) && d > 0 ? Depth.FromMetres(d) : (Depth?)null;
        var margin = ship.UkcMargin is { } m && double.IsFinite(m) && m >= 0 ? Depth.FromMetres(m) : (Depth?)null;
        var legs = route.Plan.Legs.Select(leg => new PassageLeg
        {
            Points = leg.Points,
            Geometry = leg.Geometry,
            PortLimit = leg.PortLimit,
            StarboardLimit = leg.StarboardLimit,
            Speed = leg.Speed,
            Draft = leg.Draft ?? draft,
            Squat = leg.Squat,
            RequiredUkc = leg.RequiredUkc ?? margin,
            Name = leg.Name,
        }).ToList();
        var plan = new PassagePlan(legs, route.Plan.Departure) { VesselLimits = route.Plan.VesselLimits };

        var safetyContour = route.SafetyContour ?? ship.SafetyContour;
        var safetyDepth = route.SafetyDepth ?? ship.SafetyDepth;
        var settings = new RouteCheckSettings
        {
            At = route.Plan.Departure ?? now,
            SafetyContour = Depth.FromMetres(safetyContour),
            SafetyDepth = Depth.FromMetres(safetyDepth),
        };

        var routeDraft = route.Plan.Legs.Any(l => l.Draft is not null);
        var routeUkc = route.Plan.Legs.Any(l => l.RequiredUkc is not null);
        var parameters = new RouteCheckParameters(
            Draft: routeDraft ? legs.Max(l => l.Draft?.TotalMetres) : draft?.TotalMetres,
            DraftFromRoute: routeDraft,
            UkcMargin: routeUkc ? legs.Max(l => l.RequiredUkc?.TotalMetres) : margin?.TotalMetres,
            UkcMarginFromRoute: routeUkc,
            SafetyContour: safetyContour,
            SafetyContourFromRoute: route.SafetyContour is not null,
            SafetyDepth: safetyDepth,
            SafetyDepthFromRoute: route.SafetyDepth is not null,
            HasDraft: legs.All(l => l.Draft is not null));
        return new PreparedRouteCheck(plan, settings, parameters);
    }
}
