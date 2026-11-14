using EncDotNet.SoundCharts.Features;

namespace EncDotNet.SoundCharts.Tests.Features;

/// <summary>
/// The table mapping over EncDotNet.S100's own test datasets (issue #15):
/// S-101 and S-57 cells, whose geometry comes from the library's vector
/// source, and S-124 and S-421 GML, whose geometry comes from the
/// processor. Skips without <see cref="Fixtures.UpstreamDatasetsVariable"/>.
/// </summary>
public sealed class UpstreamDatasetTablesTests
{
    [Fact]
    public void S101Cell_HasATablePerTypeAndGeometry()
    {
        var tables = Open("S101/S-101/DATASET_FILES/101AA00DS0003.000");

        Assert.Equal("S-101", tables.Product);
        Assert.Equal("101AA00DS0003", tables.Name);

        // UpdateInformation has point, curve and surface instances: three tables.
        Assert.Equal(
            ["UpdateInformation_Point", "UpdateInformation_Line", "UpdateInformation_Area"],
            tables.Tables.Where(t => t.FeatureType == "UpdateInformation").Select(t => t.Name));

        var depthAreas = Table(tables, "DepthArea_Area");
        Assert.Equal("Depth Area", depthAreas.FeatureTypeName);
        Assert.Equal(10, depthAreas.Rows.Count);
        Assert.Contains(depthAreas.Fields, f => f.Name == "depthRangeMinimumValue" && f.Type != FeatureFieldType.String);
        Assert.All(depthAreas.Rows, r => Assert.Equal(FeatureGeometryKind.Polygon, r.Shape?.Kind));
        Assert.Equal(FeatureGeometryKind.Polyline, Table(tables, "DepthContour_Line").Kind);

        // Every feature has its geometry, read from the vector source by feature reference.
        Assert.All(tables.Tables.Where(t => t.IsSpatial).SelectMany(t => t.Rows), r => Assert.NotNull(r.Shape));
        AssertWithin(tables, -33, 61, -32, 63);
    }

    [Fact]
    public void S57Cell_SoundingsAreMultipoints()
    {
        var tables = Open("S57/US5MA1BO/US5MA1BO.000");

        Assert.Equal("S-57", tables.Product);
        var soundings = Table(tables, "Sounding_Multipoint");
        Assert.All(soundings.Rows, r => Assert.Equal(FeatureGeometryKind.Multipoint, r.Shape?.Kind));
        Assert.Contains(soundings.Rows, r => r.Shape!.Parts[0].Count > 1);

        var anchorage = Table(tables, "AnchorageArea_Area");
        var name = anchorage.Fields.Select((f, i) => (f, i)).Single(x => x.f.Name == "name").i;
        Assert.Equal("Vineyard and Nantucket Sounds Anchorage Area G", anchorage.Rows[0].Values[name]);

        // US5MA1BO's footprint: W -70.35 S 41.25 E -70.275 N 41.325.
        AssertWithin(tables, 41.2, -70.4, 41.4, -70.2);
        var depthAreas = Table(tables, "DepthArea_Area");
        var inCorner = depthAreas.Search(new FeatureEnvelope(-70.35, 41.25, -70.34, 41.26)).ToList();
        Assert.NotEmpty(inCorner);
        Assert.True(inCorner.Count < depthAreas.Rows.Count);
    }

    [Fact]
    public void S124Warning_IsAPointWithItsText()
    {
        var tables = Open("S124/navwarn_bulletin_navarea_x_104_26.gml");

        Assert.Equal("S-124", tables.Product);
        var part = Table(tables, "NavwarnPart_Point");
        var text = part.Fields.Select((f, i) => (f, i)).Single(x => x.f.Name == "warningInformation_information_text").i;
        Assert.Contains("CABLE OPERATIONS", (string?)part.Rows[0].Values[text], StringComparison.Ordinal);
        Assert.Equal(new FeatureEnvelope(146, -16.5, 146, -16.5), part.Extent);
    }

    [Fact]
    public void S421Route_HasLegsWaypointsAndAssociations()
    {
        var tables = Open("S421/RTE-TEST-GFULL.s421.gml");

        Assert.Equal("S-421", tables.Product);
        Assert.Equal(10, Table(tables, "RouteWaypoint_Point").Rows.Count);
        Assert.Equal(9, Table(tables, "RouteWaypointLeg_Line").Rows.Count);
        Assert.False(Table(tables, "Route_Table").IsSpatial);

        var associations = Table(tables, FeatureNames.AssociationsTable);
        Assert.Contains(associations.Rows, r => Equals(r.Values[1], "RTE") && Equals(r.Values[5], "RTE.INFO"));

        // Every association's source is a feature of the dataset, so it relates to the tables.
        var refs = tables.Tables.Where(t => t.FeatureType is not null)
            .SelectMany(t => t.Rows.Select(r => r.Values[t.IsSpatial ? 2 : 1]))
            .ToHashSet();
        Assert.All(associations.Rows, r => Assert.Contains(r.Values[1], refs));
    }

    private static DatasetTables Open(string relativePath)
    {
        var path = Fixtures.Upstream(relativePath);
        Assert.SkipWhen(path is null, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        return DatasetTables.Open(path);
    }

    private static FeatureTable Table(DatasetTables tables, string name) =>
        tables.Table(name) ?? throw new Xunit.Sdk.XunitException($"No table {name}: {string.Join(", ", tables.Tables.Select(t => t.Name))}");

    private static void AssertWithin(DatasetTables tables, double south, double west, double north, double east)
    {
        foreach (var extent in tables.Tables.Select(t => t.Extent).OfType<FeatureEnvelope>())
        {
            Assert.InRange(extent.XMin, west, east);
            Assert.InRange(extent.XMax, west, east);
            Assert.InRange(extent.YMin, south, north);
            Assert.InRange(extent.YMax, south, north);
        }
    }
}
