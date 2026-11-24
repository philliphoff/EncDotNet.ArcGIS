using System.Globalization;
using System.Text.RegularExpressions;

namespace EncDotNet.SoundCharts.MapTime;

/// <summary>
/// When the set of S-124 navigational warnings drawn can change, and the
/// <c>Time</c> segment the <b>Navigational warnings</b> layer asks its chart
/// server for at a map time (U7, issue #75).
/// </summary>
/// <remarks>
/// <para>
/// The chart server draws only the warnings in force at a time it's asked
/// for (EncDotNet.S100 #1525), and without a time draws every warning.
/// Between two consecutive <see cref="Changes"/> the same warnings are in
/// force, so the layer asks for the latest change at or before map time:
/// every map time between two changes has the same tile URLs, and Pro's
/// cache refetches only when the warnings in force change.
/// </para>
/// <para>
/// With S-124 alone the server's WMTS capabilities advertise no <c>Time</c>
/// dimension, so their tile template has no <c>{Time}</c> for Pro to fill.
/// The layer's connection therefore also carries a tile template with the
/// time written in (<see cref="TemplateUrl"/>), as well as the
/// <c>Time</c> dimension.
/// </para>
/// </remarks>
public sealed partial class WarningTimes
{
    /// <summary>
    /// The segment asked for before the first change: an instant long before
    /// any S-124 data, which draws what every time before the first change does.
    /// </summary>
    public static readonly DateTime BeforeAll = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly DateTime[] _changes;

    /// <summary>Creates the times from the instants at which the warnings drawn can change.</summary>
    /// <param name="changes">The instants, in any order; duplicates are dropped.</param>
    public WarningTimes(IEnumerable<DateTime> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        _changes = [.. changes.Select(TimeSteps.Utc).Where(c => c > BeforeAll).Distinct().Order()];
    }

    /// <summary>Gets times without changes: the same warnings draw at every time.</summary>
    public static WarningTimes None { get; } = new([]);

    /// <summary>Gets the instants (UTC) at which the warnings drawn can change, earliest first.</summary>
    public IReadOnlyList<DateTime> Changes => _changes;

    /// <summary>
    /// Gets the time to ask for at <paramref name="instant"/>: the latest
    /// change at or before it, or <see cref="BeforeAll"/> before the first.
    /// </summary>
    /// <param name="instant">The map instant.</param>
    public DateTime Representative(DateTime instant)
    {
        var at = TimeSteps.Utc(instant);
        var index = Array.BinarySearch(_changes, at);
        if (index >= 0)
        {
            return _changes[index];
        }

        var before = ~index - 1;
        return before >= 0 ? _changes[before] : BeforeAll;
    }

    /// <summary>Gets the <c>Time</c> segment to ask for at <paramref name="instant"/> (<see cref="Representative"/>).</summary>
    /// <param name="instant">The map instant.</param>
    public string Segment(DateTime instant) => FormatSegment(Representative(instant));

    /// <summary>
    /// Formats a time as a segment: ISO 8601 UTC to the second, such as
    /// <c>2026-10-12T00:00:00Z</c>, with seven fractional digits when it
    /// isn't a whole second (a validity range ends a tick after its end).
    /// </summary>
    /// <param name="time">The time.</param>
    public static string FormatSegment(DateTime time)
    {
        var utc = TimeSteps.Utc(time);
        return utc.Ticks % TimeSpan.TicksPerSecond == 0
            ? TimeSteps.FormatSegment(utc)
            : utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets the RESTful WMTS tile template for a chart server, as its
    /// capabilities write it, with <paramref name="segment"/> written in after
    /// the style: <c>.../wmts/1.0.0/s100/{Style}/2026-10-12T00:00:00Z/{TileMatrixSet}/{TileMatrix}/{TileRow}/{TileCol}.png</c>.
    /// Without a segment, it's the capabilities' template: every warning draws.
    /// </summary>
    /// <param name="capabilitiesUri">The server's WMTS capabilities URL, <c>.../wmts/1.0.0/WMTSCapabilities.xml</c>.</param>
    /// <param name="segment">The time segment, or <see langword="null"/> for none.</param>
    public static string TemplateUrl(Uri capabilitiesUri, string? segment)
    {
        ArgumentNullException.ThrowIfNull(capabilitiesUri);
        var root = new Uri(capabilitiesUri, ".").AbsoluteUri;
        return root + EncDotNet.SoundCharts.Server.ServerEndpoint.WmtsLayer + "/{Style}/"
            + (segment is null ? string.Empty : segment + "/")
            + "{TileMatrixSet}/{TileMatrix}/{TileRow}/{TileCol}.png";
    }

    /// <summary>
    /// Reads a tile template written by <see cref="TemplateUrl"/>, to carry
    /// its time over to a new connection when the layer is repointed (D20).
    /// </summary>
    /// <param name="templateUrl">The connection's template, or <see langword="null"/>.</param>
    /// <param name="segment">The time segment it carries, or <see langword="null"/> for none.</param>
    /// <returns>Whether the template has <see cref="TemplateUrl"/>'s form.</returns>
    public static bool TryReadTemplate(string? templateUrl, out string? segment)
    {
        segment = null;
        if (templateUrl is null || TemplatePattern().Match(templateUrl) is not { Success: true } match)
        {
            return false;
        }

        segment = match.Groups["time"].Success ? match.Groups["time"].Value : null;
        return true;
    }

    [GeneratedRegex(@"/wmts/1\.0\.0/s100/\{Style\}/(?:(?<time>[^/{}]+)/)?\{TileMatrixSet\}/\{TileMatrix\}/\{TileRow\}/\{TileCol\}\.png$", RegexOptions.CultureInvariant)]
    private static partial Regex TemplatePattern();
}
