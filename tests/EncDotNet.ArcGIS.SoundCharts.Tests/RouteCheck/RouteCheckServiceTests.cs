using EncDotNet.S100.DataModel;
using EncDotNet.S100.Navigation;
using EncDotNet.SoundCharts.ObjectInfo;
using EncDotNet.SoundCharts.RouteCheck;

namespace EncDotNet.SoundCharts.Tests.RouteCheck;

/// <summary>
/// Runs <see cref="RouteCheckService"/>, the findings-to-features mapping and
/// the pane's presenter against EncDotNet.S100's own test data: the
/// US5MA1BO S-57 cell, and the S-421 and RTZ route fixtures. Skipped without
/// <see cref="Fixtures.UpstreamDatasetsVariable"/>.
/// </summary>
public sealed class RouteCheckServiceTests : IDisposable
{
    private const string Cell = "S57/US5MA1BO/US5MA1BO.000";

    // Across US5MA1BO (W -70.35 S 41.25 E -70.275 N 41.325), corner to corner.
    private static readonly GeoPosition[] AcrossTheCell =
    [
        new(41.255, -70.345),
        new(41.29, -70.31),
        new(41.32, -70.28),
    ];

    private static readonly DateTimeOffset Now = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

    private readonly RouteCheckService _service = new();
    private readonly CultureScope _culture = new();

    public void Dispose() => _culture.Dispose();

    [Fact]
    public void Check_AcrossTheCell_FindsWhatTheViewerWould()
    {
        var chart = Chart();
        var route = RouteLines.FromLine("Across", RouteSource.Sketch, AcrossTheCell);

        var outcome = _service.Check(route, [chart], ShipParameters.Default, Now, TestContext.Current.CancellationToken);

        Assert.Empty(outcome.Problems);
        Assert.Same(route, outcome.Route);
        Assert.NotEmpty(outcome.Report.Findings);
        Assert.Contains("US5MA1BO.000", outcome.Report.DatasetsUsed);
        Assert.All(outcome.Report.Findings, f => Assert.False(string.IsNullOrWhiteSpace(f.Title)));
        Assert.Equal(["S-57"], outcome.Products);

        // The route crosses the 30 m safety contour, in the viewer's words.
        var crossing = Assert.Single(outcome.Report.Findings, f => f.Code == RouteFindingCodes.CrossesSafetyContour && f.LegIndex == 0);
        Assert.Equal(RouteFindingGroup.MustReview, crossing.Group);
        Assert.Equal("Crosses the safety contour", crossing.Title);
        Assert.StartsWith("Leg 1 → 2 · ", crossing.Meta);
        Assert.Equal("DepthArea", crossing.Feature?.FeatureType);

        // No draft on the route or the Options page: no clearance check.
        Assert.False(outcome.Prepared.Parameters.HasDraft);
        Assert.Null(outcome.Report.Clearance);
    }

    [Fact]
    public void Check_FindingsBecomePointAndSegmentFeatures()
    {
        var route = RouteLines.FromLine("Across", RouteSource.Sketch, AcrossTheCell);
        var outcome = _service.Check(route, [Chart()], ShipParameters.Default, Now, TestContext.Current.CancellationToken);

        var features = FindingFeatures.From(outcome.Report.Findings);

        var located = outcome.Report.Findings.Where(f => f.Location is { Path.Count: > 0 }).ToList();
        Assert.NotEmpty(located);
        Assert.Equal(located.Select(f => f.Id), features.Select(f => f.FindingId));
        foreach (var (finding, feature) in located.Zip(features))
        {
            Assert.Equal(finding.Location!.Path.Count == 1 ? FindingShape.Point : FindingShape.Segment, feature.Shape);
            Assert.Equal(finding.Code.Value, feature.Rule);
            Assert.Equal(finding.Title, feature.Title);
            Assert.Equal(finding.Meta, feature.Message);
            Assert.Equal(FindingFeatures.SeverityOf(finding.Group), feature.Severity);
            Assert.All(feature.Path, p => Assert.InRange(p.Latitude, 41.2, 41.4));
            if (finding.Feature is { } chartFeature)
            {
                Assert.StartsWith("US5MA1BO.000 ", feature.Features);
                Assert.EndsWith(chartFeature.FeatureId, feature.Features);
            }
        }
    }

    [Fact]
    public void Check_WithADraft_ChecksClearance()
    {
        var route = RouteLines.FromLine("Across", RouteSource.Sketch, AcrossTheCell);
        var ship = ShipParameters.Default with { Draft = 7.0, UkcMargin = 1.0 };

        var outcome = _service.Check(route, [Chart()], ship, Now, TestContext.Current.CancellationToken);

        Assert.True(outcome.Prepared.Parameters.HasDraft);
        Assert.NotNull(outcome.Report.Clearance);
        Assert.Equal(["S-57", "S-101"], outcome.Products);
        var view = RouteCheckPresenter.Present(outcome);
        Assert.Contains(view.Rows, r => r.Label == "Clearance" && r.Text != "Not checked");

        // With a draft, the worst finding to review is the title, as in the viewer.
        Assert.Equal("Crosses the safety contour", view.Title);
        Assert.EndsWith(" · and 1 more to review", view.Subtitle);
        Assert.Equal("Charted depth from S-101 on 4.9 NM", view.Confidence);
        Assert.Equal(
            "Draft 7.0 m · UKC margin 1.0 m · Safety contour 30.0 m · Safety depth 30.0 m",
            view.Parameters);
    }

    [Fact]
    public void Present_GroupsFindingsAndNamesTheProducts()
    {
        var route = RouteLines.FromLine("Across", RouteSource.Sketch, AcrossTheCell);
        var outcome = _service.Check(route, [Chart()], ShipParameters.Default, Now, TestContext.Current.CancellationToken);

        var view = RouteCheckPresenter.Present(outcome);

        Assert.Equal("Across", view.Heading);
        Assert.Equal(outcome.Report.Findings.Count, view.Items.Count);
        Assert.Equal(view.Items.Select(i => i.GroupRank).Order(), view.Items.Select(i => i.GroupRank));
        string[] groups = ["MUST REVIEW", "CHECK", "INFO"];
        Assert.All(view.Items, i => Assert.Contains(i.Group, groups));
        Assert.StartsWith("Not for navigation · ", view.Footer);
        Assert.Equal(["Clearance", "Route"], view.Rows.Take(2).Select(r => r.Label));
        Assert.Equal("Not checked", view.Rows[0].Text);
        // Without a draft the viewer counts what to review and names the worst.
        Assert.Equal("2 to review", view.Title);
        Assert.StartsWith("Crosses the safety contour · Crosses the safety contour · ", view.Subtitle);
        Assert.EndsWith(" to check", view.Subtitle);
        Assert.Equal(RouteCheckTone.Destructive, view.Tone);
        Assert.Equal("Not for navigation · computed from S-57 for your settings", view.Footer);
        Assert.Equal(
            "No draft · UKC margin no margin set · Safety contour 30.0 m · Safety depth 30.0 m",
            view.Parameters);
    }

    [Fact]
    public void Check_S421RouteOffTheChart_LeavesChartCoverage()
    {
        var path = Upstream("S421/RTE-TEST-GBASIC.s421.gml");
        var route = RouteFiles.Read(path);

        var outcome = _service.Check(route, [Chart()], ShipParameters.Default, Now, TestContext.Current.CancellationToken);

        Assert.Contains(outcome.Report.Findings, f => f.Code == RouteFindingCodes.OutsideChartCoverage);
        Assert.Equal(RouteCheckVerdictKind.Check, outcome.Report.Verdict.Kind);
        var view = RouteCheckPresenter.Present(outcome);
        Assert.Equal(RouteCheckTone.Warning, view.Tone);
        Assert.Equal("Not for navigation · no data loaded for this route", view.Footer);
    }

    [Fact]
    public void Check_WithoutDatasets_SaysNoDataLoaded()
    {
        var route = RouteLines.FromLine("Nowhere", RouteSource.Sketch, AcrossTheCell);

        var outcome = _service.Check(route, [], ShipParameters.Default, Now, TestContext.Current.CancellationToken);

        Assert.Empty(outcome.Products);
        Assert.Equal("Not for navigation · no data loaded for this route", RouteCheckPresenter.Present(outcome).Footer);
    }

    [Fact]
    public async Task CheckAsync_RunsOffTheCallingThreadAndHonoursCancellation()
    {
        var route = RouteLines.FromLine("Across", RouteSource.Sketch, AcrossTheCell);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _service.CheckAsync(route, [Chart()], ShipParameters.Default, Now, cancelled.Token));
    }

    private static ChartDataset Chart() => new(Upstream(Cell));

    private static string Upstream(string relativePath)
    {
        var path = Fixtures.Upstream(relativePath);
        Assert.SkipWhen(path is null, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        return path!;
    }
}
