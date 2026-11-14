using EncDotNet.S100.DataModel;
using EncDotNet.S100.Features;
using EncDotNet.S100.Pipelines.Vector;
using EncDotNet.SoundCharts.ChartLayers;

namespace EncDotNet.SoundCharts.Tests.ChartLayers;

/// <summary>
/// Footprint extraction (issue #9): M_COVR and DataCoverage for vector
/// datasets, HDF5 bounds for grids, without Pro. The dataset tests use
/// EncDotNet.S100's test datasets when <see cref="Fixtures.UpstreamDatasetsVariable"/>
/// is set, and skip otherwise.
/// </summary>
public sealed class FootprintReaderTests : IDisposable
{
    private readonly TempFolder _temp = new();
    private readonly DatasetInspector _inspector;
    private readonly FootprintReader _reader = new();

    public FootprintReaderTests()
    {
        _inspector = new DatasetInspector(Path.Combine(_temp.Path, "extracted"));
    }

    public void Dispose()
    {
        _reader.Dispose();
        _inspector.Dispose();
        _temp.Dispose();
    }

    [Fact]
    public async Task S57_FootprintIsTheCellsMCovr()
    {
        var dataset = await InspectAsync("S57/US5MA1BO/US5MA1BO.000");

        var footprint = _reader.Read(dataset, out var error);

        Assert.Null(error);
        Assert.NotNull(footprint);
        Assert.Equal(FootprintSource.Coverage, footprint.Source);
        Assert.Same(dataset, footprint.Dataset);
        Assert.NotNull(footprint.IssueDate);

        // US5MA1BO covers W -70.35 S 41.25 E -70.275 N 41.325 (A3's finding).
        var extent = footprint.Extent;
        Assert.InRange(extent.West, -70.36, -70.34);
        Assert.InRange(extent.East, -70.28, -70.27);
        Assert.InRange(extent.South, 41.24, 41.26);
        Assert.InRange(extent.North, 41.32, 41.33);
        Assert.True(footprint.Contains(41.29, -70.31));
        Assert.False(footprint.Contains(32.75, -79.9));
    }

    [Fact]
    public async Task S101_FootprintIsTheCellsDataCoverage()
    {
        var dataset = await InspectAsync("S101/S-101/DATASET_FILES/101AA00DS0003.000");

        var footprint = _reader.Read(dataset, out var error);

        Assert.Null(error);
        Assert.NotNull(footprint);
        Assert.Equal(FootprintSource.Coverage, footprint.Source);
        Assert.True(footprint.Extent.IsValid);
        Assert.All(footprint.Polygons, p => Assert.True(p.ExteriorRing.Count >= 3));
    }

    [Theory]
    [InlineData("S102/102US004MI1CI262227.h5", false)]
    [InlineData("S104/104US004SC1BO_20251217T12Z.h5", true)]
    [InlineData("S111/111US00_DBOFS_20260320T18Z_US4DE1BB.h5", true)]
    public async Task Grid_FootprintIsItsHdf5Bounds(string relativePath, bool timeVarying)
    {
        var dataset = await InspectAsync(relativePath);

        var footprint = _reader.Read(dataset, out var error);

        Assert.Null(error);
        Assert.NotNull(footprint);
        Assert.Equal(FootprintSource.Bounds, footprint.Source);
        var polygon = Assert.Single(footprint.Polygons);
        Assert.Equal(5, polygon.ExteriorRing.Count);
        Assert.True(footprint.Extent.IsValid);
        if (timeVarying)
        {
            Assert.NotNull(footprint.FirstTime);
            Assert.NotNull(footprint.LastTime);
            Assert.True(footprint.FirstTime <= footprint.LastTime);
        }
    }

    [Fact]
    public async Task S104Charleston_IsFarFromS57Nantucket()
    {
        // Step 0: the union of these two is empty ocean; each footprint is its own.
        var cell = _reader.Read(await InspectAsync("S57/US5MA1BO/US5MA1BO.000"), out _);
        var tide = _reader.Read(await InspectAsync("S104/104US004SC1BO_20251217T12Z.h5"), out _);

        Assert.NotNull(cell);
        Assert.NotNull(tide);
        var union = GeoExtent.Union([cell.Extent, tide.Extent]);
        Assert.NotNull(union);
        var (latitude, longitude) = ((union.Value.South + union.Value.North) / 2, (union.Value.West + union.Value.East) / 2);
        Assert.False(cell.Contains(latitude, longitude));
        Assert.False(tide.Contains(latitude, longitude));
    }

    [Fact]
    public void NoCoverage_FallsBackToTheBoxAddFound()
    {
        var path = Path.Combine(_temp.Path, "US5XX1XX.000");
        File.WriteAllText(path, "not an ISO 8211 file");
        var dataset = new ChartLayerDataset(path, "S-57", null, new GeoExtent(-70.4, 41.2, -70.2, 41.4));

        var footprint = _reader.Read(dataset, out _);

        Assert.NotNull(footprint);
        Assert.Equal(FootprintSource.Bounds, footprint.Source);
        Assert.Equal(new GeoExtent(-70.4, 41.2, -70.2, 41.4), footprint.Extent);
    }

    [Fact]
    public void Missing_WithNoBox_HasNoFootprint()
    {
        var dataset = new ChartLayerDataset(Path.Combine(_temp.Path, "gone.000"), "S-57");

        Assert.Null(_reader.Read(dataset, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void CoverageOf_TakesDataCoverageSurfaces()
    {
        Feature[] features =
        [
            Surface("DataCoverage", Square(0, 0, 1), holes: [Square(0.25, 0.25, 0.5)]),
            Surface("DataCoverage", Square(5, 5, 1), attributes: new() { ["categoryOfCoverage"] = 1 }),
            Surface("DepthArea", Square(10, 10, 1)),
            new Feature
            {
                FeatureType = "DataCoverage",
                Id = 9,
                GeometryType = GeometryType.Curve,
                Coordinates = Square(20, 20, 1),
                Attributes = new Dictionary<string, object?>(),
            },
        ];

        var polygons = FootprintReader.CoverageOf(features);

        Assert.Equal(2, polygons.Count);
        Assert.Single(polygons[0].Holes);
        Assert.True(polygons[0].Contains(0.1, 0.1));
        Assert.False(polygons[0].Contains(0.5, 0.5));
        Assert.True(polygons[1].Contains(5.5, 5.5));
    }

    [Theory]
    [InlineData(2)]
    [InlineData("2")]
    [InlineData("noCoverageAvailable")]
    public void CoverageOf_LeavesOutNoCoverageAvailable(object category)
    {
        var polygons = FootprintReader.CoverageOf([Surface("DataCoverage", Square(0, 0, 1), attributes: new() { ["categoryOfCoverage"] = category })]);

        Assert.Empty(polygons);
    }

    [Fact]
    public void CoverageOf_TakesEachSurfacePart()
    {
        var feature = new Feature
        {
            FeatureType = "DataCoverage",
            Id = 1,
            GeometryType = GeometryType.Surface,
            Coordinates = Square(0, 0, 1),
            SurfaceParts = [new SurfacePart(Square(0, 0, 1), []), new SurfacePart(Square(3, 3, 1), [])],
            Attributes = new Dictionary<string, object?>(),
        };

        var polygons = FootprintReader.CoverageOf([feature]);

        Assert.Equal(2, polygons.Count);
        Assert.Equal(new GeoExtent(3, 3, 4, 4), polygons[1].Extent);
    }

    [Fact]
    public void CoverageOf_DropsDegenerateRings()
    {
        GeoPosition[] line = [new(0, 0), new(1, 1), new(0, 0)];

        Assert.Empty(FootprintReader.CoverageOf([Surface("DataCoverage", line)]));
    }

    private async Task<ChartLayerDataset> InspectAsync(string relativePath)
    {
        var path = Fixtures.Upstream(relativePath);
        Assert.SkipWhen(path is null, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        var inspection = await _inspector.InspectAsync(path, cancellationToken: TestContext.Current.CancellationToken);
        return Assert.Single(inspection.Datasets);
    }

    private static Feature Surface(
        string type,
        IReadOnlyList<GeoPosition> ring,
        IReadOnlyList<IReadOnlyList<GeoPosition>>? holes = null,
        Dictionary<string, object?>? attributes = null) => new()
        {
            FeatureType = type,
            Id = 1,
            GeometryType = GeometryType.Surface,
            Coordinates = ring,
            InteriorRings = holes ?? [],
            Attributes = attributes ?? [],
        };

    /// <summary>A closed square ring with its south-west corner at (<paramref name="latitude"/>, <paramref name="longitude"/>).</summary>
    private static GeoPosition[] Square(double latitude, double longitude, double size) =>
    [
        new(latitude, longitude),
        new(latitude + size, longitude),
        new(latitude + size, longitude + size),
        new(latitude, longitude + size),
        new(latitude, longitude),
    ];
}
