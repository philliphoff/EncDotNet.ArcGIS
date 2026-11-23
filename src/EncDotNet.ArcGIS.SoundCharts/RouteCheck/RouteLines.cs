using EncDotNet.S100.DataModel;
using EncDotNet.S100.Navigation;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts.RouteCheck;

/// <summary>
/// Turns a line from the map, a selected feature or a drawn sketch, into a
/// route: each vertex is a waypoint and each pair of vertices a rhumb-line leg.
/// </summary>
public static class RouteLines
{
    /// <summary>Makes a route from a line's vertices, in WGS-84.</summary>
    /// <param name="name">The route's name.</param>
    /// <param name="source">Whether the line is a feature or a sketch.</param>
    /// <param name="vertices">
    /// The line's vertices in order. A multipart line's parts are joined end
    /// to start. Repeated vertices are dropped.
    /// </param>
    /// <returns>The route, with no ship parameters of its own.</returns>
    /// <exception cref="RouteFileException">The line has fewer than two distinct vertices.</exception>
    public static RouteInput FromLine(string name, RouteSource source, IReadOnlyList<GeoPosition> vertices)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(vertices);
        List<GeoPosition> waypoints = [];
        foreach (var vertex in vertices)
        {
            if (!double.IsFinite(vertex.Latitude) || !double.IsFinite(vertex.Longitude))
            {
                continue;
            }

            if (waypoints.Count == 0 || waypoints[^1] != vertex)
            {
                waypoints.Add(vertex);
            }
        }

        if (waypoints.Count < 2)
        {
            throw new RouteFileException(Strings.RouteCheck_TooFewWaypoints);
        }

        // Named as EncDotNet.S100 names S-421 legs: "Leg 1 → 2".
        var legs = new List<PassageLeg>(waypoints.Count - 1);
        for (var i = 0; i + 1 < waypoints.Count; i++)
        {
            legs.Add(new PassageLeg
            {
                Points = [waypoints[i], waypoints[i + 1]],
                Name = $"Leg {i + 1} → {i + 2}",
            });
        }

        return new RouteInput(name.Trim(), source, new PassagePlan(legs));
    }
}
