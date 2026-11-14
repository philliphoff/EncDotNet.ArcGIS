using EncDotNet.S100.DataModel;
using EncDotNet.SoundCharts.ChartLayers;
using EncDotNet.SoundCharts.ObjectInfo;
using static EncDotNet.SoundCharts.Tests.ChartLayers.ChartLayerSamples;

namespace EncDotNet.SoundCharts.Tests.ChartLayers;

/// <summary>
/// The Coverage layer without Pro (issue #9): its feature values, how it
/// stays in step with the chart's datasets, its marker, and the zoom floor.
/// </summary>
public class CoverageTests
{
    [Fact]
    public void Values_RecordTheDatasetAndItsTimes()
    {
        var first = new DateTime(2025, 12, 17, 12, 0, 0, DateTimeKind.Utc);
        var footprint = new DatasetFootprint(
            Tide, [FootprintPolygon.FromExtent(new GeoExtent(-80.1, 32.6, -79.8, 32.9))], FootprintSource.Bounds, new DateOnly(2025, 12, 17), first, first.AddDays(2));

        var values = CoverageTable.ValuesOf(footprint);

        Assert.Equal("104US004SC1BO_20251217T12Z", values[CoverageTable.NameField]);
        Assert.Equal("S-104", values[CoverageTable.ProductField]);
        Assert.Equal("2.0.0", values[CoverageTable.EditionField]);
        Assert.Equal("water-level", values[CoverageTable.LayerField]);
        Assert.Equal(Tide.Path, values[CoverageTable.PathField]);
        Assert.Equal(new DateTime(2025, 12, 17), values[CoverageTable.IssueDateField]);
        Assert.Equal(first, values[CoverageTable.FirstTimeField]);
        Assert.Equal(first.AddDays(2), values[CoverageTable.LastTimeField]);
        Assert.Equal("bounds", values[CoverageTable.SourceField]);
    }

    [Fact]
    public void Values_LeaveUnknownsEmpty()
    {
        var footprint = new DatasetFootprint(Cell, [FootprintPolygon.FromExtent(new GeoExtent(-71.06, 42.31, -70.94, 42.38))], FootprintSource.Coverage);

        var values = CoverageTable.ValuesOf(footprint);

        Assert.Null(values[CoverageTable.EditionField]);
        Assert.Null(values[CoverageTable.IssueDateField]);
        Assert.Null(values[CoverageTable.FirstTimeField]);
        Assert.Equal("coverage", values[CoverageTable.SourceField]);
    }

    [Fact]
    public void Label_ShowsNameProductAndEdition()
    {
        Assert.Contains(CoverageTable.NameField, CoverageTable.LabelExpression, StringComparison.Ordinal);
        Assert.Contains(CoverageTable.ProductField, CoverageTable.LabelExpression, StringComparison.Ordinal);
        Assert.Contains(CoverageTable.EditionField, CoverageTable.LabelExpression, StringComparison.Ordinal);
        Assert.Equal("ChartLayer = 'surface-currents'", CoverageTable.WhereLayer(ChartLayerKind.SurfaceCurrents));
    }

    [Fact]
    public void WherePath_EscapesQuotes()
    {
        Assert.Equal("DatasetPath = 'C:\\O''Brien\\a.000'", CoverageTable.WherePath(@"C:\O'Brien\a.000"));
    }

    [Fact]
    public void FeatureClassName_IsAValidUniqueName()
    {
        var a = CoverageTable.NewFeatureClassName();
        var b = CoverageTable.NewFeatureClassName();

        Assert.Matches("^Coverage_[0-9a-f]{8}$", a);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Sync_EmptyLayer_ReadsEveryDataset()
    {
        var plan = CoverageSyncPlan.For([Cell, Tide], []);

        // In path order.
        Assert.Equal([Tide, Cell], plan.ToRead);
        Assert.Empty(plan.ToDelete);
    }

    [Fact]
    public void Sync_UpToDate_DoesNothing()
    {
        var plan = CoverageSyncPlan.For([Cell, Tide], [Row(Cell), Row(Tide with { Path = Tide.Path.ToUpperInvariant() })]);

        Assert.True(plan.IsEmpty);
    }

    [Fact]
    public void Sync_RemovedDataset_DeletesItsFootprint()
    {
        var plan = CoverageSyncPlan.For([Cell], [Row(Cell), Row(Tide)]);

        Assert.Empty(plan.ToRead);
        Assert.Equal([Tide.Path], plan.ToDelete);
    }

    [Fact]
    public void Sync_NewEdition_ReadsTheFootprintAgain()
    {
        var plan = CoverageSyncPlan.For([S101 with { Edition = "2.1.0" }], [Row(S101)]);

        Assert.Equal([S101.Path], plan.ToDelete);
        Assert.Equal("2.1.0", Assert.Single(plan.ToRead).Edition);
    }

    [Fact]
    public void Sync_PathWrittenTwice_IsWrittenAgain()
    {
        var plan = CoverageSyncPlan.For([Cell], [Row(Cell), Row(Cell)]);

        Assert.Equal([Cell.Path], plan.ToDelete);
        Assert.Equal([Cell], plan.ToRead);
    }

    [Fact]
    public void Marker_Coverage_RoundTripsAndHasNoDatasets()
    {
        var read = ChartLayerMarker.TryParse(ChartLayerMarker.ForCoverage().ToJson());

        Assert.NotNull(read);
        Assert.True(read.IsCoverage);
        Assert.False(read.IsGroup);
        Assert.Null(read.Kind);
        Assert.Empty(read.Datasets);
        Assert.Equal(ChartLayerMarker.ForCoverage(), read);
        Assert.Throws<InvalidOperationException>(() => read.ToDatasetSet());
    }

    [Fact]
    public void Registry_KeepsCoverageApartFromChartLayers()
    {
        var registry = new ChartLayerRegistry();
        registry.Set("chart", "map", ChartLayerMarker.ForLayer(ChartLayerKind.Chart, [Cell]));
        registry.Set("coverage", "map", ChartLayerMarker.ForCoverage());
        registry.Set("other-coverage", "other-map", ChartLayerMarker.ForCoverage());

        Assert.Equal(["chart"], registry.LayersOf("map").Select(pair => pair.Key));
        Assert.Equal("coverage", registry.CoverageOf("map"));
        Assert.Null(registry.CoverageOf("no-map"));

        var resolved = new RegistryChartDatasetResolver(registry).Resolve(
            [new ChartLayerReference("coverage", "Coverage", "map"), new ChartLayerReference("chart", "Chart", "map")]);
        Assert.Equal([Cell.Path], resolved.Select(d => d.Path));
    }

    [Theory]
    [InlineData(100_000, true)]
    [InlineData(72_224, false)]
    [InlineData(50_000, false)]
    public void ZoomFloor_BelowAnyLayersMinimumScale(double scale, bool below)
    {
        Assert.Equal(below, ZoomFloor.IsBelow(scale, [72_224, 1_155_581]));
    }

    [Fact]
    public void ZoomFloor_NoMinimumScale_IsNeverBelow()
    {
        Assert.False(ZoomFloor.IsBelow(50_000_000, [0]));
        Assert.False(ZoomFloor.IsBelow(50_000_000, []));
        Assert.False(ZoomFloor.IsBelow(double.NaN, [72_224]));
    }

    [Fact]
    public void ZoomIn_SmallCellJustBeyondItsChart_ZoomsInToTheChart()
    {
        // A3: US5MA1BO's footprint lands at 1:73,880 against Chart's 1:72,224.
        Assert.Equal(72_224 * ZoomFloor.Inside, ZoomFloor.ZoomInScale(73_880, 72_224, centreHasData: true));
    }

    [Fact]
    public void ZoomIn_DistantDatasets_StaysOnTheFootprints()
    {
        // Step 0: S-57 Nantucket and S-104 Charleston; their centre is empty ocean.
        Assert.Null(ZoomFloor.ZoomInScale(9_000_000, 72_224, centreHasData: false));
    }

    [Fact]
    public void ZoomIn_InScale_Stays()
    {
        Assert.Null(ZoomFloor.ZoomInScale(50_000, 72_224, centreHasData: true));
        Assert.Null(ZoomFloor.ZoomInScale(9_000_000, 0, centreHasData: true));
    }

    [Fact]
    public void Polygon_FromExtent_IsTheBox()
    {
        var polygon = FootprintPolygon.FromExtent(new GeoExtent(-80.1, 32.6, -79.8, 32.9));

        Assert.Equal(new GeoExtent(-80.1, 32.6, -79.8, 32.9), polygon.Extent);
        Assert.True(polygon.Contains(32.75, -79.95));
        Assert.False(polygon.Contains(41.29, -70.31));
    }

    [Fact]
    public void Polygon_TryCreate_NeedsThreeDistinctPositions()
    {
        Assert.Null(FootprintPolygon.TryCreate(null, null));
        Assert.Null(FootprintPolygon.TryCreate([new GeoPosition(0, 0), new GeoPosition(0, 0), new GeoPosition(0, 0), new GeoPosition(0, 0)], null));
        Assert.Null(FootprintPolygon.TryCreate([new GeoPosition(0, 0), new GeoPosition(1, double.NaN), new GeoPosition(1, 1)], null));
        Assert.NotNull(FootprintPolygon.TryCreate([new GeoPosition(0, 0), new GeoPosition(1, 0), new GeoPosition(1, 1)], [[new GeoPosition(0, 0)]]));
    }

    private static KeyValuePair<string, string> Row(ChartLayerDataset dataset) => new(dataset.Path, CoverageTable.KeyOf(dataset));
}
