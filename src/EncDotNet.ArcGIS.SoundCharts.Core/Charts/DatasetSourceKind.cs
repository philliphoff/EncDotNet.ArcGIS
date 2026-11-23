namespace EncDotNet.SoundCharts.Charts;

/// <summary>What a <see cref="DatasetSource"/> path points at.</summary>
public enum DatasetSourceKind
{
    /// <summary>One dataset file, such as an S-101 <c>.000</c> or an S-104 <c>.h5</c>.</summary>
    Dataset,

    /// <summary>
    /// An exchange set: a folder holding a <c>CATALOG.XML</c> or
    /// <c>CATALOG.031</c>, the catalogue file itself, or an exchange-set
    /// <c>.zip</c>.
    /// </summary>
    ExchangeSet,

    /// <summary>A folder of datasets, searched recursively by the server.</summary>
    Folder,
}
