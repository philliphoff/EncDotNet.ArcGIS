using EncDotNet.S100.Datasets.Pipelines;
using EncDotNet.SoundCharts.ChartLayers;
using EncDotNet.SoundCharts.FeatureLayers;
using EncDotNet.SoundCharts.Features;
using static EncDotNet.SoundCharts.Tests.Features.FakeProcessor;

namespace EncDotNet.SoundCharts.Tests.Features;

/// <summary>
/// What Add as Feature Layers adds (design brief §3.2, D14, D16): the
/// group and layer names, their order, and the group's marker.
/// </summary>
public sealed class FeatureLayerPlanTests
{
    [Fact]
    public void Plan_NamesTheGroupAndLayersAfterTheDataset()
    {
        var tables = DatasetTables.Build(new FakeProcessor(
            "S-101",
            new FakeFeature("1", "Landmark", Point(0, 0), [], "Landmark"),
            new FakeFeature("2", "Landmark", Box(0, 0, 1, 1), [], "Landmark"),
            new FakeFeature("3", "DepthArea", Box(0, 0, 1, 1), [], "Depth Area"),
            new FakeFeature("4", "BuoyLateral", Point(0, 0), [], "Lateral Buoy"),
            new FakeFeature("5", "DepthContour", Line((0, 0), (1, 1)), [], "Depth Contour"),
            new FakeFeature("6", "QualityOfData", null, [], "Quality of Data", [new FeatureReference { Role = "r", TargetRef = "1" }])),
            @"C:\charts\US5MA1BO.000");

        var plan = FeatureLayerPlan.For(tables);

        Assert.Equal("SoundCharts features · US5MA1BO", plan.GroupName);
        // Added bottom up: areas, lines, then points; Z to A, so Contents reads A to Z.
        Assert.Equal(
            [
                "Landmark (area) · US5MA1BO",
                "Depth Area · US5MA1BO",
                "Depth Contour · US5MA1BO",
                "Lateral Buoy · US5MA1BO",
                "Landmark (point) · US5MA1BO",
                "Quality of Data · US5MA1BO",
                "Feature associations · US5MA1BO",
            ],
            plan.Members.Select(m => m.Name));
        Assert.Equal([true, true, true, true, true, false, false], plan.Members.Select(m => m.IsSpatial));
        Assert.Equal("Landmark_Area", plan.Members[0].TableName);
        Assert.Equal(FeatureNames.AssociationsTable, plan.Members[^1].TableName);
    }

    [Theory]
    [InlineData("S-101", true)]
    [InlineData("S-57", true)]
    [InlineData("S-124", true)]
    [InlineData("S-421", true)]
    [InlineData("S-102", false)]
    [InlineData("S-104", false)]
    public void Products_AreTheVectorOnes(string product, bool supported) =>
        Assert.Equal(supported, FeatureLayerPlan.Products.Contains(product));

    [Fact]
    public void Marker_RoundTrips_AndIsNotAChartMarker()
    {
        var marker = new FeatureGroupMarker(@"C:\charts\US5MA1BO.000", "S-57");

        var json = marker.ToJson();

        Assert.Equal(marker, FeatureGroupMarker.TryParse(json));
        Assert.Contains("\"role\":\"features\"", json, StringComparison.Ordinal);
        Assert.Null(ChartLayerMarker.TryParse(json));
        Assert.Null(FeatureGroupMarker.TryParse(ChartLayerMarker.ForGroup(["a"]).ToJson()));
        Assert.Null(FeatureGroupMarker.TryParse("not json"));
        Assert.Null(FeatureGroupMarker.TryParse(null));
    }
}
