using System.IO;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using EncDotNet.SoundCharts.Features;

namespace EncDotNet.SoundCharts.PluginDatasource;

/// <summary>
/// The SoundCharts plugin datasource (<c>SoundCharts_FeaturesDatasource</c>
/// in Config.xml): one S-1xx dataset file as read-only tables, one per
/// feature type and geometry kind (<see cref="DatasetTables"/>).
/// </summary>
/// <remarks>
/// Pro makes an instance per thread and per opened data source; they share
/// the parsed dataset through <see cref="DatasetTablesCache.Shared"/>.
/// Public, as Pro's plugin factory creates it by reflection from
/// Config.xml.
/// </remarks>
public sealed class FeaturesPluginDatasource : PluginDatasourceTemplate
{
    private DatasetTables? _dataset;

    /// <inheritdoc/>
    /// <remarks>The connection path is the dataset file.</remarks>
    public override void Open(Uri connectionPath)
    {
        ArgumentNullException.ThrowIfNull(connectionPath);
        var path = connectionPath.LocalPath;
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The SoundCharts dataset doesn't exist.", path);
        }

        _dataset = DatasetTablesCache.Shared.Get(path);
    }

    /// <inheritdoc/>
    public override void Close() => _dataset = null;

    /// <inheritdoc/>
    public override PluginTableTemplate OpenTable(string name)
    {
        var dataset = Dataset;
        var table = dataset.Table(name) ?? throw new ArgumentException($"The SoundCharts dataset {dataset.Name} has no table {name}.", nameof(name));
        return new FeaturesPluginTable(table);
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> GetTableNames() => [.. Dataset.Tables.Select(t => t.Name)];

    /// <inheritdoc/>
    /// <remarks>
    /// No: Pro then evaluates where clauses itself on the rows a search
    /// returns.
    /// </remarks>
    public override bool IsQueryLanguageSupported() => false;

    /// <inheritdoc/>
    /// <remarks>
    /// Only for a dataset this plugin has already read: the plugin claims no
    /// file type (epic #1), so it never offers to open files Pro finds
    /// elsewhere, such as geoprocessing outputs.
    /// </remarks>
    public override bool CanOpen(Uri connectionPath) =>
        connectionPath is { IsFile: true } && DatasetTablesCache.Shared.Contains(connectionPath.LocalPath);

    /// <inheritdoc/>
    public override string GetDatasourceDescription(bool inPluralForm) => "SoundCharts features";

    /// <inheritdoc/>
    public override string GetDatasetDescription(DatasetType datasetType) => datasetType switch
    {
        DatasetType.FeatureClass => "SoundCharts feature class",
        DatasetType.Table => "SoundCharts table",
        _ => base.GetDatasetDescription(datasetType),
    };

    private DatasetTables Dataset => _dataset ?? throw new InvalidOperationException("The SoundCharts plugin datasource isn't open.");
}
