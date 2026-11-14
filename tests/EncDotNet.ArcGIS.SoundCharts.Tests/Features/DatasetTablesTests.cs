using EncDotNet.S100.Datasets.Pipelines;
using EncDotNet.SoundCharts.Features;
using static EncDotNet.SoundCharts.Tests.Features.FakeProcessor;

namespace EncDotNet.SoundCharts.Tests.Features;

/// <summary>
/// The plugin datasource's table and schema mapping (issue #15), without
/// Pro: a table per feature type and geometry kind, attributes flattened
/// and typed, feature associations as a table, spatial search through the
/// library's index.
/// </summary>
public sealed class DatasetTablesTests
{
    private const string Path = @"C:\data\TEST0001.gml";

    [Fact]
    public void Tables_AreOnePerFeatureTypeAndGeometryKind()
    {
        var tables = DatasetTables.Build(new FakeProcessor(
            "S-124",
            new FakeFeature("a", "Landmark", Point(1, 2), [], "Landmark"),
            new FakeFeature("b", "Landmark", Box(0, 0, 1, 1), []),
            new FakeFeature("c", "DepthArea", Box(0, 0, 1, 1), [], "Depth Area"),
            new FakeFeature("d", "Sounding", Points((1, 1), (2, 2)), []),
            new FakeFeature("e", "Sounding", Point(3, 3), []),
            new FakeFeature("f", "Coastline", Line((0, 0), (1, 1)), []),
            new FakeFeature("g", "Meta", null, [])), Path);

        Assert.Equal("S-124", tables.Product);
        Assert.Equal("TEST0001", tables.Name);
        Assert.Equal(
            ["Coastline_Line", "DepthArea_Area", "Landmark_Point", "Landmark_Area", "Meta_Table", "Sounding_Multipoint"],
            tables.Tables.Select(t => t.Name));
        Assert.Equal(FeatureGeometryKind.Multipoint, tables.Table("sounding_multipoint")?.Kind);
        Assert.Equal(2, tables.Table("Sounding_Multipoint")?.Rows.Count);
        Assert.Equal("Depth Area", tables.Table("DepthArea_Area")?.DisplayName);
        Assert.Equal("Meta", tables.Table("Meta_Table")?.DisplayName);
        Assert.False(tables.Table("Meta_Table")?.IsSpatial);
        Assert.Null(tables.Table("Meta_Table")?.Extent);
    }

    [Fact]
    public void ObjectIds_AreTheEnumerationOrderPlusOne_UniqueAcrossTables()
    {
        var tables = DatasetTables.Build(new FakeProcessor(
            "S-124",
            new FakeFeature("a", "Landmark", Point(1, 2), []),
            new FakeFeature("b", "DepthArea", Box(0, 0, 1, 1), []),
            new FakeFeature("c", "Landmark", Point(3, 4), [])), Path);

        Assert.Equal([1L, 3L], tables.Table("Landmark_Point")!.Rows.Select(r => r.ObjectId));
        Assert.Equal([2L], tables.Table("DepthArea_Area")!.Rows.Select(r => r.ObjectId));
        Assert.Equal(3L, tables.Table("Landmark_Point")!.Find(3)?.Values[0]);
        Assert.Null(tables.Table("Landmark_Point")!.Find(2));
    }

    [Fact]
    public void Fields_ComeFirstFixedThenAttributesInFirstSeenOrder()
    {
        var table = Single(
            new FakeFeature("1", "Wreck", Point(0, 0), [Simple("categoryOfWreck", "2", "dangerous wreck", "Category of Wreck")]),
            new FakeFeature("2", "Wreck", Point(1, 1), [Simple("valueOfSounding", "12.5", "12.5 m", "Value of Sounding"), Simple("categoryOfWreck", "1", "non-dangerous wreck")]));

        Assert.Equal(
            ["OBJECTID", "Shape", "FeatureRef", "FeatureType", "categoryOfWreck", "valueOfSounding"],
            table.Fields.Select(f => f.Name));
        Assert.Equal(
            [FeatureFieldType.ObjectId, FeatureFieldType.Geometry, FeatureFieldType.String, FeatureFieldType.String, FeatureFieldType.String, FeatureFieldType.Double],
            table.Fields.Select(f => f.Type));
        Assert.Equal("Category of Wreck", table.Fields[4].Alias);
        Assert.Equal([1L, null, "1", "Wreck", "dangerous wreck", null], table.Rows[0].Values);
        Assert.Equal([2L, null, "2", "Wreck", "non-dangerous wreck", 12.5], table.Rows[1].Values);
    }

    [Fact]
    public void Columns_AreTypedOnlyWhenEveryValueFits()
    {
        var date = new DateTime(2026, 11, 12, 14, 0, 0, DateTimeKind.Utc);
        var table = Single(
            new FakeFeature("1", "Light", Point(0, 0),
            [
                Simple("height", "12"),
                Simple("range", "7"),
                Simple("name", "12"),
                Simple("issued", "2026-11-12T14:00:00Z", date: date),
                Simple("multi", "1"),
                Simple("multi", "2"),
            ]),
            new FakeFeature("2", "Light", Point(1, 1),
            [
                Simple("height", "-3"),
                Simple("range", "7.5"),
                Simple("name", "Rock light"),
                Simple("issued", "2026-11-13", date: date.AddDays(1)),
                Simple("multi", "3"),
            ]));

        var types = table.Fields.ToDictionary(f => f.Name, f => f.Type);
        Assert.Equal(FeatureFieldType.Integer, types["height"]);
        Assert.Equal(FeatureFieldType.Double, types["range"]);
        Assert.Equal(FeatureFieldType.String, types["name"]);
        Assert.Equal(FeatureFieldType.Date, types["issued"]);
        Assert.Equal(FeatureFieldType.String, types["multi"]);

        var first = Row(table, 0);
        Assert.Equal(12, first["height"]);
        Assert.Equal(7.0, first["range"]);
        Assert.Equal(date, first["issued"]);
        Assert.Equal(DateTimeKind.Utc, ((DateTime)first["issued"]!).Kind);
        Assert.Equal("1; 2", first["multi"]);
    }

    [Fact]
    public void ComplexAttributesAndInformation_AreFlattenedIntoColumns()
    {
        var table = Single(new FakeFeature("1", "Bridge", Line((0, 0), (1, 0)),
        [
            Complex("featureName", "Feature Name", Simple("name", "Old Bridge", name: "Name"), Simple("language", "eng")),
            Complex("featureName", "Feature Name", Simple("name", "Vieux Pont", name: "Name"), Simple("language", "fra")),
            // An associated information type, as the library lists it.
            Complex("NauticalInformation", "Nautical Information", Complex("information", "Information", Simple("text", "Opens on request"))),
        ]));

        var row = Row(table, 0);
        Assert.Equal("Old Bridge; Vieux Pont", row["featureName_name"]);
        Assert.Equal("eng; fra", row["featureName_language"]);
        Assert.Equal("Opens on request", row["NauticalInformation_information_text"]);
        Assert.Equal("Feature Name: Name", table.Fields.Single(f => f.Name == "featureName_name").Alias);
    }

    [Fact]
    public void FieldNames_AreCleanedAndUnique()
    {
        var table = Single(new FakeFeature("1", "Area", Box(0, 0, 1, 1),
        [
            Simple("FeatureRef", "clash"),
            Simple("bad-name", "x"),
            Simple("bad_name", "y"),
            Simple("1st", "z"),
            Simple(new string('a', 80), "long"),
        ]));

        Assert.Equal(
            ["OBJECTID", "Shape", "FeatureRef", "FeatureType", "FeatureRef_2", "bad_name", "bad_name_2", "F1st", new string('a', 64)],
            table.Fields.Select(f => f.Name));
    }

    [Fact]
    public void TextFields_AreAsLongAsTheLongestValue()
    {
        var table = Single(
            new FakeFeature("1", "Notice", Point(0, 0), [Simple("text", "short")]),
            new FakeFeature("2", "Notice", Point(0, 0), [Simple("text", new string('x', 300))]));

        Assert.Equal(300, table.Fields.Single(f => f.Name == "text").Length);
        Assert.Equal(16, table.Fields.Single(f => f.Name == "FeatureType").Length);
    }

    [Fact]
    public void Polygons_AreClosedClockwiseExteriors()
    {
        var table = Single(new FakeFeature("1", "DepthArea", Box(0, 0, 2, 1), []));

        var ring = Assert.Single(table.Rows[0].Shape!.Parts);
        Assert.Equal(ring[0], ring[^1]);
        Assert.Equal(new FeatureVertex(0, 0), ring[0]);
        Assert.Equal(new FeatureVertex(0, 1), ring[1]);
        Assert.Equal(new FeatureEnvelope(0, 0, 2, 1), table.Rows[0].Shape!.Envelope);
    }

    [Fact]
    public void Associations_AreATableKeyedByFeatureRef()
    {
        var tables = DatasetTables.Build(new FakeProcessor(
            "S-421",
            new FakeFeature("RTE", "Route", null, [], References:
            [
                new FeatureReference { Role = "routeWaypoints", ArcRole = "theWaypoints", TargetRef = "WPTS" },
            ]),
            new FakeFeature("WPTS", "RouteWaypoints", null, [])), Path);

        var associations = tables.Tables[^1];
        Assert.Equal(FeatureNames.AssociationsTable, associations.Name);
        Assert.Null(associations.FeatureType);
        Assert.False(associations.IsSpatial);
        Assert.Equal(["OBJECTID", "FeatureRef", "FeatureType", "Association", "Role", "TargetRef"], associations.Fields.Select(f => f.Name));
        Assert.Equal([1L, "RTE", "Route", "routeWaypoints", "theWaypoints", "WPTS"], associations.Rows[0].Values);
    }

    [Fact]
    public void NoAssociations_NoAssociationTable()
    {
        var tables = DatasetTables.Build(new FakeProcessor("S-124", new FakeFeature("a", "Landmark", Point(1, 2), [])), Path);

        Assert.Null(tables.Table(FeatureNames.AssociationsTable));
    }

    [Fact]
    public void Search_UsesTheSpatialIndexAndObjectIds()
    {
        var tables = DatasetTables.Build(new FakeProcessor(
            "S-124",
            new FakeFeature("a", "Buoy", Point(0, 0), []),
            new FakeFeature("b", "Buoy", Point(10, 10), []),
            new FakeFeature("c", "Buoy", Point(10.5, 10.5), []),
            new FakeFeature("d", "Area", Box(9, 9, 20, 20), [])), Path);
        var buoys = tables.Table("Buoy_Point")!;

        Assert.Equal([1L, 2L, 3L], buoys.Search(null).Select(r => r.ObjectId));
        Assert.Equal([2L, 3L], buoys.Search(new FeatureEnvelope(9, 9, 11, 11)).Select(r => r.ObjectId));
        Assert.Equal([3L], buoys.Search(new FeatureEnvelope(9, 9, 11, 11), [3L, 1L]).Select(r => r.ObjectId));
        Assert.Empty(buoys.Search(new FeatureEnvelope(50, 50, 60, 60)));
        Assert.Equal([4L], tables.Table("Area_Area")!.Search(new FeatureEnvelope(15, 15, 15, 15)).Select(r => r.ObjectId));
        Assert.Equal(new FeatureEnvelope(0, 0, 10.5, 10.5), buoys.Extent);
    }

    [Fact]
    public void Open_ReadsACommittedSyntheticDataset()
    {
        var tables = DatasetTables.Open(Fixtures.PathOf("pick_aton.gml"));

        Assert.NotEmpty(tables.Tables);
        Assert.All(tables.Tables.Where(t => t.FeatureType is not null), t => Assert.True(t.Rows.Count > 0));
        Assert.Contains(tables.Tables, t => t.Kind == FeatureGeometryKind.Point);
    }

    [Fact]
    public void Open_MissingFile_Throws() =>
        Assert.Throws<FileNotFoundException>(() => DatasetTables.Open(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "no-such-soundcharts-dataset.gml")));

    private static FeatureTable Single(params FakeFeature[] features) =>
        Assert.Single(DatasetTables.Build(new FakeProcessor("S-124", features), Path).Tables);

    private static Dictionary<string, object?> Row(FeatureTable table, int index) =>
        table.Fields.Select((f, i) => (f.Name, Value: table.Rows[index].Values[i])).ToDictionary(x => x.Name, x => x.Value);
}
