namespace EncDotNet.SoundCharts.Features;

/// <summary>A column of a SoundCharts feature table.</summary>
/// <param name="Name">
/// The field name: letters, digits and underscores, starting with a letter,
/// at most <see cref="FeatureNames.MaxFieldNameLength"/> characters. For an
/// attribute it's the attribute code, with a complex attribute's or
/// information type's sub-attributes joined by <c>_</c>
/// (<c>featureName_name</c>).
/// </param>
/// <param name="Alias">The field's display name, from the feature catalogue where it has one.</param>
/// <param name="Type">The field's type.</param>
/// <param name="Length">The longest value, for text fields; 0 otherwise.</param>
public sealed record FeatureField(string Name, string Alias, FeatureFieldType Type, int Length = 0);
