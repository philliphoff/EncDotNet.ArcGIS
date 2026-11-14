namespace EncDotNet.SoundCharts.Features;

/// <summary>
/// Keeps the most recently read <see cref="DatasetTables"/>s, so the
/// plugin datasource reads a dataset once however many instances Pro makes
/// of it (one per thread, and again for every layer and table).
/// </summary>
/// <remarks>
/// A dataset is read again when its file's size or modification time
/// changes. Reading runs once per key even when several threads ask at
/// the same time. Thread-safe.
/// </remarks>
public sealed class DatasetTablesCache
{
    private readonly object _gate = new();
    private readonly LinkedList<Entry> _entries = new();
    private readonly Func<string, DatasetTables> _open;

    /// <summary>Initializes a new instance of the <see cref="DatasetTablesCache"/> class.</summary>
    /// <param name="capacity">How many datasets to keep.</param>
    /// <param name="open">Reads a dataset; <see cref="DatasetTables.Open"/> when <see langword="null"/>.</param>
    public DatasetTablesCache(int capacity = 8, Func<string, DatasetTables>? open = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        Capacity = capacity;
        _open = open ?? (path => DatasetTables.Open(path));
    }

    /// <summary>Gets the process-wide cache the plugin datasource uses.</summary>
    public static DatasetTablesCache Shared { get; } = new();

    /// <summary>Gets how many datasets the cache keeps.</summary>
    public int Capacity { get; }

    /// <summary>Gets whether the dataset at <paramref name="path"/> has been read, successfully or still reading.</summary>
    /// <param name="path">The dataset file.</param>
    public bool Contains(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var full = Path.GetFullPath(path);
        lock (_gate)
        {
            // A failed read removes its entry, so any entry is a good or pending one.
            return _entries.Any(e => string.Equals(e.Path, full, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>Gets the dataset at <paramref name="path"/>, reading it if it isn't cached or has changed.</summary>
    /// <param name="path">The dataset file.</param>
    /// <exception cref="FileNotFoundException">There's no file at <paramref name="path"/>.</exception>
    public DatasetTables Get(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var file = new FileInfo(Path.GetFullPath(path));
        if (!file.Exists)
        {
            throw new FileNotFoundException("The dataset doesn't exist.", file.FullName);
        }

        var stamp = (file.Length, file.LastWriteTimeUtc);
        Lazy<DatasetTables> lazy;
        lock (_gate)
        {
            var node = _entries.First;
            while (node is not null && !string.Equals(node.Value.Path, file.FullName, StringComparison.OrdinalIgnoreCase))
            {
                node = node.Next;
            }

            if (node is not null && node.Value.Stamp == stamp)
            {
                _entries.Remove(node);
                _entries.AddFirst(node);
                lazy = node.Value.Dataset;
            }
            else
            {
                if (node is not null)
                {
                    _entries.Remove(node);
                }

                lazy = new Lazy<DatasetTables>(() => _open(file.FullName), LazyThreadSafetyMode.ExecutionAndPublication);
                _entries.AddFirst(new Entry(file.FullName, stamp, lazy));
                while (_entries.Count > Capacity)
                {
                    _entries.RemoveLast();
                }
            }
        }

        try
        {
            return lazy.Value;
        }
        catch
        {
            // Lazy keeps the exception; forget it so the next call tries again.
            lock (_gate)
            {
                var failed = _entries.FirstOrDefault(e => ReferenceEquals(e.Dataset, lazy));
                if (failed is not null)
                {
                    _entries.Remove(failed);
                }
            }

            throw;
        }
    }

    private sealed record Entry(string Path, (long Length, DateTime Modified) Stamp, Lazy<DatasetTables> Dataset);
}
