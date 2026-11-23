using EncDotNet.S100;
using EncDotNet.S100.Crs.ProjNet;
using EncDotNet.SoundCharts.ObjectInfo;

namespace EncDotNet.SoundCharts.Tests.ObjectInfo;

/// <summary>
/// Coverage values in Object information, against EncDotNet.S100's own test
/// datasets. They skip unless <see cref="Fixtures.UpstreamDatasetsVariable"/>
/// names that folder; CI sets it.
/// </summary>
public sealed class ObjectInfoCoverageTests
{
    public static TheoryData<string, string, string> Coverages() => new()
    {
        { "S102/102US004MI1CI262227.h5", "S-102", "depth" },
        { "S104/104US004SC1BO_20251217T12Z.h5", "S-104", "waterLevelHeight" },
        { "S111/111US00_DBOFS_20260320T18Z_US4DE1BB.h5", "S-111", "surfaceCurrentSpeed" },
    };

    [Theory]
    [MemberData(nameof(Coverages))]
    public void Identify_SamplesTheCoverage(string relativePath, string spec, string valueCode)
    {
        var path = Fixtures.Upstream(relativePath);
        Assert.SkipWhen(path is null, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");

        using var service = new ObjectInfoService();
        var dataset = new ChartDataset(path);
        var found = PointsIn(path).Select(p => service.Identify([dataset], p.Latitude, p.Longitude, 50, time: null, TestContext.Current.CancellationToken))
            .FirstOrDefault(r => r.Datasets.Count > 0);

        Assert.NotNull(found);
        Assert.Empty(found.Problems);
        var result = Assert.Single(found.Datasets);
        Assert.Equal(spec, result.Spec);
        var coverage = Assert.Single(result.Features, f => f.Kind == ObjectInfoFeatureKind.Coverage);
        Assert.Contains(coverage.Attributes, a => a.Code == valueCode);
        Assert.Null(coverage.Geometry);
    }

    [Fact]
    public void Identify_SamplesS104AtTheGivenTime()
    {
        var path = Fixtures.Upstream("S104/104US004SC1BO_20251217T12Z.h5");
        Assert.SkipWhen(path is null, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");

        using var service = new ObjectInfoService();
        var dataset = new ChartDataset(path);
        var point = PointsIn(path).First(p => service.Identify([dataset], p.Latitude, p.Longitude, 50, null, TestContext.Current.CancellationToken).FeatureCount > 0);

        var first = TimeOf(service.Identify([dataset], point.Latitude, point.Longitude, 50, null, TestContext.Current.CancellationToken));
        var later = TimeOf(service.Identify([dataset], point.Latitude, point.Longitude, 50, first.AddHours(3), TestContext.Current.CancellationToken));

        Assert.True(later > first, $"Expected a later step than {first:O}, got {later:O}.");
    }

    private static DateTime TimeOf(ObjectInfoReport report)
    {
        var row = report.Datasets.Single().Features.Single().Attributes.Single(a => a.Code == "timePoint");
        return DateTime.Parse(row.RawValue, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal);
    }

    /// <summary>A 15 × 15 lattice over the dataset's geographic extent, centre first.</summary>
    private static IEnumerable<(double Latitude, double Longitude)> PointsIn(string path)
    {
        using var factory = BundledDatasetProcessorFactory.Create();
        var processor = factory.CreateProcessor(path);
        var extent = processor.Metadata.GetGeographicExtent(new ProjNetCrsTransformFactory())
            ?? throw new InvalidOperationException($"{path} has no extent.");
        (processor as IDisposable)?.Dispose();

        var points = new List<(double, double)>
        {
            ((extent.SouthLatitude + extent.NorthLatitude) / 2, (extent.WestLongitude + extent.EastLongitude) / 2),
        };
        const int steps = 15;
        for (var i = 1; i < steps; i++)
        {
            for (var j = 1; j < steps; j++)
            {
                points.Add((
                    extent.SouthLatitude + ((extent.NorthLatitude - extent.SouthLatitude) * i / steps),
                    extent.WestLongitude + ((extent.EastLongitude - extent.WestLongitude) * j / steps)));
            }
        }

        return points;
    }
}
