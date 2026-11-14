using System.Security.Cryptography;
using System.Text;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts.RouteCheck;

/// <summary>
/// The findings feature classes in the project's <c>SoundCharts.gdb</c>
/// (design brief D15) and their layers: names, fields and field lengths.
/// </summary>
/// <remarks>
/// Each route has its own pair of feature classes, named from a hash of the
/// route name, so checking the same route again replaces its features
/// (design brief §3.2) while other routes' findings stay.
/// </remarks>
public static class FindingsSchema
{
    /// <summary>The project geodatabase's file name.</summary>
    public const string GeodatabaseName = "SoundCharts.gdb";

    /// <summary>The severity field: MUST REVIEW, CHECK or INFO. The layers are symbolized by it.</summary>
    public const string SeverityField = "Severity";

    /// <summary>The severity rank field: 1, 2 or 3.</summary>
    public const string SeverityRankField = "SeverityRank";

    /// <summary>The rule (finding code) field.</summary>
    public const string RuleField = "Rule";

    /// <summary>The category (viewer tag) field.</summary>
    public const string CategoryField = "Category";

    /// <summary>The title field.</summary>
    public const string TitleField = "Title";

    /// <summary>The message field: the finding's detail line.</summary>
    public const string MessageField = "Message";

    /// <summary>The referenced chart feature field.</summary>
    public const string FeaturesField = "Features";

    /// <summary>The leg field: the number of the leg's first waypoint.</summary>
    public const string LegField = "Leg";

    /// <summary>The distance-along-route field, in nautical miles.</summary>
    public const string DistanceField = "DistanceNM";

    /// <summary>The finding id field.</summary>
    public const string FindingIdField = "FindingId";

    /// <summary>The route name field.</summary>
    public const string RouteField = "Route";

    /// <summary>The field for when the route was checked (UTC).</summary>
    public const string CheckedField = "CheckedUtc";

    /// <summary>Gets the text fields and their lengths, in field order.</summary>
    public static IReadOnlyList<(string Name, int Length)> TextFields { get; } =
    [
        (SeverityField, 16),
        (RuleField, 64),
        (CategoryField, 32),
        (TitleField, 255),
        (MessageField, 1024),
        (FeaturesField, 255),
        (FindingIdField, 128),
        (RouteField, 255),
    ];

    /// <summary>Gets the group layer's name: <b>Route check · {route}</b>.</summary>
    /// <param name="routeName">The route's name.</param>
    /// <returns>The group name.</returns>
    public static string GroupName(string routeName) =>
        string.Format(System.Globalization.CultureInfo.CurrentCulture, Strings.RouteCheck_GroupLayerFormat, routeName);

    /// <summary>Gets a route's feature class name for a shape: <c>RouteCheck_{hash}_Points</c> or <c>_Segments</c>.</summary>
    /// <param name="routeName">The route's name.</param>
    /// <param name="shape">Points or segments.</param>
    /// <returns>A file geodatabase table name: letters, digits and underscores, starting with a letter.</returns>
    public static string FeatureClassName(string routeName, FindingShape shape)
    {
        ArgumentNullException.ThrowIfNull(routeName);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(routeName.Trim().ToUpperInvariant()));
        var suffix = shape == FindingShape.Point ? "Points" : "Segments";
        return $"RouteCheck_{Convert.ToHexStringLower(hash.AsSpan(0, 4))}_{suffix}";
    }

    /// <summary>Cuts a value to a text field's length.</summary>
    /// <param name="value">The value, or <c>null</c>.</param>
    /// <param name="length">The field's length.</param>
    /// <returns>The value, shortened with an ellipsis when it's too long; <c>null</c> stays <c>null</c>.</returns>
    public static string? Fit(string? value, int length)
    {
        if (value is null || value.Length <= length)
        {
            return value;
        }

        return length <= 1 ? value[..length] : string.Concat(value.AsSpan(0, length - 1), "…");
    }
}
