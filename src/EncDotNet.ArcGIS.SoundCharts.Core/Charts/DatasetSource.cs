using EncDotNet.S100.Core;
using EncDotNet.S100.Datasets.Pipelines;
using EncDotNet.S100.Datasets.TropicalCyclones;

namespace EncDotNet.SoundCharts.Charts;

/// <summary>
/// One input of a chart: a dataset file, an exchange set or a folder of
/// datasets, by its full path.
/// </summary>
/// <remarks>
/// Two sources are equal when their kinds match and their paths match
/// ignoring case, as Windows paths do. The path is made absolute and loses
/// any trailing separator, so <c>C:\Charts\</c> and <c>c:\charts</c> are the
/// same source.
/// </remarks>
public sealed class DatasetSource : IEquatable<DatasetSource>
{
    private DatasetSource(string path, DatasetSourceKind kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown dataset source kind.");
        }

        Path = Normalize(path);
        Kind = kind;
    }

    /// <summary>Gets the full path of the dataset, exchange set or folder.</summary>
    public string Path { get; }

    /// <summary>Gets what <see cref="Path"/> points at.</summary>
    public DatasetSourceKind Kind { get; }

    /// <summary>Gets the file or folder name, for display.</summary>
    public string Name => System.IO.Path.GetFileName(Path) is { Length: > 0 } name ? name : Path;

    /// <summary>Creates a source for one dataset: a file, or a folder read as one dataset (<see cref="IsAdvisoryFolder"/>).</summary>
    /// <param name="path">The dataset's path.</param>
    public static DatasetSource Dataset(string path) => new(path, DatasetSourceKind.Dataset);

    /// <summary>Creates a source for an exchange set (folder, catalogue file or <c>.zip</c>).</summary>
    /// <param name="path">The exchange set's path.</param>
    public static DatasetSource ExchangeSet(string path) => new(path, DatasetSourceKind.ExchangeSet);

    /// <summary>Creates a source for a folder of datasets.</summary>
    /// <param name="path">The folder's path.</param>
    public static DatasetSource Folder(string path) => new(path, DatasetSourceKind.Folder);

    /// <summary>
    /// Creates a source for <paramref name="path"/>, choosing its kind from
    /// what is on disk: a folder with a <c>CATALOG.XML</c> or
    /// <c>CATALOG.031</c>, a catalogue file or a <c>.zip</c> is an exchange
    /// set, any other folder is a folder of datasets, and any other file is a
    /// dataset.
    /// </summary>
    /// <remarks>
    /// A <c>.zip</c> that EncDotNet.S100 reads as a dataset of its own, such
    /// as an NHC tropical cyclone advisory (<c>al092022_5day_020.zip</c>,
    /// product <c>TC</c>), is a dataset, not an exchange set (issue #63).
    /// </remarks>
    /// <param name="path">The path to classify.</param>
    public static DatasetSource FromPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (Directory.Exists(path))
        {
            return File.Exists(System.IO.Path.Combine(path, "CATALOG.XML")) || File.Exists(System.IO.Path.Combine(path, "CATALOG.031"))
                ? ExchangeSet(path)
                : Folder(path);
        }

        var fileName = System.IO.Path.GetFileName(path.TrimEnd('\\', '/'));
        if (IsZip(fileName))
        {
            return IsZippedDataset(path) ? Dataset(path) : ExchangeSet(path);
        }

        return fileName.Equals("CATALOG.XML", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("CATALOG.031", StringComparison.OrdinalIgnoreCase)
            ? ExchangeSet(path)
            : Dataset(path);
    }

    /// <summary>
    /// Gets whether <paramref name="path"/> is a <c>.zip</c> that
    /// EncDotNet.S100 reads as one dataset, such as an NHC tropical cyclone
    /// advisory's forecast zip, rather than as an exchange set.
    /// </summary>
    /// <remarks>
    /// The zips an advisory's forecast zip reads from beside it (its wind
    /// radii and best track zips) aren't datasets of their own, so they give
    /// <see langword="false"/>. A missing or unreadable file gives
    /// <see langword="false"/>.
    /// </remarks>
    /// <param name="path">The file's path.</param>
    public static bool IsZippedDataset(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!IsZip(path) || !File.Exists(path))
        {
            return false;
        }

        try
        {
            return DatasetPipelineFactory.DetectProductSpec(path) is not null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets whether <paramref name="path"/> is a folder EncDotNet.S100 reads
    /// as one tropical cyclone dataset: an NHC advisory folder, holding one
    /// storm's zips (<c>al092022_5day_020.zip</c> and its wind radii and best
    /// track zips), read as the storm's latest advisory.
    /// </summary>
    /// <remarks>
    /// The Library lists such a folder as one <c>TC</c> dataset located at
    /// the folder (issue #69). Other files in it, such as charts, are
    /// datasets of their own. A folder that is also a GRIB2 model run is read
    /// by EncDotNet.S100 as the run, so it gives <see langword="false"/>, and
    /// the Library lists the storm's forecast zip instead; the add-in reads
    /// GRIB2 runs file by file. A missing or unreadable folder gives
    /// <see langword="false"/>.
    /// </remarks>
    /// <param name="path">The folder's path.</param>
    public static bool IsAdvisoryFolder(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        try
        {
            return NhcAdvisoryFiles.IsAdvisoryFolder(path)
                && DatasetPipelineFactory.DetectProductSpec(path) == SpecName.TropicalCyclone;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public bool Equals(DatasetSource? other) =>
        other is not null
        && Kind == other.Kind
        && string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as DatasetSource);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Kind, StringComparer.OrdinalIgnoreCase.GetHashCode(Path));

    /// <inheritdoc/>
    public override string ToString() => $"{Kind}: {Path}";

    private static bool IsZip(string path) =>
        System.IO.Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string path)
    {
        var full = System.IO.Path.GetFullPath(path);
        var root = System.IO.Path.GetPathRoot(full) ?? string.Empty;
        return full.Length > root.Length ? full.TrimEnd('\\', '/') : full;
    }
}
