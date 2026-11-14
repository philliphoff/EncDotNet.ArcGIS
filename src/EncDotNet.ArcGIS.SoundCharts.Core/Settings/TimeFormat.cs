namespace EncDotNet.SoundCharts.Settings;

/// <summary>
/// How the add-in shows times (D28), with the viewer's labels:
/// <b>UTC</b> or <b>Local time</b>.
/// </summary>
public enum TimeFormat
{
    /// <summary>UTC, the default: <c>12 Nov 2026 14:00 UTC</c>.</summary>
    Utc,

    /// <summary>Local time, in the system culture.</summary>
    Local,
}
