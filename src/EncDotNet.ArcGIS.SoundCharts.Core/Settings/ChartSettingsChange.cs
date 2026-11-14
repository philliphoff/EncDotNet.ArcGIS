namespace EncDotNet.SoundCharts.Settings;

/// <summary>What a settings change means for SoundCharts layers (design brief §5.5).</summary>
public enum ChartSettingsChange
{
    /// <summary>Nothing the tiles show changed: a setting the server doesn't apply, or none.</summary>
    None,

    /// <summary>The palette changed: a new WMTS style, never a restart (D27).</summary>
    Restyle,

    /// <summary>A start-up option changed: new chart servers, and the layers repointed to them.</summary>
    Restart,
}
