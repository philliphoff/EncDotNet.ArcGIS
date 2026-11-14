namespace EncDotNet.SoundCharts.RouteCheck;

/// <summary>What the <b>SoundCharts Route check</b> pane shows.</summary>
/// <param name="Heading">The route's name, or what the pane is waiting for.</param>
/// <param name="Parameters">The ship parameters the check used, or <c>null</c>.</param>
/// <param name="Title">The verdict, in the viewer's words, or <c>null</c> before a check.</param>
/// <param name="Subtitle">The line under the verdict, or <c>null</c>.</param>
/// <param name="Tone">The verdict's tone.</param>
/// <param name="Rows">The summary rows.</param>
/// <param name="Confidence">The clearance check's lower-confidence notes, or <c>null</c>.</param>
/// <param name="Items">The findings, in group order (MUST REVIEW, CHECK, INFO) and then along the route.</param>
/// <param name="Footer">The viewer's footer, "Not for navigation · computed from …", or <c>null</c> before a check.</param>
/// <param name="Problems">One line per dataset that couldn't be read.</param>
public sealed record RouteCheckView(
    string Heading,
    string? Parameters,
    string? Title,
    string? Subtitle,
    RouteCheckTone Tone,
    IReadOnlyList<RouteCheckSummaryRow> Rows,
    string? Confidence,
    IReadOnlyList<RouteFindingItem> Items,
    string? Footer,
    IReadOnlyList<string> Problems)
{
    /// <summary>Gets whether a check ran and found nothing to list.</summary>
    public bool IsEmpty => Footer is not null && Items.Count == 0;
}
