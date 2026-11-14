#if DEBUG
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using EncDotNet.SoundCharts.Features;
using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts.FeatureLayers;

/// <summary>
/// Debug builds only: when <see cref="EnvironmentVariable"/> names a
/// dataset, opens it through the SoundCharts plugin datasource as Pro
/// starts (after a delay, so Pro has finished starting) and logs each
/// table's count and a spatial search, to check the plugin is installed
/// and answers without clicking in Pro.
/// </summary>
internal static class FeaturesSelfCheck
{
    /// <summary>The environment variable naming the dataset to open.</summary>
    public const string EnvironmentVariable = "SOUNDCHARTS_DEBUG_FEATURES";

    /// <summary>Starts the check when the variable is set.</summary>
    public static void RunIfRequested()
    {
        if (Environment.GetEnvironmentVariable(EnvironmentVariable) is not { Length: > 0 } path)
        {
            return;
        }

        _ = Task.Delay(TimeSpan.FromSeconds(45)).ContinueWith(_ => QueuedTask.Run(() =>
        {
            try
            {
                using var store = new PluginDatastore(new PluginDatasourceConnectionPath(FeatureNames.PluginDatasourceId, new Uri(path)));
                var names = store.GetTableNames();
                AddInLog.Write($"Features self-check: {path}: {names.Count} table(s).");
                foreach (var name in names)
                {
                    using var table = store.OpenTable(name);
                    var count = table.GetCount();
                    if (table is FeatureClass featureClass)
                    {
                        var definition = featureClass.GetDefinition();
                        var extent = definition.GetExtent();
                        var half = EnvelopeBuilderEx.CreateEnvelope(extent.XMin, extent.YMin, extent.Center.X, extent.Center.Y, extent.SpatialReference);
                        var inHalf = featureClass.GetCount(new SpatialQueryFilter { FilterGeometry = half, SpatialRelationship = SpatialRelationship.Intersects });
                        var withWhere = featureClass.GetCount(new QueryFilter { WhereClause = "OBJECTID > 0" });
                        AddInLog.Write($"Features self-check: {name}: {definition.GetShapeType()} {count} row(s), {inHalf} in the south-west quarter, {withWhere} with OBJECTID > 0, {definition.GetFields().Count} field(s), {definition.GetSpatialReference()?.Name}.");
                    }
                    else
                    {
                        AddInLog.Write($"Features self-check: {name}: table, {count} row(s), {table.GetDefinition().GetFields().Count} field(s).");
                    }
                }
            }
            catch (Exception ex)
            {
                AddInLog.Write(LogLevel.Error, $"Features self-check failed: {ex}");
            }
        }), TaskScheduler.Default);
    }
}
#endif
