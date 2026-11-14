using EncDotNet.S100.DataModel;
using EncDotNet.S100.Navigation;
using EncDotNet.S100.Quantities;
using EncDotNet.SoundCharts.RouteCheck;

namespace EncDotNet.SoundCharts.Tests.RouteCheck;

public sealed class FindingFeaturesTests
{
    private static readonly GeoPosition A = new(41.0, -70.0);
    private static readonly GeoPosition B = new(41.1, -70.1);

    [Fact]
    public void From_APlace_IsAPoint()
    {
        var finding = new RouteFinding("danger.1", RouteFindingCodes.DangerInCorridor)
        {
            LegIndex = 1,
            Distance = Length.FromMetres(3704),
            Location = RouteFindingLocation.Point(A, 3704),
            Feature = new RouteFindingFeature("US5MA1BO.000", "41") { FeatureType = "Wreck" },
            Title = "Wreck in the corridor",
            Meta = "Leg 2 → 3 · 40 m to port",
        };

        var feature = Assert.Single(FindingFeatures.From([finding]));

        Assert.Equal(FindingShape.Point, feature.Shape);
        Assert.Equal([A], feature.Path);
        Assert.Equal(FindingFeatures.SeverityOf(finding.Group), feature.Severity);
        Assert.Equal(FindingFeatures.RankOf(finding.Group), feature.SeverityRank);
        Assert.Equal(RouteFindingCodes.DangerInCorridor.Value, feature.Rule);
        Assert.Equal("Route check", feature.Category);
        Assert.Equal("Wreck in the corridor", feature.Title);
        Assert.Equal("Leg 2 → 3 · 40 m to port", feature.Message);
        Assert.Equal("US5MA1BO.000 Wreck 41", feature.Features);
        Assert.Equal(2, feature.Leg);
        Assert.Equal(2.0, feature.DistanceNm!.Value, 6);
        Assert.Equal("danger.1", feature.FindingId);
    }

    [Fact]
    public void From_AStretch_IsASegment()
    {
        var finding = new RouteFinding("ukc.1", RouteFindingCodes.BelowSetting)
        {
            Location = new RouteFindingLocation([A, B], 1852, 3704),
            Title = "Below your UKC setting for 1.0 NM",
        };

        var feature = Assert.Single(FindingFeatures.From([finding]));

        Assert.Equal(FindingShape.Segment, feature.Shape);
        Assert.Equal([A, B], feature.Path);
        Assert.Equal("MUST REVIEW", feature.Severity);
        Assert.Equal(1, feature.SeverityRank);
        Assert.Equal("Clearance", feature.Category);
        Assert.Null(feature.Features);
        Assert.Null(feature.Leg);
        Assert.Equal(1.0, feature.DistanceNm!.Value, 6);
    }

    [Fact]
    public void From_NoPlace_HasNoFeature()
    {
        var finding = new RouteFinding("datum", RouteFindingCodes.DatumUnconfirmed) { Title = "Vertical datums unconfirmed" };

        Assert.Empty(FindingFeatures.From([finding]));
    }

    [Theory]
    [InlineData(RouteFindingGroup.MustReview, "MUST REVIEW", 1)]
    [InlineData(RouteFindingGroup.Check, "CHECK", 2)]
    [InlineData(RouteFindingGroup.Info, "INFO", 3)]
    public void SeverityOf_IsTheViewersGroup(RouteFindingGroup group, string label, int rank)
    {
        Assert.Equal(label, FindingFeatures.SeverityOf(group));
        Assert.Equal(rank, FindingFeatures.RankOf(group));
    }

    [Fact]
    public void Item_KeepsTheFindingsWordsAndPlace()
    {
        var finding = new RouteFinding("ukc.1", RouteFindingCodes.BelowSetting)
        {
            Location = new RouteFindingLocation([A, B, new(41.2, -70.2)], 0, 100),
            Title = "Below your UKC setting for 1.0 NM",
            Meta = "Leg 1 → 2",
        };

        var item = RouteCheckPresenter.Item(finding);

        Assert.Equal("MUST REVIEW", item.Group);
        Assert.Equal(RouteCheckTone.Destructive, item.Tone);
        Assert.Equal("Below your UKC setting for 1.0 NM", item.Title);
        Assert.Equal("Leg 1 → 2", item.Meta);
        Assert.True(item.HasLocation);
        Assert.Equal(B, item.Position);
    }

    [Fact]
    public void Item_AConfidenceNote_IsNeutral()
    {
        var finding = new RouteFinding("s101", RouteFindingCodes.ChartedDepthFromS101, RouteFindingGroup.Check) { Title = "Charted depth from S-101 only" };

        var item = RouteCheckPresenter.Item(finding);

        Assert.Equal("CHECK", item.Group);
        Assert.Equal("Confidence", item.Tag);
        Assert.Equal(RouteCheckTone.Neutral, item.Tone);
        Assert.False(item.HasLocation);
        Assert.Null(item.Position);
    }
}
