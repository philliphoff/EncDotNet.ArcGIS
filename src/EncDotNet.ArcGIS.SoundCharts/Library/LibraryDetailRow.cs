using EncDotNet.S100.Collections.Library;
using EncDotNet.SoundCharts.Themes;

namespace EncDotNet.SoundCharts.Library;

/// <summary>
/// A labelled value in the Library's details: EncDotNet.S100's
/// <see cref="LibraryDetailField"/>, or a status shown as a toned chip with
/// an optional note under it (Validation and Compatibility, look and feel
/// design §6).
/// </summary>
/// <param name="Label">The field's label.</param>
/// <param name="Value">The value; under a chip, the note that explains it. <see langword="null"/> for a chip alone.</param>
/// <param name="Chip">The status chip's word, such as <c>Valid</c>, or <see langword="null"/> for a plain value.</param>
/// <param name="Tone">The chip's tone.</param>
/// <param name="IsWorking">Whether the status is still being worked out: a spinner shows beside the chip.</param>
public sealed record LibraryDetailRow(
    string Label,
    string? Value,
    string? Chip = null,
    SoundChartsTone Tone = SoundChartsTone.Neutral,
    bool IsWorking = false)
{
    /// <summary>Gets a plain row for one of EncDotNet.S100's fields.</summary>
    /// <param name="field">The field.</param>
    /// <returns>The row.</returns>
    public static LibraryDetailRow From(LibraryDetailField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return new LibraryDetailRow(field.Label, field.Value);
    }
}
