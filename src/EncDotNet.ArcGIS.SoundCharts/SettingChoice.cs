namespace EncDotNet.SoundCharts;

/// <summary>One choice in a settings combo box: a value and its localized label.</summary>
/// <typeparam name="T">The value's type.</typeparam>
/// <param name="Value">The value.</param>
/// <param name="Label">The label shown.</param>
internal sealed record SettingChoice<T>(T Value, string Label)
{
    /// <inheritdoc/>
    public override string ToString() => Label;
}
