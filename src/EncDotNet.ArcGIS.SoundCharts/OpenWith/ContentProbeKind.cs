namespace EncDotNet.SoundCharts.OpenWith;

/// <summary>What <see cref="ContentProbe"/> found at a path.</summary>
public enum ContentProbeKind
{
    /// <summary>Nothing SoundCharts can draw was found, or the path doesn't exist.</summary>
    None,

    /// <summary>A dataset file whose content names an S-57 or S-1xx product.</summary>
    Dataset,

    /// <summary>
    /// An exchange set: a folder or <c>.zip</c> holding an exchange-set
    /// catalogue, or the catalogue file itself.
    /// </summary>
    ExchangeSet,

    /// <summary>A folder with at least one dataset in it or below it.</summary>
    Folder,
}
