using EncDotNet.S100.Collections.Library;

namespace EncDotNet.SoundCharts.Library;

/// <summary>A source <see cref="SoundChartsLibrary.ImportAsync"/> added.</summary>
/// <param name="CollectionId">The collection it's in.</param>
/// <param name="SourceId">The new source.</param>
/// <param name="Kind">What the path was read as.</param>
/// <param name="Path">The full path.</param>
public sealed record LibraryImportedSource(Guid CollectionId, Guid SourceId, LibrarySourceKind Kind, string Path);
