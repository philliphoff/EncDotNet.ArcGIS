using System.Globalization;
using System.Text;
using System.Windows;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.ChartLayers;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Resources;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>Add SoundCharts Data</b> after the user chose paths: reads each one's
/// datasets from their content, warns about what can't be drawn, and adds
/// the rest to the active map's SoundCharts group (design brief §2.1, D11).
/// </summary>
internal static class AddSoundChartsData
{
    /// <summary>The most warnings listed in the message; the rest are in the log.</summary>
    private const int MaxListedWarnings = 12;

    private static readonly Lazy<DatasetInspector> Inspector = new(() => new DatasetInspector(SoundChartsSession.Folders.ExchangeSets));

    /// <summary>Adds the datasets in <paramref name="paths"/> to <paramref name="view"/>'s map.</summary>
    /// <param name="view">The active map view.</param>
    /// <param name="paths">Files, exchange sets or folders the user chose.</param>
    public static async Task RunAsync(MapView view, IReadOnlyList<string> paths)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (paths.Count == 0 || view.Map is not { } map)
        {
            return;
        }

        var progress = new ProgressDialog(Strings.AddData_ProgressReading);
        progress.Show();
        try
        {
            var inspections = await Task.Run(() => InspectAsync(paths));
            var warnings = inspections.SelectMany(i => i.Warnings).ToList();
            foreach (var warning in warnings)
            {
                AddInLog.Write(LogLevel.Warning, $"Add SoundCharts Data: {warning}");
            }

            ChartLayerManager.AddOutcome? outcome = null;
            if (inspections.Any(i => i.Datasets.Count > 0))
            {
                outcome = await ChartLayerManager.Instance.AddAsync(map, inspections);
                if (outcome is { IsNewChart: true, DatasetCount: > 0 })
                {
                    await ChartLayerManager.ZoomToAsync(view, inspections.SelectMany(i => i.Datasets));
                }
            }

            progress.Hide();
            Report(outcome, warnings);
        }
        catch (Exception ex)
        {
            progress.Hide();
            AddInLog.Write(LogLevel.Error, $"Add SoundCharts Data failed: {ex}");
            MessageBox.Show(
                string.Format(CultureInfo.CurrentCulture, Strings.AddData_Failed, ex.Message),
                Strings.AddData_FileDialogTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            progress.Dispose();
        }
    }

    private static async Task<List<DatasetInspection>> InspectAsync(IReadOnlyList<string> paths)
    {
        var inspections = new List<DatasetInspection>();
        foreach (var path in paths)
        {
            try
            {
                var inspection = await Inspector.Value.InspectAsync(path).ConfigureAwait(false);
                AddInLog.Write(
                    $"Add SoundCharts Data: {inspection.Input}: {inspection.Datasets.Count} dataset(s): " +
                    string.Join(", ", inspection.Datasets.Select(d => $"{d.Name} ({d.ProductAndEdition})")));
                inspections.Add(inspection);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One unreadable input mustn't stop the others.
                AddInLog.Write(LogLevel.Error, $"Add SoundCharts Data: reading {path} failed: {ex}");
                inspections.Add(new DatasetInspection(
                    System.IO.Path.GetFileName(path),
                    [],
                    [string.Format(CultureInfo.CurrentCulture, Strings.AddData_Unreadable, System.IO.Path.GetFileName(path), ex.Message)]));
            }
        }

        return inspections;
    }

    private static void Report(ChartLayerManager.AddOutcome? outcome, IReadOnlyList<string> warnings)
    {
        var problems = (outcome?.Failures ?? []).Concat(warnings).ToList();
        if (problems.Count == 0 && outcome is { DatasetCount: > 0 })
        {
            // Success shows on the map; the log has the details.
            return;
        }

        var text = new StringBuilder();
        if (outcome is { DatasetCount: > 0 } added)
        {
            var count = added.DatasetCount == 1
                ? Strings.AddData_OneDataset
                : string.Format(CultureInfo.CurrentCulture, Strings.AddData_DatasetCount, added.DatasetCount);
            text.AppendLine(string.Format(CultureInfo.CurrentCulture, Strings.AddData_Added, count, added.GroupName));
        }
        else
        {
            text.AppendLine(Strings.AddData_NothingAdded);
        }

        if (problems.Count > 0)
        {
            text.AppendLine().AppendLine(Strings.AddData_WarningsHeading);
            foreach (var problem in problems.Take(MaxListedWarnings))
            {
                text.Append("• ").AppendLine(problem);
            }

            if (problems.Count > MaxListedWarnings)
            {
                text.AppendLine(string.Format(CultureInfo.CurrentCulture, Strings.AddData_MoreInLog, problems.Count - MaxListedWarnings));
            }
        }

        text.AppendLine().Append(Strings.AddData_NotForNavigation);
        MessageBox.Show(
            text.ToString(),
            Strings.AddData_FileDialogTitle,
            MessageBoxButton.OK,
            outcome is { DatasetCount: > 0 } ? MessageBoxImage.Warning : MessageBoxImage.Error);
    }
}
