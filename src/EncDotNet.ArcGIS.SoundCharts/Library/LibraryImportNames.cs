using System.IO;

namespace EncDotNet.SoundCharts.Library;

/// <summary>Names the collection a Library import creates.</summary>
public static class LibraryImportNames
{
    /// <summary>
    /// Gets the name of the one collection several paths are added to: the
    /// suggested name of a single path (its folder or file name, or a
    /// manifest's own name), else the folder the paths share, else the first
    /// path's suggested name.
    /// </summary>
    /// <param name="candidates">Each path with the name EncDotNet.S100 suggests for it.</param>
    /// <returns>The name, or <see langword="null"/> when there are no paths.</returns>
    public static string? NewCollectionName(IReadOnlyList<(string Path, string SuggestedName)> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (candidates.Count == 0)
        {
            return null;
        }

        if (candidates.Count == 1)
        {
            return candidates[0].SuggestedName;
        }

        var parents = candidates
            .Select(c => Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(c.Path)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return parents is [{ } parent] && Path.GetFileName(parent) is { Length: > 0 } name
            ? name
            : candidates[0].SuggestedName;
    }
}
