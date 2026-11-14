namespace EncDotNet.SoundCharts.Library;

/// <summary>What <see cref="SoundChartsLibrary.ImportAsync"/> did.</summary>
/// <param name="Added">The sources added, in order.</param>
/// <param name="Problems">The paths not added, and why.</param>
public sealed record LibraryImport(IReadOnlyList<LibraryImportedSource> Added, IReadOnlyList<LibraryImportProblem> Problems)
{
    /// <summary>Gets the collection the sources went to, or <see langword="null"/> when nothing was added.</summary>
    public Guid? CollectionId => Added.Count > 0 ? Added[0].CollectionId : null;
}
