using EncDotNet.SoundCharts.ObjectInfo;

namespace EncDotNet.SoundCharts.Tests.ObjectInfo;

public class ObjectInfoPaneModelTests
{
    private static readonly ChartDataset Cell = new(@"C:\data\101AA00DS0003.000", "AA00DS0003");

    [Fact]
    public void StartsEmpty()
    {
        var model = new ObjectInfoPaneModel();

        Assert.Equal(ObjectInfoState.Empty, model.View.State);
        Assert.Null(model.PositionText);
    }

    [Fact]
    public void Click_GoesFromWorkingToResults()
    {
        var model = new ObjectInfoPaneModel();

        var working = model.ShowWorking(36.95, -76.05);
        Assert.Equal(ObjectInfoState.Working, working.State);
        Assert.Same(working, model.View);
        Assert.Equal((36.95, -76.05), model.Point);
        Assert.Equal("36.950000, -76.050000", model.PositionText);

        var results = model.ShowReport(Report(36.95, -76.05, Feature("a")));
        Assert.Equal(ObjectInfoState.Results, results.State);
        Assert.Single(model.View.Items);
    }

    [Fact]
    public void Click_GoesFromWorkingToNothingHere()
    {
        var model = new ObjectInfoPaneModel();
        model.ShowWorking(1, 2);

        model.ShowReport(Report(1, 2));

        Assert.Equal(ObjectInfoState.NothingHere, model.View.State);
        Assert.Empty(model.View.Items);
        Assert.NotNull(model.PositionText);
    }

    [Fact]
    public void Failure_ClearsTheResultsButKeepsThePoint()
    {
        var model = new ObjectInfoPaneModel();
        model.ShowReport(Report(36.95, -76.05, Feature("a")));
        model.ShowWorking(10, 20);

        model.ShowFailed();

        Assert.Equal(ObjectInfoState.Failed, model.View.State);
        Assert.Empty(model.View.Items);
        Assert.Equal((10.0, 20.0), model.Point);
        Assert.Null(model.PositionText);
    }

    [Fact]
    public void NoChartLayers_ReplacesEarlierResults()
    {
        var model = new ObjectInfoPaneModel();
        model.ShowReport(Report(1, 2, Feature("a")));

        model.ShowNoChartLayers(3, 4);

        Assert.Equal(ObjectInfoState.NoChartLayers, model.View.State);
        Assert.Empty(model.View.Items);
        Assert.Equal((3.0, 4.0), model.Point);
    }

    [Fact]
    public void NextClick_LeavesAFailure()
    {
        var model = new ObjectInfoPaneModel();
        model.ShowFailed();

        model.ShowWorking(1, 2);

        Assert.Equal(ObjectInfoState.Working, model.View.State);
    }

    private static ObjectInfoReport Report(double latitude, double longitude, params ObjectInfoFeature[] features) =>
        new(latitude, longitude, null, features.Length == 0 ? [] : [new(Cell, "S-101", features)], []);

    private static ObjectInfoFeature Feature(string id) =>
        new(id, "LIGHTS", "Light", ObjectInfoFeatureKind.Point, 3.0, [], [], Geometry: null);
}
