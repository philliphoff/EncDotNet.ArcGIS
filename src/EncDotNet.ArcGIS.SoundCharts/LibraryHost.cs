using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Windows;
using EncDotNet.SoundCharts.Library;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Resources;
using EncDotNet.SoundCharts.Server;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The add-in's one SoundCharts Library (issue #14), created when first
/// used: by the <b>Library</b> pane, <b>Open with SoundCharts</b> &gt;
/// <b>Add to Library</b>, or a drop on the pane. Also holds this session's
/// validation results and the time steps read for the details pane.
/// </summary>
internal static class LibraryHost
{
    private static readonly Lazy<LibraryDatasetReader> LazyReader = new(() => new LibraryDatasetReader());

    private static readonly Lazy<SoundChartsLibrary> LazyLibrary = new(() =>
    {
        var root = Path.Combine(SoundChartsSession.Folders.Root, SoundChartsLibrary.FolderName);
        var library = new SoundChartsLibrary(root, LazyReader.Value.Probe);
        library.Initialize();
        AddInLog.Write($"SoundCharts Library opened: {library.Collections.Count} collection(s).");
        return library;
    });

    private static readonly ConcurrentDictionary<string, IReadOnlyList<DateTime>> TimeSteps = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the library, loading it and starting to index on first use.</summary>
    public static SoundChartsLibrary Library => LazyLibrary.Value;

    /// <summary>Gets the reader for loose datasets and time steps.</summary>
    public static LibraryDatasetReader Reader => LazyReader.Value;

    /// <summary>Gets this session's validation results.</summary>
    public static DatasetValidationStore Validations { get; } = new();

    /// <summary>Gets the validator, or <see langword="null"/> when no <c>s100</c> is installed.</summary>
    public static DatasetValidator? Validator =>
        ServerExecutable.LocateForCurrentMachine(SoundChartsSession.AddInFolder) is { } executable
            ? new DatasetValidator(executable.Path)
            : null;

    /// <summary>Gets the time steps read for <paramref name="path"/>, or <see langword="null"/> when not read yet.</summary>
    /// <param name="path">The dataset file.</param>
    public static IReadOnlyList<DateTime>? CachedTimeSteps(string path) => TimeSteps.TryGetValue(path, out var steps) ? steps : null;

    /// <summary>Reads, off the UI thread, the time steps of the dataset at <paramref name="path"/>, once per session.</summary>
    /// <param name="path">The dataset file.</param>
    public static async Task<IReadOnlyList<DateTime>> ReadTimeStepsAsync(string path)
    {
        if (CachedTimeSteps(path) is { } cached)
        {
            return cached;
        }

        var steps = await Task.Run(() => Reader.ReadTimeSteps(path)).ConfigureAwait(false);
        TimeSteps[path] = steps;
        return steps;
    }

    /// <summary>
    /// Adds <paramref name="paths"/> to the library and says what wasn't
    /// added. Opens the Library pane on what was.
    /// </summary>
    /// <param name="paths">Folders, exchange sets, datasets or collection manifests.</param>
    /// <param name="collectionId">The collection to add to, or <see langword="null"/> for a new one.</param>
    /// <returns>What was added.</returns>
    public static async Task<LibraryImport> AddAsync(IReadOnlyList<string> paths, Guid? collectionId = null)
    {
        var import = await Library.ImportAsync(paths, collectionId);
        foreach (var added in import.Added)
        {
            AddInLog.Write($"SoundCharts Library: added {added.Path} ({added.Kind}).");
        }

        foreach (var problem in import.Problems)
        {
            AddInLog.Write(LogLevel.Warning, $"SoundCharts Library: {problem.Path} not added ({problem.Kind}) {problem.Detail}".TrimEnd());
        }

        if (import.Problems.Count > 0)
        {
            var text = new StringBuilder(Strings.Library_ImportProblemsHeading).AppendLine().AppendLine();
            foreach (var problem in import.Problems)
            {
                text.Append("• ").AppendLine(problem.Describe());
            }

            MessageBox.Show(
                text.ToString(),
                Strings.Library_Caption,
                MessageBoxButton.OK,
                import.Added.Count > 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }

        if (import.CollectionId is { } collection)
        {
            LibraryPaneViewModel.Show(collection);
        }

        return import;
    }
}
