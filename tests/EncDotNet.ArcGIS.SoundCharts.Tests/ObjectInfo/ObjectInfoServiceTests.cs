using EncDotNet.SoundCharts.ObjectInfo;

namespace EncDotNet.SoundCharts.Tests.ObjectInfo;

/// <summary>
/// Tests for <see cref="ObjectInfoService"/> against a small synthetic S-125
/// dataset (<c>Fixtures/pick_aton.gml</c>): a beacon and a light at
/// 36.95N 76.05W, inside an area, with the light referring to the beacon and
/// to an information type.
/// </summary>
public sealed class ObjectInfoServiceTests : IDisposable
{
    private const double Latitude = 36.95;
    private const double Longitude = -76.05;

    private static readonly ChartDataset Aton = new(Fixtures.PathOf("pick_aton.gml"), "Synthetic AtoN");

    private readonly ObjectInfoService _service = new();

    public void Dispose() => _service.Dispose();

    [Fact]
    public void Identify_ReturnsEveryFeatureAtThePoint_NotJustTheTop()
    {
        var report = _service.Identify([Aton], Latitude, Longitude, 50, time: null, TestContext.Current.CancellationToken);

        Assert.Empty(report.Problems);
        var dataset = Assert.Single(report.Datasets);
        Assert.Same(Aton, dataset.Dataset);
        Assert.Equal("S-125", dataset.Spec);
        Assert.Equal(
            ["CardinalBeacon", "DataCoverage", "LightAllAround"],
            dataset.Features.Select(f => f.FeatureType).Order(StringComparer.Ordinal));
        Assert.Equal(3, report.FeatureCount);
    }

    [Fact]
    public void Identify_OrdersPointsBeforeAreas()
    {
        var report = _service.Identify([Aton], Latitude, Longitude, 50, time: null, TestContext.Current.CancellationToken);

        var kinds = report.Datasets.Single().Features.Select(f => f.Kind).ToList();
        Assert.Equal(
            [ObjectInfoFeatureKind.Point, ObjectInfoFeatureKind.Point, ObjectInfoFeatureKind.Surface],
            kinds);
    }

    [Fact]
    public void Identify_ReportsDecodedAttributes()
    {
        var report = _service.Identify([Aton], Latitude, Longitude, 50, time: null, TestContext.Current.CancellationToken);

        var light = report.Datasets.Single().Features.Single(f => f.FeatureType == "LightAllAround");
        var colour = Assert.Single(light.Attributes, a => a.Code == "colour");
        Assert.Equal("3", colour.RawValue);
        Assert.False(string.IsNullOrWhiteSpace(colour.Value));
    }

    [Fact]
    public void Identify_ReportsInformationAssociations()
    {
        var report = _service.Identify([Aton], Latitude, Longitude, 50, time: null, TestContext.Current.CancellationToken);

        var light = report.Datasets.Single().Features.Single(f => f.FeatureType == "LightAllAround");
        // The library models the S-125 information binding (AtoNStatus); the
        // information type isn't a feature, so it's named by its id.
        var status = Assert.Single(light.References, r => r.TargetRef == "info1");
        Assert.Equal("AtoNStatus", status.Role);
        Assert.Equal("info1", status.Target);
    }

    [Fact]
    public void Identify_ReturnsGeometryForTheHighlight()
    {
        var report = _service.Identify([Aton], Latitude, Longitude, 50, time: null, TestContext.Current.CancellationToken);

        var features = report.Datasets.Single().Features;
        var light = features.Single(f => f.FeatureType == "LightAllAround");
        var point = Assert.Single(light.Geometry!.Points);
        Assert.Equal(Latitude, point.Latitude, 6);
        Assert.Equal(Longitude, point.Longitude, 6);

        var area = features.Single(f => f.Kind == ObjectInfoFeatureKind.Surface);
        Assert.Equal(5, area.Geometry!.ExteriorRing.Count);
    }

    [Fact]
    public void Identify_RadiusDecidesWhichPointsCount()
    {
        // About 1.1 km north of the beacon: inside the area, far from the points.
        var report = _service.Identify([Aton], Latitude + 0.01, Longitude, 50, time: null, TestContext.Current.CancellationToken);

        var feature = Assert.Single(report.Datasets.Single().Features);
        Assert.Equal(ObjectInfoFeatureKind.Surface, feature.Kind);
        Assert.Equal(0.0, feature.DistanceMeters);

        var wide = _service.Identify([Aton], Latitude + 0.01, Longitude, 2_000, time: null, TestContext.Current.CancellationToken);
        Assert.Equal(3, wide.FeatureCount);
    }

    [Fact]
    public void Identify_ListsNoDatasetWhenNothingIsThere()
    {
        var report = _service.Identify([Aton], 10.0, 10.0, 50, time: null, TestContext.Current.CancellationToken);

        Assert.Empty(report.Datasets);
        Assert.Empty(report.Problems);
        Assert.Equal(0, report.FeatureCount);
    }

    [Fact]
    public void Identify_ReportsUnreadableDatasetsAndCarriesOn()
    {
        var missing = new ChartDataset(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.000"));
        var garbage = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.gml");
        File.WriteAllText(garbage, "not a dataset");
        try
        {
            var report = _service.Identify(
                [missing, new ChartDataset(garbage), Aton], Latitude, Longitude, 50, time: null, TestContext.Current.CancellationToken);

            Assert.Equal(2, report.Problems.Count);
            Assert.Equal(missing, report.Problems[0].Dataset);
            Assert.Single(report.Datasets);
        }
        finally
        {
            File.Delete(garbage);
        }
    }

    [Fact]
    public void Identify_ListsEachDatasetOnce()
    {
        var report = _service.Identify([Aton, Aton with { DisplayName = "Again" }], Latitude, Longitude, 50, time: null, TestContext.Current.CancellationToken);

        Assert.Single(report.Datasets);
    }

    [Fact]
    public void Identify_RejectsAnInvalidPoint()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.Identify([Aton], 91, 0, 50, null, TestContext.Current.CancellationToken));
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.Identify([Aton], 0, double.NaN, 50, null, TestContext.Current.CancellationToken));
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.Identify([Aton], 0, 0, -1, null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Identify_HonoursCancellation()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => _service.Identify([Aton], Latitude, Longitude, 50, null, cancelled.Token));
    }

    [Fact]
    public async Task IdentifyAsync_ReturnsTheSameReport()
    {
        var report = await _service.IdentifyAsync([Aton], Latitude, Longitude, 50, null, TestContext.Current.CancellationToken);

        Assert.Equal(3, report.FeatureCount);
    }

    [Fact]
    public void Identify_AfterDispose_Throws()
    {
        _service.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _service.Identify([Aton], Latitude, Longitude, 50, null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Identify_ReportsTheTimeInUtc()
    {
        var time = new DateTime(2026, 11, 12, 14, 0, 0, DateTimeKind.Unspecified);

        var report = _service.Identify([Aton], Latitude, Longitude, 50, time, TestContext.Current.CancellationToken);

        Assert.Equal(DateTimeKind.Utc, report.Time!.Value.Kind);
        Assert.Equal(time.Ticks, report.Time.Value.Ticks);
    }
}
