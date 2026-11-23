using EncDotNet.S100.DataModel;
using EncDotNet.SoundCharts.RouteCheck;
using EncDotNet.SoundCharts.Themes;

namespace EncDotNet.SoundCharts.Tests.RouteCheck;

/// <summary>
/// The pane's states and summary chips from <see cref="RouteCheckPresenter"/>,
/// without upstream test data.
/// </summary>
public sealed class RouteCheckPresenterTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

    private readonly CultureScope _culture = new();

    public void Dispose() => _culture.Dispose();

    [Fact]
    public void Empty_IsTheEmptyState()
    {
        var view = RouteCheckPresenter.Empty;

        Assert.Equal(RouteCheckPaneState.Empty, view.State);
        Assert.Equal("Check a route", view.Heading);
        Assert.StartsWith("Select a line feature and choose Check Selected Line", view.Parameters);
        Assert.Null(view.Title);
        Assert.Null(view.Footer);
        Assert.Empty(view.Rows);
        Assert.Empty(view.Items);
        Assert.False(view.IsEmpty);
    }

    [Fact]
    public void Working_IsTheWorkingState()
    {
        var view = RouteCheckPresenter.Working("Solent inbound");

        Assert.Equal(RouteCheckPaneState.Working, view.State);
        Assert.Equal("Solent inbound", view.Heading);
        Assert.Equal("Checking the passage...", view.Title);
        Assert.Equal(RouteCheckTone.Neutral, view.Tone);
        Assert.Null(view.Footer);
        Assert.Empty(view.Rows);
        Assert.False(view.IsEmpty);
    }

    [Fact]
    public void Failed_IsADangerMessageThatSaysWhy()
    {
        var view = RouteCheckPresenter.Failed("Solent inbound", "The file is locked.");

        Assert.Equal(RouteCheckPaneState.Failed, view.State);
        Assert.Equal("Solent inbound", view.Heading);
        Assert.Equal("Couldn't check the passage", view.Title);
        Assert.Equal("The file is locked.", view.Subtitle);
        Assert.Equal(RouteCheckTone.Destructive, view.Tone);
        Assert.Equal(SoundChartsTone.Danger, Tones.From(view.Tone));
        Assert.Null(view.Confidence);
        Assert.Null(view.Footer);
        Assert.Empty(view.Rows);
        Assert.Empty(view.Items);
        Assert.False(view.IsEmpty);
    }

    [Fact]
    public void Failed_WithoutARoute_KeepsTheEmptyTitle()
    {
        var view = RouteCheckPresenter.Failed(null, "This file isn't an RTZ or S-421 route.");

        Assert.Equal(RouteCheckPaneState.Failed, view.State);
        Assert.Equal("This file isn't an RTZ or S-421 route.", view.Subtitle);
        Assert.Equal("Check a route", view.Heading);
        Assert.Null(view.Parameters);
    }

    [Fact]
    public void Present_WithoutData_IsAVerdictWithTonedChips()
    {
        GeoPosition[] line = [new(41.255, -70.345), new(41.29, -70.31)];
        var route = RouteLines.FromLine("Nowhere", RouteSource.Sketch, line);
        var outcome = new RouteCheckService().Check(route, [], ShipParameters.Default, Now, TestContext.Current.CancellationToken);

        var view = RouteCheckPresenter.Present(outcome);

        Assert.Equal(RouteCheckPaneState.Verdict, view.State);
        Assert.Equal("Nowhere", view.Heading);
        Assert.Equal("Not for navigation · no data loaded for this route", view.Footer);

        // No draft: clearance isn't checked and has no chip.
        var clearance = view.Rows[0];
        Assert.Equal("Clearance", clearance.Label);
        Assert.Null(clearance.Chip);
        Assert.Equal(RouteCheckTone.Neutral, clearance.ChipTone);

        // The route row's chip counts its findings, toned by the worst of them.
        var routeRow = view.Rows[1];
        Assert.Equal("Route", routeRow.Label);
        Assert.NotNull(routeRow.Chip);
        if (routeRow.Chip == "0")
        {
            Assert.Equal(RouteCheckTone.Success, routeRow.ChipTone);
            Assert.Equal(SoundChartsTone.Success, Tones.From(routeRow.ChipTone));
        }
        else
        {
            Assert.Contains(routeRow.ChipTone, new[] { RouteCheckTone.Warning, RouteCheckTone.Destructive });
            Assert.Contains(Tones.From(routeRow.ChipTone), new[] { SoundChartsTone.Warning, SoundChartsTone.Danger });
        }
    }

    [Fact]
    public void State_FollowsTheChecksLifecycle()
    {
        Assert.Equal(
            [RouteCheckPaneState.Empty, RouteCheckPaneState.Working, RouteCheckPaneState.Failed],
            new[] { RouteCheckPresenter.Empty, RouteCheckPresenter.Working("Route"), RouteCheckPresenter.Failed("Route", "No.") }.Select(v => v.State));
    }
}
