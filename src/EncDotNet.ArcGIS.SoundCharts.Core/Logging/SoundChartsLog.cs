using System.Globalization;

namespace EncDotNet.SoundCharts.Logging;

/// <summary>
/// The SoundCharts log: add-in events and every chart server's output, in
/// one list for the SoundCharts Log pane and in a daily file (design brief
/// §4.4, D8).
/// </summary>
/// <remarks>
/// <para>
/// Safe to call from any thread. Writing never throws: a full disk or a
/// locked file loses the line from the file, not the add-in, and an
/// <see cref="EntryAdded"/> handler that throws is ignored.
/// </para>
/// <para>
/// The list keeps the latest <see cref="Capacity"/> entries. The file is
/// <c>soundcharts-yyyyMMdd.log</c> in the log folder, named by the UTC date,
/// and files older than <see cref="RetentionDays"/> days are deleted when the
/// log is created.
/// </para>
/// </remarks>
public sealed class SoundChartsLog
{
    /// <summary>The <see cref="LogEntry.Source"/> of the add-in's own events.</summary>
    public const string AddInSource = "add-in";

    /// <summary>The number of days log files are kept.</summary>
    public const int RetentionDays = 14;

    private const string FilePrefix = "soundcharts-";
    private const string FileExtension = ".log";

    private readonly Lock _gate = new();
    private readonly Queue<LogEntry> _entries = new();
    private readonly TimeProvider _time;

    /// <summary>Creates a log that writes files to <paramref name="folder"/>.</summary>
    /// <param name="folder">The log folder, or <see langword="null"/> to keep entries in memory only.</param>
    /// <param name="time">The clock, or <see langword="null"/> for the system clock.</param>
    /// <param name="capacity">The number of entries kept in memory.</param>
    public SoundChartsLog(string? folder, TimeProvider? time = null, int capacity = 5000)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);

        Folder = folder;
        Capacity = capacity;
        _time = time ?? TimeProvider.System;
        PruneOldFiles();
    }

    /// <summary>Raised on the writing thread after an entry is added.</summary>
    public event EventHandler<LogEntry>? EntryAdded;

    /// <summary>Gets the log folder, or <see langword="null"/> when the log isn't written to disk.</summary>
    public string? Folder { get; }

    /// <summary>Gets the number of entries kept in memory.</summary>
    public int Capacity { get; }

    /// <summary>Gets the path of today's log file, or <see langword="null"/> when there's no folder.</summary>
    public string? CurrentFilePath => Folder is null ? null : FilePathFor(_time.GetUtcNow());

    /// <summary>Gets the entries in memory, oldest first.</summary>
    public IReadOnlyList<LogEntry> Snapshot()
    {
        lock (_gate)
        {
            return [.. _entries];
        }
    }

    /// <summary>Adds an entry.</summary>
    /// <param name="level">Its severity.</param>
    /// <param name="source">What it came from (<see cref="AddInSource"/>, or <c>chart {id}</c>).</param>
    /// <param name="message">The text.</param>
    public void Write(LogLevel level, string source, string message)
    {
        var entry = new LogEntry(_time.GetUtcNow(), level, source ?? AddInSource, message ?? string.Empty);
        lock (_gate)
        {
            _entries.Enqueue(entry);
            while (_entries.Count > Capacity)
            {
                _entries.Dequeue();
            }

            AppendToFile(entry);
        }

        try
        {
            EntryAdded?.Invoke(this, entry);
        }
        catch (Exception)
        {
            // A listener's failure must not reach the writer, which may be a
            // server's output thread; there is nowhere left to report it.
        }
    }

    /// <summary>Adds a Debug entry from the add-in.</summary>
    /// <param name="message">The text.</param>
    public void Debug(string message) => Write(LogLevel.Debug, AddInSource, message);

    /// <summary>Adds an Info entry from the add-in.</summary>
    /// <param name="message">The text.</param>
    public void Info(string message) => Write(LogLevel.Info, AddInSource, message);

    /// <summary>Adds a Warning entry from the add-in.</summary>
    /// <param name="message">The text.</param>
    public void Warning(string message) => Write(LogLevel.Warning, AddInSource, message);

    /// <summary>Adds an Error entry from the add-in.</summary>
    /// <param name="message">The text.</param>
    public void Error(string message) => Write(LogLevel.Error, AddInSource, message);

    /// <summary>Gets the log file for the UTC date of <paramref name="time"/>.</summary>
    private string? FilePathFor(DateTimeOffset time) => Folder is null
        ? null
        : Path.Combine(Folder, FilePrefix + time.UtcDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + FileExtension);

    private void AppendToFile(LogEntry entry)
    {
        if (FilePathFor(entry.Timestamp) is not { } path)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            File.AppendAllText(path, entry.ToText() + Environment.NewLine);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The entry is still in memory and the pane; the file loses it.
        }
    }

    private void PruneOldFiles()
    {
        if (Folder is null)
        {
            return;
        }

        try
        {
            if (!Directory.Exists(Folder))
            {
                return;
            }

            var oldest = _time.GetUtcNow().UtcDateTime.Date.AddDays(-RetentionDays);
            foreach (var file in Directory.EnumerateFiles(Folder, FilePrefix + "*" + FileExtension))
            {
                var stamp = Path.GetFileNameWithoutExtension(file)[FilePrefix.Length..];
                if (DateTime.TryParseExact(stamp, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                    && date < oldest)
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Old files stay until the next start.
        }
    }
}
