using EncDotNet.SoundCharts.Resources;
using EncDotNet.SoundCharts.Themes;

namespace EncDotNet.SoundCharts;

/// <summary>
/// What the scale note on the SoundCharts Layer tab shows: a glyph in a tone
/// and one line of text (look and feel design §5.7). When there's nothing to
/// say, the text is empty and the glyph is dimmed (§10 decision 11).
/// </summary>
/// <param name="Text">The note, or an empty string.</param>
/// <param name="Tone">The glyph's tone: <see cref="SoundChartsTone.Warning"/> with a note, <see cref="SoundChartsTone.Neutral"/> without.</param>
internal readonly record struct ScaleNoteState(string Text, SoundChartsTone Tone)
{
    /// <summary>Gets the idle state: no text and a dimmed glyph.</summary>
    public static ScaleNoteState Idle => new(string.Empty, SoundChartsTone.Neutral);

    /// <summary>Gets whether there's a note to show.</summary>
    public bool HasNote => Text.Length > 0;

    /// <summary>Gets the tooltip: the full note, or <see langword="null"/> for none.</summary>
    public string? ToolTip => HasNote ? Text : null;

    /// <summary>Gets the state for the map's scale.</summary>
    /// <param name="belowMinimumScale">Whether the map is below the minimum scale of a selected chart layer.</param>
    public static ScaleNoteState For(bool belowMinimumScale) => belowMinimumScale
        ? new(Strings.ScaleNote_ZoomIn, SoundChartsTone.Warning)
        : Idle;
}
