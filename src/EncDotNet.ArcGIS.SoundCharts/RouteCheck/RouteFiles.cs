using System.IO;
using System.Xml;
using EncDotNet.S100.Datasets.S421;
using EncDotNet.S100.Datasets.S421.DataModel;
using EncDotNet.S100.Routes.Rtz;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts.RouteCheck;

/// <summary>
/// Reads S-421 and RTZ route files into a <see cref="RouteInput"/>, with
/// EncDotNet.S100's readers and passage-plan mappings, the ones the viewer
/// imports routes with.
/// </summary>
public static class RouteFiles
{
    /// <summary>
    /// Reads a route file. An <c>.rtz</c> file is read as RTZ; anything else
    /// (<c>.gml</c>, <c>.xml</c>) as S-421.
    /// </summary>
    /// <param name="path">The file's full path.</param>
    /// <returns>The route, named by the file's route name or, without one, its file name.</returns>
    /// <exception cref="RouteFileException">The file isn't a route, or has fewer than two waypoints.</exception>
    public static RouteInput Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return string.Equals(Path.GetExtension(path), ".rtz", StringComparison.OrdinalIgnoreCase)
            ? ReadRtz(path)
            : ReadS421(path);
    }

    private static RouteInput ReadRtz(string path)
    {
        RtzRoute route;
        try
        {
            route = RtzReader.Read(path);
        }
        catch (Exception ex) when (IsUnreadable(ex))
        {
            throw new RouteFileException(Strings.RouteCheck_NotARoute, ex);
        }

        if (route.Waypoints.Count < 2)
        {
            throw new RouteFileException(Strings.RouteCheck_TooFewWaypoints);
        }

        var legs = route.Waypoints.Select(w => w.Leg).Append(route.DefaultWaypoint?.Leg).OfType<RtzLeg>().ToList();
        return new RouteInput(NameOf(route.Info.RouteName, path), RouteSource.Rtz, route.ToPassagePlan())
        {
            SafetyContour = Deepest(legs.Select(l => l.SafetyContour)),
            SafetyDepth = Deepest(legs.Select(l => l.SafetyDepth)),
            Path = path,
        };
    }

    private static RouteInput ReadS421(string path)
    {
        S421Route route;
        try
        {
            route = S421RoutePlan.From(S421Dataset.Open(path), out _).Route;
        }
        catch (Exception ex) when (IsUnreadable(ex))
        {
            throw new RouteFileException(Strings.RouteCheck_NotARoute, ex);
        }

        if (route.Waypoints.Count < 2)
        {
            throw new RouteFileException(Strings.RouteCheck_TooFewWaypoints);
        }

        return new RouteInput(NameOf(route.Info?.Name, path), RouteSource.S421, route.ToPassagePlan())
        {
            SafetyContour = Deepest(route.Legs.Select(l => l.SafetyContour)),
            SafetyDepth = Deepest(route.Legs.Select(l => l.SafetyDepth)),
            Path = path,
        };
    }

    // The exceptions the viewer's importers turn into "not a route".
    private static bool IsUnreadable(Exception ex) =>
        ex is InvalidOperationException or InvalidDataException or IOException or XmlException or FormatException
            or ArgumentException or NotSupportedException;

    private static string NameOf(string? routeName, string path) =>
        string.IsNullOrWhiteSpace(routeName) ? StripExtensions(Path.GetFileName(path)) : routeName.Trim();

    // "RTE-TEST-GBASIC.s421.gml" names the route "RTE-TEST-GBASIC".
    private static string StripExtensions(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        return name.EndsWith(".s421", StringComparison.OrdinalIgnoreCase) ? name[..^5] : name;
    }

    private static double? Deepest(IEnumerable<double?> values)
    {
        double? deepest = null;
        foreach (var value in values)
        {
            if (value is { } metres && double.IsFinite(metres) && metres >= 0 && (deepest is null || metres > deepest))
            {
                deepest = metres;
            }
        }

        return deepest;
    }
}
