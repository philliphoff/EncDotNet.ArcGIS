namespace EncDotNet.SoundCharts.Settings;

/// <summary>
/// The unit depths are shown in, as the viewer's <b>Depth Unit</b>. Depths
/// are always stored in metres.
/// </summary>
public enum DepthUnit
{
    /// <summary>Metres (m), the default.</summary>
    Metres,

    /// <summary>Feet (ft).</summary>
    Feet,

    /// <summary>Fathoms and feet (fm/ft).</summary>
    FathomsFeet,

    /// <summary>Fathoms (fm).</summary>
    Fathoms,
}
