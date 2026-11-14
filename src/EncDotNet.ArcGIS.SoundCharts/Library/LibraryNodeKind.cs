namespace EncDotNet.SoundCharts.Library;

/// <summary>What a row of the Library tree is.</summary>
public enum LibraryNodeKind
{
    /// <summary>A collection: a named group of sources.</summary>
    Collection,

    /// <summary>A source: a folder, exchange set or collection manifest.</summary>
    Source,

    /// <summary>A dataset the source's index lists.</summary>
    Dataset,
}
