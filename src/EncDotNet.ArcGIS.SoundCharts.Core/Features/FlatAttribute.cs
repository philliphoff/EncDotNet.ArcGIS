namespace EncDotNet.SoundCharts.Features;

/// <summary>One column of a flattened feature.</summary>
/// <param name="Path">The column's code path, such as <c>featureName_name</c>.</param>
/// <param name="Alias">The column's display name path, such as <c>Feature name: Name</c>.</param>
/// <param name="Text">The value, repeated values joined; <see langword="null"/> when empty.</param>
/// <param name="Date">The value as a date, when it is one value that is a date.</param>
/// <param name="IsRepeated">Whether <paramref name="Text"/> joins several values.</param>
public sealed record FlatAttribute(string Path, string Alias, string? Text, DateTime? Date, bool IsRepeated);
