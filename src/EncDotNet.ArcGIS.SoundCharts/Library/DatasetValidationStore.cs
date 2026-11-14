using System.Collections.Concurrent;
using System.IO;

namespace EncDotNet.SoundCharts.Library;

/// <summary>
/// The validation status of each dataset validated this session, kept
/// while the file is unchanged: a new edition or update on disk shows
/// <b>Not validated</b> again.
/// </summary>
public sealed class DatasetValidationStore
{
    private readonly ConcurrentDictionary<string, (string Stamp, DatasetValidation Validation)> _results =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Raised when a dataset's status changes, with its path, on the changing thread.</summary>
    public event EventHandler<string>? Changed;

    /// <summary>Gets the status of the dataset at <paramref name="path"/>.</summary>
    /// <param name="path">The dataset file.</param>
    public DatasetValidation Get(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return _results.TryGetValue(path, out var entry) && entry.Stamp == Stamp(path)
            ? entry.Validation
            : DatasetValidation.NotValidated;
    }

    /// <summary>Records the status of the dataset at <paramref name="path"/>.</summary>
    /// <param name="path">The dataset file.</param>
    /// <param name="validation">Its status.</param>
    public void Set(string path, DatasetValidation validation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(validation);
        _results[path] = (Stamp(path), validation);
        Changed?.Invoke(this, path);
    }

    /// <summary>
    /// Validates the dataset at <paramref name="path"/> with
    /// <paramref name="validator"/>, showing <b>Validating…</b> meanwhile.
    /// </summary>
    /// <param name="path">The dataset file.</param>
    /// <param name="validator">Runs the validation.</param>
    /// <param name="cancellationToken">Stops it; the status goes back to what it was.</param>
    /// <returns>The outcome.</returns>
    public async Task<DatasetValidation> ValidateAsync(string path, DatasetValidator validator, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(validator);
        var before = Get(path);
        if (before.State == DatasetValidationState.Running)
        {
            return before;
        }

        Set(path, DatasetValidation.Running);
        try
        {
            var result = await validator.ValidateAsync(path, cancellationToken).ConfigureAwait(false);
            Set(path, result);
            return result;
        }
        catch (OperationCanceledException)
        {
            Set(path, before);
            throw;
        }
    }

    // The file's size and last write time: a different edition or a new update changes it.
    private static string Stamp(string path)
    {
        try
        {
            var file = new FileInfo(path);
            return file.Exists ? $"{file.Length}:{file.LastWriteTimeUtc.Ticks}:{UpdatesStamp(file)}" : string.Empty;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return string.Empty;
        }
    }

    // S-57 and S-101 updates (.001, .002, ...) beside a base cell are applied by validation.
    private static string UpdatesStamp(FileInfo file)
    {
        if (!file.Extension.Equals(".000", StringComparison.OrdinalIgnoreCase) || file.Directory is not { } folder)
        {
            return string.Empty;
        }

        var name = Path.GetFileNameWithoutExtension(file.Name);
        return folder.EnumerateFiles(name + ".*")
            .Where(f => f.Extension.Length == 4 && f.Extension[1..].All(char.IsAsciiDigit) && f.Extension != ".000")
            .Sum(f => f.Length + f.LastWriteTimeUtc.Ticks)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
