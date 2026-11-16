namespace EncDotNet.SoundCharts.Themes;

/// <summary>
/// The semantic tones of the add-in's chips, message bars, cards and glyphs
/// (look and feel design §4.3).
/// </summary>
/// <remarks>
/// A tone is never the only cue: a toned element always shows a glyph and a
/// word too, and in High Contrast the tones draw no colour at all.
/// </remarks>
public enum SoundChartsTone
{
    /// <summary>No judgement: a plain chip or row. Pro's subdued text colour.</summary>
    Neutral,

    /// <summary>Information, INFO findings and forecasts. Pro's accent blue.</summary>
    Info,

    /// <summary>Meets the settings, or valid.</summary>
    Success,

    /// <summary>CHECK findings, warnings and partial data.</summary>
    Warning,

    /// <summary>MUST REVIEW findings, errors and failures.</summary>
    Danger,
}
