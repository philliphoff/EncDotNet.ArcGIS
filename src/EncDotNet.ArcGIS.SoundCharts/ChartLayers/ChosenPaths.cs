using System.Globalization;
using System.IO;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>
/// Reads the datasets in the files, exchange sets and folders the user chose
/// for <b>Add SoundCharts Data</b> or <b>Add as Feature Layers</b>.
/// </summary>
internal static class ChosenPaths
{
    /// <summary>
    /// Reads the datasets in each of <paramref name="paths"/>, in order. A
    /// path that can't be read becomes a warning, so it doesn't stop the
    /// others; a cancel stops them all.
    /// </summary>
    /// <param name="command">The command, for the log.</param>
    /// <param name="paths">Files, exchange sets or folders the user chose.</param>
    /// <param name="inspect">Reads one path: <see cref="DatasetInspector.InspectAsync"/>.</param>
    /// <param name="progress">Told the name of each path, then of each dataset, as it's read.</param>
    /// <param name="cancellationToken">Stops between datasets.</param>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
    public static async Task<List<DatasetInspection>> InspectAsync(
        string command,
        IReadOnlyList<string> paths,
        Func<string, IProgress<string>?, CancellationToken, Task<DatasetInspection>> inspect,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(inspect);

        var inspections = new List<DatasetInspection>();
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(Path.GetFileName(path.TrimEnd('\\', '/')));
            try
            {
                var inspection = await inspect(path, progress, cancellationToken).ConfigureAwait(false);
                AddInLog.Write(
                    $"{command}: {inspection.Input}: {inspection.Datasets.Count} dataset(s): " +
                    string.Join(", ", inspection.Datasets.Select(d => $"{d.Name} ({d.ProductAndEdition})")));
                inspections.Add(inspection);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One unreadable input mustn't stop the others.
                AddInLog.Write(LogLevel.Error, $"{command}: reading {path} failed: {ex}");
                inspections.Add(new DatasetInspection(
                    Path.GetFileName(path),
                    [],
                    [string.Format(CultureInfo.CurrentCulture, Strings.AddData_Unreadable, Path.GetFileName(path), ex.Message)]));
            }
        }

        return inspections;
    }

}
