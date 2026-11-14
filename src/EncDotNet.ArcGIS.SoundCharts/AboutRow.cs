namespace EncDotNet.SoundCharts;

/// <summary>One labelled value in the About window.</summary>
/// <param name="Label">The localized label.</param>
/// <param name="Value">The value, such as a version number.</param>
internal sealed record AboutRow(string Label, string Value);
