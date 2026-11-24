using System.IO;
using EncDotNet.S100.Collections;
using EncDotNet.S100.Collections.Library;

namespace EncDotNet.SoundCharts.Library;

/// <summary>Where a Library dataset's files are, for its actions.</summary>
public static class LibraryItemPaths
{
    /// <summary>
    /// Gets the dataset's own file (an S-57 or S-101 base cell, an HDF5 or
    /// GML file, an NHC advisory's forecast zip) or folder (an NHC advisory
    /// folder, issue #69, or a GRIB2 run, issue #73), or <see langword="null"/> when it's inside a
    /// <c>.zip</c> exchange set or not on this computer.
    /// </summary>
    /// <param name="item">The item.</param>
    public static string? DatasetFile(CollectionItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Location is LocalItemLocation { IsZip: false } local ? LibraryAvailabilityResolver.ResolvePath(local) : null;
    }

    /// <summary>
    /// Gets what <b>Add to Map</b> passes to Add SoundCharts Data: the
    /// dataset's file, advisory folder or run folder, or for a dataset inside a
    /// <c>.zip</c> exchange set the <c>.zip</c> itself, since only a whole
    /// zipped exchange set can be added.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The path, or <see langword="null"/> when the item isn't on this computer.</returns>
    public static string? AddToMapPath(CollectionItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Location is LocalItemLocation local ? LibraryAvailabilityResolver.ResolvePath(local) : null;
    }

    /// <summary>
    /// Gets the paths <b>Add to Map</b> adds for several items: each file
    /// once, and each <c>.zip</c> once however many of its datasets are chosen.
    /// </summary>
    /// <param name="items">The items.</param>
    public static IReadOnlyList<string> AddToMapPaths(IEnumerable<CollectionItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return [.. items.Select(AddToMapPath).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Gets what <b>Open Containing Folder</b> selects in File Explorer:
    /// the dataset's file, advisory folder or run folder, or the <c>.zip</c> that holds it.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The path, or <see langword="null"/> when the item isn't on this computer.</returns>
    public static string? FileToReveal(CollectionItem item) => AddToMapPath(item);

    /// <summary>Gets whether <paramref name="path"/> exists now.</summary>
    /// <param name="path">A file or folder path, or <see langword="null"/>.</param>
    public static bool Exists(string? path)
    {
        try
        {
            return path is not null && (File.Exists(path) || Directory.Exists(path));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException)
        {
            return false;
        }
    }
}
