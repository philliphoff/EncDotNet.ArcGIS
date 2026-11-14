using System.Globalization;
using System.Text;
using System.Windows;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.ChartLayers;
using EncDotNet.SoundCharts.FeatureLayers;
using EncDotNet.SoundCharts.Features;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Resources;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>Add as Feature Layers</b> (issue #15): opens each dataset in the
/// chosen paths through the SoundCharts plugin datasource and adds its
/// features as ordinary feature layers and tables, in a
/// <b>SoundCharts features · {dataset}</b> group outside the chart group
/// (design brief §3.2, D14). Pro symbolizes them with its defaults.
/// </summary>
/// <remarks>
/// <b>Add as Feature Layers</b> on the SoundCharts tab, <b>Open with
/// SoundCharts</b> &gt; <b>Add as Feature Layers</b> in Catalog, and the
/// Library pane (A12) call <see cref="RunAsync"/>. Datasets are opened by
/// the plugin's id, never by file extension.
/// </remarks>
internal static class AddSoundChartsFeatures
{
    private const int MaxListedWarnings = 12;

    private static readonly Lazy<DatasetInspector> Inspector = new(() => new DatasetInspector(SoundChartsSession.Folders.ExchangeSets));

    /// <summary>Adds the features of the datasets in <paramref name="paths"/> to <paramref name="view"/>'s map.</summary>
    /// <param name="view">The active map view.</param>
    /// <param name="paths">Files, exchange sets or folders the user chose.</param>
    public static async Task RunAsync(MapView view, IReadOnlyList<string> paths)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (paths.Count == 0 || view.Map is not { } map)
        {
            return;
        }

        var progress = new ProgressDialog(Strings.Features_ProgressReading);
        progress.Show();
        var warnings = new List<string>();
        var added = 0;
        try
        {
            var datasets = new List<ChartLayerDataset>();
            foreach (var path in paths)
            {
                try
                {
                    var inspection = await Inspector.Value.InspectAsync(path);
                    warnings.AddRange(inspection.Warnings);
                    datasets.AddRange(inspection.Datasets);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    AddInLog.Write(LogLevel.Error, $"Add as Feature Layers: reading {path} failed: {ex}");
                    warnings.Add(string.Format(CultureInfo.CurrentCulture, Strings.AddData_Unreadable, System.IO.Path.GetFileName(path), ex.Message));
                }
            }

            foreach (var dataset in datasets.DistinctBy(d => d.Path, StringComparer.OrdinalIgnoreCase))
            {
                if (!FeatureLayerPlan.Products.Contains(dataset.Product))
                {
                    warnings.Add(string.Format(CultureInfo.CurrentCulture, Strings.Features_NotFeatures, dataset.Name, dataset.Product));
                    continue;
                }

                if (await AddDatasetAsync(map, dataset, warnings))
                {
                    added++;
                }
            }

            progress.Hide();
            Report(added, warnings);
        }
        catch (Exception ex)
        {
            progress.Hide();
            AddInLog.Write(LogLevel.Error, $"Add as Feature Layers failed: {ex}");
            MessageBox.Show(
                string.Format(CultureInfo.CurrentCulture, Strings.AddData_Failed, ex.Message),
                Strings.Features_Title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            progress.Dispose();
        }
    }

    private static async Task<bool> AddDatasetAsync(Map map, ChartLayerDataset dataset, List<string> warnings)
    {
        DatasetTables tables;
        try
        {
            // The library reads the file on the thread pool; the plugin finds
            // it in the same cache when Pro opens the tables.
            tables = await Task.Run(() => DatasetTablesCache.Shared.Get(dataset.Path));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AddInLog.Write(LogLevel.Error, $"Add as Feature Layers: reading {dataset.Path} failed: {ex}");
            warnings.Add(string.Format(CultureInfo.CurrentCulture, Strings.Features_DatasetFailed, dataset.Name, ex.Message));
            return false;
        }

        var plan = FeatureLayerPlan.For(tables);
        if (plan.Members.Count == 0)
        {
            warnings.Add(string.Format(CultureInfo.CurrentCulture, Strings.Features_NoTables, dataset.Name));
            return false;
        }

        var marker = new FeatureGroupMarker(tables.Path, tables.Product);
        var failure = await QueuedTask.Run(() => AddLayers(map, tables.Path, plan, marker));
        if (failure is not null)
        {
            warnings.Add(failure);
            return false;
        }

        AddInLog.Write($"Add as Feature Layers: {plan.GroupName}: {plan.Members.Count} layer(s) and table(s) from {tables.Product} {tables.Path}.");
        return true;
    }

    // On the MCT. Returns a warning, or null when the layers were added.
    private static string? AddLayers(Map map, string path, FeatureLayerPlan plan, FeatureGroupMarker marker)
    {
        PluginDatastore store;
        try
        {
            store = new PluginDatastore(new PluginDatasourceConnectionPath(FeatureNames.PluginDatasourceId, new Uri(path)));
        }
        catch (Exception ex)
        {
            // Pro can't load the plugin when the .esriPlugin isn't installed.
            AddInLog.Write(LogLevel.Error, $"Add as Feature Layers: the SoundCharts features plugin didn't open {path}: {ex}");
            return string.Format(CultureInfo.CurrentCulture, Strings.Features_PluginMissing, System.IO.Path.GetFileNameWithoutExtension(path));
        }

        using (store)
        {
            GroupLayer? group = null;
            try
            {
                group = LayerFactory.Instance.CreateGroupLayer(map, 0, plan.GroupName);
                if (group.GetDefinition() is CIMGroupLayer cim)
                {
                    cim.CustomProperties =
                    [
                        new CIMStringMap { Key = ChartLayerMarker.PropertyKey, Value = marker.ToJson() },
                    ];
                    group.SetDefinition(cim);
                }

                foreach (var member in plan.Members)
                {
                    using var table = store.OpenTable(member.TableName);
                    if (member.IsSpatial && table is FeatureClass featureClass)
                    {
                        LayerFactory.Instance.CreateLayer<FeatureLayer>(new FeatureLayerCreationParams(featureClass) { Name = member.Name }, group);
                    }
                    else
                    {
                        StandaloneTableFactory.Instance.CreateStandaloneTable(new StandaloneTableCreationParams(table) { Name = member.Name }, group);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                AddInLog.Write(LogLevel.Error, $"Add as Feature Layers: adding {plan.GroupName} failed: {ex}");
                if (group is not null)
                {
                    map.RemoveLayer(group);
                }

                return string.Format(CultureInfo.CurrentCulture, Strings.Features_DatasetFailed, System.IO.Path.GetFileNameWithoutExtension(path), ex.Message);
            }
        }
    }

    private static void Report(int added, List<string> warnings)
    {
        if (warnings.Count == 0 && added > 0)
        {
            return;
        }

        var text = new StringBuilder();
        if (added > 0)
        {
            var count = added == 1 ? Strings.AddData_OneDataset : string.Format(CultureInfo.CurrentCulture, Strings.AddData_DatasetCount, added);
            text.AppendLine(string.Format(CultureInfo.CurrentCulture, Strings.Features_Added, count));
        }
        else
        {
            text.AppendLine(Strings.AddData_NothingAdded);
        }

        if (warnings.Count > 0)
        {
            text.AppendLine().AppendLine(Strings.AddData_WarningsHeading);
            foreach (var warning in warnings.Take(MaxListedWarnings))
            {
                AddInLog.Write(LogLevel.Warning, $"Add as Feature Layers: {warning}");
                text.Append("• ").AppendLine(warning);
            }

            if (warnings.Count > MaxListedWarnings)
            {
                text.AppendLine(string.Format(CultureInfo.CurrentCulture, Strings.AddData_MoreInLog, warnings.Count - MaxListedWarnings));
            }
        }

        MessageBox.Show(
            text.ToString(),
            Strings.Features_Title,
            MessageBoxButton.OK,
            added > 0 ? MessageBoxImage.Warning : MessageBoxImage.Error);
    }
}
