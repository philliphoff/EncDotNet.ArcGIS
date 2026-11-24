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

        List<DatasetInspection>? inspections;
        try
        {
            inspections = await CancelableProgress.RunAsync(
                Strings.AddData_ProgressReading,
                (progress, cancellationToken) => ChosenPaths.InspectAsync("Add SoundCharts Data", paths, Inspector.Value.InspectAsync, progress, cancellationToken));
        }
        catch (Exception ex)
        {
            Fail(ex);
            return;
        }

        if (inspections is null)
        {
            // Canceled while reading: nothing is added, and there's nothing to report.
            AddInLog.Write("Add SoundCharts Data: canceled while reading the datasets.");
            return;
        }

        var warnings = inspections.SelectMany(i => i.Warnings).ToList();
        foreach (var warning in warnings)
        {
            AddInLog.Write(LogLevel.Warning, $"Add SoundCharts Data: {warning}");
        }

        var progressDialog = new ProgressDialog(Strings.AddData_ProgressStarting);
        progressDialog.Show();
        try
        {
            ChartLayerManager.AddOutcome? outcome = null;
            if (inspections.Any(i => i.Datasets.Count > 0))
            {
                outcome = await ChartLayerManager.Instance.AddAsync(map, inspections);
                if (outcome is { IsNewChart: true, DatasetCount: > 0 })
                {
                    await ChartLayerManager.ZoomToAsync(view, inspections.SelectMany(i => i.Datasets));
                }
            }

            progressDialog.Hide();
            Report(outcome, warnings);
        }
        catch (Exception ex)
        {
            progressDialog.Hide();
            Fail(ex);
        }
        finally
        {
            progressDialog.Dispose();
        }
    }

    private static void Fail(Exception ex)
    {
        AddInLog.Write(LogLevel.Error, $"Add SoundCharts Data failed: {ex}");
        MessageBox.Show(
            string.Format(CultureInfo.CurrentCulture, Strings.AddData_Failed, ex.Message),
            Strings.AddData_FileDialogTitle,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
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
