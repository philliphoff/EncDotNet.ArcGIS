using System.Globalization;
using EncDotNet.S100;
using EncDotNet.S100.Crs.ProjNet;
using EncDotNet.S100.Datasets.Pipelines;
using EncDotNet.S100.Datasets.S101;
using EncDotNet.S100.Datasets.S104;
using EncDotNet.S100.Datasets.S111;
using EncDotNet.S100.Datasets.S57;
using EncDotNet.S100.Features;
using EncDotNet.S100.Hdf5.PureHdf;
using EncDotNet.S100.Pipelines;
using EncDotNet.S100.Pipelines.Vector;

namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>
/// Reads a dataset's footprint for the <b>Coverage</b> layer (issue #9,
/// design brief Â§3.2) with EncDotNet.S100: the declared coverage of a vector
/// dataset, and the bounds of a grid.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>S-57: the cell's <c>M_COVR</c> areas, through the library's
///   S-57 to S-101 translation, where they become <c>DataCoverage</c>.</item>
///   <item>S-101: the cell's <c>DataCoverage</c> surfaces.</item>
///   <item>S-1xx GML (S-122, S-124, S-127 and others): the
///   <c>DataCoverage</c> surfaces the processor exposes.</item>
///   <item>HDF5 grids (S-102, S-104, S-111): the bounds in the file's
///   metadata, and the first and last time step of a time series.</item>
/// </list>
/// <para>
/// Coverage marked "no coverage available" (<c>CATCOV</c> = 2) is left out,
/// as the library's own coverage resolver does. A dataset with no coverage
/// surface falls back to its bounding box. Runs on the caller's thread and
/// opens the dataset: call it off Pro's UI thread and the MCT.
/// </para>
/// </remarks>
public sealed class FootprintReader : IDisposable
{
    private const string DataCoverage = "DataCoverage";
    private const string CategoryOfCoverage = "categoryOfCoverage";

    private readonly IDatasetProcessorFactory _factory;
    private readonly bool _ownsFactory;
    private readonly ICrsTransformFactory _crs = new ProjNetCrsTransformFactory();

    /// <summary>Creates a reader that opens datasets with the catalogues bundled in EncDotNet.S100.</summary>
    public FootprintReader()
        : this(BundledDatasetProcessorFactory.Create(), ownsFactory: true)
    {
    }

    /// <summary>Creates a reader that opens non-ISO 8211 datasets with <paramref name="factory"/>.</summary>
    /// <param name="factory">Creates the dataset processors.</param>
    /// <param name="ownsFactory"><see langword="true"/> to dispose <paramref name="factory"/> with the reader.</param>
    public FootprintReader(IDatasetProcessorFactory factory, bool ownsFactory = false)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
        _ownsFactory = ownsFactory;
    }

    /// <summary>
    /// Reads <paramref name="dataset"/>'s footprint, or returns
    /// <see langword="null"/> when it has neither a coverage surface nor a
    /// valid bounding box. A dataset that can't be read falls back to the
    /// box Add SoundCharts Data found.
    /// </summary>
    /// <param name="dataset">The dataset.</param>
    /// <param name="error">Why the dataset's coverage couldn't be read, if it couldn't.</param>
    public DatasetFootprint? Read(ChartLayerDataset dataset, out string? error)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        error = null;
        var read = new Found();
        try
        {
            switch (dataset.Product)
            {
                case "S-57":
                    ReadS57(dataset.Path, read);
                    break;
                case "S-101":
                    ReadVector(S101Dataset.Open(dataset.Path), read);
                    break;
                default:
                    ReadWithProcessor(dataset.Path, read);
                    ReadTimeSeries(dataset, read);
                    break;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The library reads files the user chose; a malformed one can throw almost anything.
            error = ex.Message;
        }

        if (read.Polygons.Count > 0)
        {
            return new DatasetFootprint(dataset, read.Polygons, FootprintSource.Coverage, read.IssueDate, read.FirstTime, read.LastTime);
        }

        var bounds = read.Bounds is { IsValid: true } box ? box : dataset.Extent;
        return bounds is { IsValid: true } extent
            ? new DatasetFootprint(dataset, [FootprintPolygon.FromExtent(extent)], FootprintSource.Bounds, read.IssueDate, read.FirstTime, read.LastTime)
            : null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsFactory)
        {
            (_factory as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// Gets the coverage polygons of a vector dataset's features: each
    /// <c>DataCoverage</c> surface not marked "no coverage available".
    /// </summary>
    /// <param name="features">The dataset's features, with geometry.</param>
    public static IReadOnlyList<FootprintPolygon> CoverageOf(IEnumerable<Feature> features)
    {
        ArgumentNullException.ThrowIfNull(features);
        var polygons = new List<FootprintPolygon>();
        foreach (var feature in features)
        {
            if (feature.FeatureType != DataCoverage || feature.GeometryType != GeometryType.Surface || IsNoCoverage(feature.Attributes))
            {
                continue;
            }

            IEnumerable<SurfacePart> parts = feature.SurfaceParts.Count > 0
                ? feature.SurfaceParts
                : [new SurfacePart(feature.Coordinates, feature.InteriorRings)];
            polygons.AddRange(parts.Select(p => FootprintPolygon.TryCreate(p.ExteriorRing, p.InteriorRings)).OfType<FootprintPolygon>());
        }

        return polygons;
    }

    private static void ReadS57(string path, Found read)
    {
        read.IssueDate = S57DatasetHeader.Read(path)?.IssueDate;
        var s57 = S57Dataset.Open(path);
        var s101 = S57ToS101Translator.ForTarget(s57.TranslationTarget).Translate(s57);
        ReadVector(S101Dataset.FromDocument(s101), read);
    }

    private static void ReadVector(S101Dataset dataset, Found read) =>
        read.Polygons.AddRange(CoverageOf(new S101VectorSource(dataset).GetFeatures()));

    private void ReadWithProcessor(string path, Found read)
    {
        var processor = _factory.CreateProcessor(path);
        try
        {
            var metadata = processor.Metadata;
            if (metadata.GetGeographicExtent(_crs) is { } box)
            {
                read.Bounds = new GeoExtent(box.WestLongitude, box.SouthLatitude, box.EastLongitude, box.NorthLatitude);
            }

            if (metadata.TimeCoverage is { } time)
            {
                read.FirstTime = time.Start;
                read.LastTime = time.End;
            }

            foreach (var feature in processor.EnumerateFeatures().Where(f => f.FeatureType == DataCoverage))
            {
                if (processor.GetFeatureGeometryAt(feature.Ordinal) is { Primitive: S100GeometryType.Surface } geometry
                    && FootprintPolygon.TryCreate(geometry.ExteriorRing, geometry.InteriorRings) is { } polygon)
                {
                    read.Polygons.Add(polygon);
                }
            }
        }
        finally
        {
            (processor as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// Reads an S-104 or S-111 file's first and last time step from its
    /// HDF5 metadata, which the processor's lightweight metadata leaves out.
    /// </summary>
    private static void ReadTimeSeries(ChartLayerDataset dataset, Found read)
    {
        if (read.FirstTime is not null || dataset.Kind == ChartLayerKind.Chart)
        {
            return;
        }

        using var file = PureHdfFile.Open(dataset.Path);
        var metadata = dataset.Kind == ChartLayerKind.WaterLevel ? S104DatasetReader.ReadMetadata(file) : S111DatasetReader.ReadMetadata(file);
        if (metadata.TimeCoverage is { } time)
        {
            read.FirstTime = time.Start;
            read.LastTime = time.End;
        }
    }

    /// <summary>Whether a <c>DataCoverage</c> is marked "no coverage available" (S-57 <c>CATCOV</c> = 2).</summary>
    private static bool IsNoCoverage(IReadOnlyDictionary<string, object?> attributes)
    {
        if (!attributes.TryGetValue(CategoryOfCoverage, out var raw) || raw?.ToString() is not { Length: > 0 } text)
        {
            return false;
        }

        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code)
            ? code == 2
            : string.Equals(text, "noCoverageAvailable", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>What reading a dataset found.</summary>
    private sealed class Found
    {
        public List<FootprintPolygon> Polygons { get; } = [];

        public GeoExtent? Bounds { get; set; }

        public DateOnly? IssueDate { get; set; }

        public DateTime? FirstTime { get; set; }

        public DateTime? LastTime { get; set; }
    }
}
