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

    /// <summary>Creates a source for one dataset file.</summary>
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
        return fileName.Equals("CATALOG.XML", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("CATALOG.031", StringComparison.OrdinalIgnoreCase)
            || System.IO.Path.GetExtension(fileName).Equals(".zip", StringComparison.OrdinalIgnoreCase)
            ? ExchangeSet(path)
            : Dataset(path);
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

    private static string Normalize(string path)
    {
        var full = System.IO.Path.GetFullPath(path);
        var root = System.IO.Path.GetPathRoot(full) ?? string.Empty;
        return full.Length > root.Length ? full.TrimEnd('\\', '/') : full;
    }
}
