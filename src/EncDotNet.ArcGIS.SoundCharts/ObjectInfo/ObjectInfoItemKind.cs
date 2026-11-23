namespace EncDotNet.SoundCharts.ObjectInfo;

/// <summary>Which glyph a row of the Object information list shows.</summary>
internal enum ObjectInfoItemKind
{
    /// <summary>A point, curve or area feature.</summary>
    Feature,

    /// <summary>A coverage value: an S-102 depth, an S-104 water level or an S-111 surface current.</summary>
    Coverage,

    /// <summary>A part of an S-124 navigational warning.</summary>
    NavigationalWarning,
}
