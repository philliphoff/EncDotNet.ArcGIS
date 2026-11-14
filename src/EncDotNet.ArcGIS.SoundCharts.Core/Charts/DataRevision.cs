using System.Globalization;
using System.Text;

namespace EncDotNet.SoundCharts.Charts;

/// <summary>
/// The revision of a chart's datasets as files on disk: each file's path,
/// size and last write time, update files and exchange-set contents
/// included (design brief §5.4). These are the data inputs of the chart
/// server's own disk-cache fingerprint, so a new revision means new tiles.
/// </summary>
public static class DataRevision
{
    /// <summary>
    /// Gets the revision of <paramref name="sources"/> as canonical text: a
    /// line per file, <c>{PATH}|{size}|{last write UTC ticks}</c>, sorted by
    /// path ignoring case. A missing file gives <c>{PATH}|missing</c>.
    /// </summary>
    /// <remarks>
    /// A dataset file brings its S-57/S-101 update files with it: the files
    /// beside it with the same name and a three-digit extension
    /// (<c>US5MA1BO.001</c> for <c>US5MA1BO.000</c>). An exchange set or
    /// folder brings every file under it; an exchange-set catalogue file or
    /// <c>.zip</c> brings the files of its folder, or itself.
    /// </remarks>
    /// <param name="sources">The chart's sources.</param>
    public static string Of(IEnumerable<DatasetSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        var files = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in sources)
        {
            foreach (var file in FilesOf(source))
            {
                files.Add(file);
            }
        }

        var builder = new StringBuilder();
        foreach (var file in files)
        {
            builder.Append(file.ToUpperInvariant()).Append('|');
            try
            {
                var info = new FileInfo(file);
                if (info.Exists)
                {
                    builder.Append(info.Length.ToString(CultureInfo.InvariantCulture))
                        .Append('|')
                        .Append(info.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    builder.Append("missing");
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                builder.Append("unreadable");
            }

            builder.Append('\n');
        }

        return builder.ToString();
    }

    private static IEnumerable<string> FilesOf(DatasetSource source)
    {
        try
        {
            if (Directory.Exists(source.Path))
            {
                return Directory.EnumerateFiles(source.Path, "*", SearchOption.AllDirectories).ToList();
            }

            if (source.Kind == DatasetSourceKind.ExchangeSet
                && !source.Path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                && Path.GetDirectoryName(source.Path) is { } folder
                && Directory.Exists(folder))
            {
                return Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).ToList();
            }

            return [source.Path, .. UpdateFiles(source.Path)];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [source.Path];
        }
    }

    private static IEnumerable<string> UpdateFiles(string path)
    {
        if (Path.GetDirectoryName(path) is not { } folder || !Directory.Exists(folder) || !IsIso8211Base(path))
        {
            return [];
        }

        var stem = Path.GetFileNameWithoutExtension(path);
        return Directory.EnumerateFiles(folder, stem + ".*")
            .Where(file => !string.Equals(file, path, StringComparison.OrdinalIgnoreCase) && HasNumericExtension(file))
            .ToList();
    }

    private static bool IsIso8211Base(string path) => string.Equals(Path.GetExtension(path), ".000", StringComparison.Ordinal);

    private static bool HasNumericExtension(string path) =>
        Path.GetExtension(path) is { Length: 4 } extension && extension[1..].All(char.IsAsciiDigit);
}
