namespace EncDotNet.SoundCharts.Settings;

/// <summary>
/// The ECDIS display category (S-100 Part 9 §11.7), with the viewer's labels:
/// <b>Display Base</b>, <b>Standard</b>, <b>Other Information</b>, <b>All</b>.
/// </summary>
public enum DisplayCategory
{
    /// <summary>Display Base.</summary>
    DisplayBase,

    /// <summary>Standard, the default.</summary>
    Standard,

    /// <summary>Other Information.</summary>
    OtherInformation,

    /// <summary>All.</summary>
    All,
}
