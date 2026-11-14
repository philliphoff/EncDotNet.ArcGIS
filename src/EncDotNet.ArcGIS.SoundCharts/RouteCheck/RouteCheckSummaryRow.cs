namespace EncDotNet.SoundCharts.RouteCheck;

/// <summary>A row of the verdict's summary, as the viewer's card shows it: <b>Clearance</b>, <b>Route</b>, <b>Warnings</b>, <b>Weather</b>.</summary>
/// <param name="Label">The row's label.</param>
/// <param name="Text">What the check found.</param>
/// <param name="Chip">A short count or state ("2 below", "tight"), or <c>null</c>.</param>
/// <param name="ChipTone">The chip's tone.</param>
public sealed record RouteCheckSummaryRow(string Label, string Text, string? Chip, RouteCheckTone ChipTone);
