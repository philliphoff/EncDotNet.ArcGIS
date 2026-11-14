using System.IO;
using EncDotNet.S100.Collections;
using EncDotNet.S100.Collections.Indexing;
using EncDotNet.S100.Collections.Library;
using Microsoft.Extensions.Logging;

namespace EncDotNet.SoundCharts.Library;

/// <summary>
/// The SoundCharts Library (issue #14, design brief D9): EncDotNet.S100's
/// <see cref="CollectionLibrary"/> over local folders, exchange sets and
/// collection manifests (<c>.s100collection.json</c>), indexed without
/// loading any dataset, as the viewer's Library.
/// </summary>
/// <remarks>
/// <para>
/// The add-in keeps its own library under
/// <c>%LOCALAPPDATA%\SoundCharts\ArcGIS\library\</c>: <c>collections.json</c>
/// and the index cache. Sources are references; nothing is copied. Only local
/// sources are added: online catalogues, downloads and permits are out of
/// scope.
/// </para>
/// <para>Doesn't use the Pro API, so tests reach it without Pro.</para>
/// </remarks>
public sealed class SoundChartsLibrary : IDisposable
{
    /// <summary>The folder under the SoundCharts root that holds the library.</summary>
    public const string FolderName = "library";

    private readonly CollectionLibrary _library;

    /// <summary>Creates a library stored in <paramref name="root"/>.</summary>
    /// <param name="root">The folder for <c>collections.json</c> and the index cache.</param>
    /// <param name="probe">Reads loose dataset files; <see langword="null"/> recognizes only S-57 cells, without bounds.</param>
    /// <param name="logger">Logs store, cache and indexing failures; <see langword="null"/> for none.</param>
    /// <param name="timeProvider">The clock; <see langword="null"/> for the system clock.</param>
    public SoundChartsLibrary(string root, DatasetProbe? probe = null, ILogger? logger = null, TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        Root = root;
        var paths = new LibraryDataPaths(root);
        _library = new CollectionLibrary(
            CollectionIndexer.CreateDefault(probe),
            new CollectionLibraryOptions(paths.CollectionsFile, paths.IndexCacheDirectory),
            logger,
            timeProvider);
        _library.Changed += (_, e) => Changed?.Invoke(this, e);
    }

    /// <summary>Raised after any change to <see cref="Collections"/>, on the changing thread.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets the folder the library is stored in.</summary>
    public string Root { get; }

    /// <summary>Gets EncDotNet.S100's library this one wraps.</summary>
    public CollectionLibrary Inner => _library;

    /// <summary>Gets the collections, in order. The viewer's S-128 session collection never appears here.</summary>
    public IReadOnlyList<LibraryCollection> Collections => [.. _library.Collections.Where(c => !c.IsSession)];

    /// <summary>
    /// Loads <c>collections.json</c> and starts indexing in the background:
    /// cached indexes first, then a refresh of every source. Idempotent.
    /// </summary>
    public void Initialize() => _library.Initialize();

    /// <summary>Waits until no source is waiting to be indexed.</summary>
    public Task WhenIdle() => _library.WhenIdle();

    /// <summary>
    /// Adds <paramref name="paths"/> to the library: folders, exchange sets
    /// (folders, <c>CATALOG.XML</c>, <c>CATALOG.031</c>, <c>.zip</c>), loose
    /// dataset files and collection manifests. Paths already in the library,
    /// or missing, are reported, not added.
    /// </summary>
    /// <param name="paths">The paths, from a file dialog, a drop or <b>Open with SoundCharts</b>.</param>
    /// <param name="collectionId">
    /// The collection to add to, or <see langword="null"/> for a new one,
    /// named by <see cref="LibraryImportNames.NewCollectionName"/>.
    /// </param>
    /// <param name="cancellationToken">Cancels reading a collection manifest.</param>
    /// <returns>What was added, and what wasn't and why.</returns>
    public async Task<LibraryImport> ImportAsync(IEnumerable<string> paths, Guid? collectionId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);

        var added = new List<LibraryImportedSource>();
        var problems = new List<LibraryImportProblem>();
        var candidates = new List<(string Path, LibrarySourceDraft Draft)>();
        foreach (var raw in paths.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string path;
            try
            {
                path = Normalize(raw);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                problems.Add(new LibraryImportProblem(raw, LibraryImportProblemKind.NotFound));
                continue;
            }

            if (!File.Exists(path) && !Directory.Exists(path))
            {
                problems.Add(new LibraryImportProblem(path, LibraryImportProblemKind.NotFound));
                continue;
            }

            if (Contains(path) || candidates.Any(c => IsSameOrUnder(path, c.Path)))
            {
                problems.Add(new LibraryImportProblem(path, LibraryImportProblemKind.AlreadyInLibrary));
                continue;
            }

            var draft = LibrarySourceDraft.ForPath(LibrarySourceKinds.Classify(path), path);
            var error = await draft.LoadAsync(cancellationToken).ConfigureAwait(false);
            if (error is not null || !draft.CanBuild)
            {
                problems.Add(new LibraryImportProblem(path, LibraryImportProblemKind.Unreadable, error));
                continue;
            }

            candidates.Add((path, draft));
        }

        var target = collectionId is { } id && _library.Find(id) is { IsSession: false } ? id : (Guid?)null;
        var name = LibraryImportNames.NewCollectionName(candidates.Select(c => (c.Path, c.Draft.SuggestedName)).ToList());
        foreach (var (path, draft) in candidates)
        {
            var (collection, source) = draft.AddTo(_library, target, target is null ? name : null);
            target = collection;
            added.Add(new LibraryImportedSource(collection, source, draft.Kind, path));
        }

        return new LibraryImport(added, problems);
    }

    /// <summary>
    /// Gets whether <paramref name="path"/> is already in the library: under
    /// a folder source, or the path of an exchange set or manifest source.
    /// </summary>
    /// <param name="path">A full path.</param>
    public bool Contains(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var full = Normalize(path);
        return Collections
            .SelectMany(c => c.Sources)
            .Any(s => s.Definition switch
            {
                LocalFolderSource f => IsSameOrUnder(full, Normalize(f.Path)),
                ExchangeSetSource e => string.Equals(full, Normalize(e.Path), StringComparison.OrdinalIgnoreCase),
                LocalManifestSource m => string.Equals(full, Normalize(m.Path), StringComparison.OrdinalIgnoreCase),
                _ => false,
            });
    }

    /// <summary>Removes a source, or a whole collection when <paramref name="sourceId"/> is <see langword="null"/>. No file is deleted.</summary>
    /// <param name="collectionId">The collection.</param>
    /// <param name="sourceId">The source, or <see langword="null"/> for the collection.</param>
    /// <returns>Whether something was removed.</returns>
    public bool Remove(Guid collectionId, Guid? sourceId = null) =>
        sourceId is { } source ? _library.RemoveSource(collectionId, source) : _library.RemoveCollection(collectionId);

    /// <summary>Re-indexes a source, a collection, or everything.</summary>
    /// <param name="collectionId">The collection, or <see langword="null"/> for every collection.</param>
    /// <param name="sourceId">The source, or <see langword="null"/> for every source of the collection.</param>
    public void Refresh(Guid? collectionId = null, Guid? sourceId = null) => _library.Refresh(collectionId, sourceId);

    /// <summary>Renames a collection.</summary>
    /// <param name="collectionId">The collection.</param>
    /// <param name="name">The new name.</param>
    /// <returns>Whether the collection was renamed.</returns>
    public bool Rename(Guid collectionId, string name) => _library.RenameCollection(collectionId, name);

    /// <inheritdoc/>
    public void Dispose() => _library.Dispose();

    private static string Normalize(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    private static bool IsSameOrUnder(string path, string folder) =>
        string.Equals(path, folder, StringComparison.OrdinalIgnoreCase)
        || path.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
