using System.Globalization;
using System.Security.Cryptography;

namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>
/// The <b>Coverage</b> feature class (design brief §3.2, D15): one polygon
/// feature per dataset in the project's <c>SoundCharts.gdb</c>, with the
/// dataset's name, product, edition, issue date, path and, for time-varying
/// datasets, first and last time.
/// </summary>
public static class CoverageTable
{
    /// <summary>The file geodatabase in the project's home folder (D15).</summary>
    public const string GeodatabaseName = "SoundCharts.gdb";

    /// <summary>The prefix of each map's coverage feature class name.</summary>
    public const string FeatureClassPrefix = "Coverage_";

    /// <summary>The dataset's name, such as <c>US5MA1BO</c>.</summary>
    public const string NameField = "DatasetName";

    /// <summary>The product, such as <c>S-101</c>.</summary>
    public const string ProductField = "Product";

    /// <summary>The product edition, such as <c>2.0.0</c>; empty when the dataset says none.</summary>
    public const string EditionField = "Edition";

    /// <summary>The chart layer drawing the dataset: <c>chart</c>, <c>water-level</c>, <c>surface-currents</c> or <c>weather</c>.</summary>
    public const string LayerField = "ChartLayer";

    /// <summary>The dataset file's full path; identifies the feature.</summary>
    public const string PathField = "DatasetPath";

    /// <summary>The issue date, when the dataset says one.</summary>
    public const string IssueDateField = "IssueDate";

    /// <summary>A time-varying dataset's first time step, in UTC.</summary>
    public const string FirstTimeField = "FirstTime";

    /// <summary>A time-varying dataset's last time step, in UTC.</summary>
    public const string LastTimeField = "LastTime";

    /// <summary>Where the polygon came from: <c>coverage</c> or <c>bounds</c>.</summary>
    public const string SourceField = "FootprintSource";

    /// <summary>The length of the short text fields.</summary>
    public const int TextLength = 128;

    /// <summary>The length of <see cref="PathField"/>: long paths are allowed (A2).</summary>
    public const int PathLength = 2048;

    /// <summary>
    /// The Arcade label: the dataset's name on the first line, product and
    /// edition on the second (issue #9).
    /// </summary>
    public const string LabelExpression =
        $"$feature.{NameField} + TextFormatting.NewLine + $feature.{ProductField}"
        + $" + IIf(IsEmpty($feature.{EditionField}), '', ' ' + $feature.{EditionField})";

    /// <summary>Creates a new, unique coverage feature class name, such as <c>Coverage_1a2b3c4d</c>.</summary>
    public static string NewFeatureClassName() =>
        FeatureClassPrefix + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(4));

    /// <summary>
    /// Gets what a footprint's feature records about its dataset besides the
    /// path. A footprint is read again when this changes: the file at the
    /// path is another edition, or now draws in another layer.
    /// </summary>
    /// <param name="dataset">The dataset.</param>
    public static string KeyOf(ChartLayerDataset dataset)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        return KeyOf(dataset.Product, dataset.Edition, ChartLayerKinds.Id(dataset.Kind));
    }

    /// <summary>Gets the key of a feature from its field values; see <see cref="KeyOf(ChartLayerDataset)"/>.</summary>
    /// <param name="product">The <see cref="ProductField"/> value.</param>
    /// <param name="edition">The <see cref="EditionField"/> value.</param>
    /// <param name="layer">The <see cref="LayerField"/> value.</param>
    public static string KeyOf(string? product, string? edition, string? layer) =>
        $"{product}|{(string.IsNullOrEmpty(edition) ? null : edition)}|{layer}";

    /// <summary>Gets the attribute values of a footprint's feature, by field name.</summary>
    /// <param name="footprint">The footprint.</param>
    public static IReadOnlyDictionary<string, object?> ValuesOf(DatasetFootprint footprint)
    {
        ArgumentNullException.ThrowIfNull(footprint);
        var dataset = footprint.Dataset;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [NameField] = Truncate(dataset.Name, TextLength),
            [ProductField] = Truncate(dataset.Product, TextLength),
            [EditionField] = dataset.Edition is null ? null : Truncate(dataset.Edition, TextLength),
            [LayerField] = ChartLayerKinds.Id(dataset.Kind),
            [PathField] = dataset.Path,
            [IssueDateField] = footprint.IssueDate?.ToDateTime(TimeOnly.MinValue),
            [FirstTimeField] = footprint.FirstTime,
            [LastTimeField] = footprint.LastTime,
            [SourceField] = footprint.Source == FootprintSource.Coverage ? "coverage" : "bounds",
        };
    }

    /// <summary>Gets the where clause selecting one layer's footprints, for its label class.</summary>
    /// <param name="kind">The chart layer.</param>
    public static string WhereLayer(ChartLayerKind kind) => $"{LayerField} = '{ChartLayerKinds.Id(kind)}'";

    /// <summary>Gets the where clause selecting the footprint of the dataset at <paramref name="path"/>.</summary>
    /// <param name="path">The dataset's path.</param>
    public static string WherePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return string.Create(CultureInfo.InvariantCulture, $"{PathField} = '{path.Replace("'", "''", StringComparison.Ordinal)}'");
    }

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];
}
