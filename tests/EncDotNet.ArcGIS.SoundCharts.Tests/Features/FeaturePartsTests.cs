using EncDotNet.SoundCharts.Features;
using static EncDotNet.SoundCharts.Tests.Features.FakeProcessor;

namespace EncDotNet.SoundCharts.Tests.Features;

/// <summary>The small parts of the table mapping: names, rings, flattening and the cache.</summary>
public sealed class FeaturePartsTests
{
    [Theory]
    [InlineData("Landmark", FeatureGeometryKind.Point, "Landmark_Point")]
    [InlineData("Sounding", FeatureGeometryKind.Multipoint, "Sounding_Multipoint")]
    [InlineData("DepthContour", FeatureGeometryKind.Polyline, "DepthContour_Line")]
    [InlineData("DepthArea", FeatureGeometryKind.Polygon, "DepthArea_Area")]
    [InlineData("Route", FeatureGeometryKind.None, "Route_Table")]
    public void TableNames_AlwaysCarryTheGeometry(string type, FeatureGeometryKind kind, string expected) =>
        Assert.Equal(expected, FeatureNames.Table(type, kind));

    [Fact]
    public void Unique_AppendsANumberWithinTheLengthLimit()
    {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "name", new string('a', 64) };

        Assert.Equal("Name_2", FeatureNames.Unique("Name", taken));
        Assert.Equal("Name_3", FeatureNames.Unique("Name", taken));
        Assert.Equal(new string('a', 62) + "_2", FeatureNames.Unique(new string('a', 64), taken));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OrientRing_ClosesAndOrients(bool clockwise)
    {
        FeatureVertex[] counterClockwise = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)];

        var ring = FeatureShape.OrientRing(counterClockwise, clockwise);

        Assert.Equal(5, ring.Count);
        Assert.Equal(ring[0], ring[^1]);
        Assert.Equal(clockwise ? new FeatureVertex(0, 1) : new FeatureVertex(1, 0), ring[1]);
    }

    [Theory]
    [InlineData("12.5", "12.5 m", "12.5")]
    [InlineData("3", "tower", "tower")]
    [InlineData("3", null, "3")]
    [InlineData("eng", "English", "English")]
    [InlineData("2", "2nd edition", "2nd edition")]
    public void ValueOf_KeepsTheNumberWithoutItsUnit(string raw, string? display, string expected) =>
        Assert.Equal(expected, AttributeFlattener.ValueOf(Simple("code", raw, display)));

    [Fact]
    public void Flatten_SkipsPickNoticesAndEmptyValues()
    {
        var flat = AttributeFlattener.Flatten(
        [
            Simple("notice", "Not for navigation"),
            Simple("stale", "Old"),
            Simple("empty", string.Empty),
            Simple("kept", "x"),
        ]);

        Assert.Equal(["empty", "kept"], flat.Select(f => f.Path));
        Assert.Null(flat[0].Text);
    }

    [Fact]
    public void Cache_ReadsOnceUntilTheFileChanges()
    {
        using var temp = new TempFolder();
        var path = System.IO.Path.Combine(temp.Path, "a.gml");
        File.WriteAllText(path, "one");
        var reads = 0;
        var cache = new DatasetTablesCache(open: p =>
        {
            reads++;
            return DatasetTables.Build(new FakeProcessor("S-124", new FakeFeature("a", "Buoy", Point(0, 0), [])), p);
        });

        var first = cache.Get(path);
        Assert.Same(first, cache.Get(path.ToUpperInvariant()));
        Assert.True(cache.Contains(path));
        Assert.Equal(1, reads);

        File.WriteAllText(path, "changed, longer");
        Assert.NotSame(first, cache.Get(path));
        Assert.Equal(2, reads);
    }

    [Fact]
    public void Cache_KeepsTheMostRecent_AndForgetsFailures()
    {
        using var temp = new TempFolder();
        var paths = Enumerable.Range(0, 3).Select(i => System.IO.Path.Combine(temp.Path, $"{i}.gml")).ToList();
        paths.ForEach(p => File.WriteAllText(p, "x"));
        var fail = true;
        var cache = new DatasetTablesCache(capacity: 2, open: p =>
        {
            if (fail && p.EndsWith("0.gml", StringComparison.Ordinal))
            {
                throw new InvalidDataException("bad");
            }

            return DatasetTables.Build(new FakeProcessor("S-124"), p);
        });

        Assert.Throws<InvalidDataException>(() => cache.Get(paths[0]));
        Assert.False(cache.Contains(paths[0]));
        fail = false;
        cache.Get(paths[0]);
        cache.Get(paths[1]);
        cache.Get(paths[2]);

        Assert.False(cache.Contains(paths[0]));
        Assert.True(cache.Contains(paths[1]));
        Assert.True(cache.Contains(paths[2]));
        Assert.Throws<FileNotFoundException>(() => cache.Get(System.IO.Path.Combine(temp.Path, "missing.gml")));
    }
}
