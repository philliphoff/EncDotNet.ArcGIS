namespace EncDotNet.SoundCharts.OpenWith;

/// <summary>
/// The Catalog items <b>Open with SoundCharts</b> was last offered for:
/// the context menu sets them when it opens, and its commands act on them.
/// </summary>
/// <remarks>
/// Pro creates the menu and its commands separately from DAML, and a
/// command can't ask the menu that showed it what was right-clicked, so
/// the menu leaves its finding here.
/// </remarks>
public static class OpenWithTargets
{
    private static IReadOnlyList<ContentProbeResult> _current = [];

    /// <summary>Gets the items last offered, with chart data in each.</summary>
    public static IReadOnlyList<ContentProbeResult> Current => Volatile.Read(ref _current);

    /// <summary>Gets the paths of <see cref="Current"/>.</summary>
    public static IReadOnlyList<string> Paths => [.. Current.Select(t => t.Path)];

    /// <summary>Replaces the items offered.</summary>
    /// <param name="targets">The items with chart data; empty when the menu offered nothing.</param>
    public static void Set(IReadOnlyList<ContentProbeResult> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);
        Volatile.Write(ref _current, targets);
    }
}
