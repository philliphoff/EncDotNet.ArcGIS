using System.Globalization;
using EncDotNet.SoundCharts.ObjectInfo;
using EncDotNet.SoundCharts.Resources;
using EncDotNet.SoundCharts.Themes;

namespace EncDotNet.SoundCharts.Tests.ObjectInfo;

public class ObjectInfoPresenterTests
{
    private static readonly ChartDataset Cell = new(@"C:\data\101AA00DS0003.000", "AA00DS0003");

    [Fact]
    public void Present_CountsEveryFeatureInTheHeading()
    {
        var view = ObjectInfoPresenter.Present(Report(Feature("a"), Feature("b"), Feature("c")));

        Assert.Equal(string.Format(CultureInfo.CurrentCulture, Strings.ObjectInfo_FeatureCountFormat, 3), view.Heading);
        Assert.Null(view.Description);
        Assert.Equal(3, view.Items.Count);
    }

    [Fact]
    public void Present_SaysOneFeature()
    {
        var view = ObjectInfoPresenter.Present(Report(Feature("a")));

        Assert.Equal(Strings.ObjectInfo_OneFeature, view.Heading);
    }

    [Fact]
    public void Present_SaysWhenNothingIsThere()
    {
        var view = ObjectInfoPresenter.Present(new ObjectInfoReport(1, 2, null, [], []));

        Assert.Equal(Strings.ObjectInfo_NothingHereTitle, view.Heading);
        Assert.Equal(Strings.ObjectInfo_NothingHereDescription, view.Description);
        Assert.Empty(view.Items);
    }

    [Fact]
    public void Present_GroupsByDatasetWithItsProduct()
    {
        var view = ObjectInfoPresenter.Present(Report(Feature("a")));

        var item = Assert.Single(view.Items);
        Assert.Equal("AA00DS0003 · S-101", item.Group);
        Assert.Equal(@"Source: C:\data\101AA00DS0003.000", item.Source);
        Assert.Equal("ID a", item.Caption);
        Assert.Equal("Light", item.Title);
    }

    [Fact]
    public void Present_IndentsSubAttributes()
    {
        var feature = Feature("a") with
        {
            Attributes =
            [
                new("featureName", "Feature name", "", "", 0),
                new("name", "Name", "Bell Rock", "Bell Rock", 1),
            ],
        };

        var rows = ObjectInfoPresenter.Present(Report(feature)).Items.Single().Attributes;

        Assert.Equal("0,0,0,0", rows[0].Indent);
        Assert.Equal("12,0,0,0", rows[1].Indent);
        Assert.Equal("name = Bell Rock", rows[1].ToolTip);
    }

    [Fact]
    public void Present_ListsReferencesAsRoleAndTarget()
    {
        var feature = Feature("a") with { References = [new("AtoNStatus", "info1", "Aton status information")] };

        var item = ObjectInfoPresenter.Present(Report(feature)).Items.Single();

        Assert.Equal(["AtoNStatus → Aton status information"], item.References);
    }

    [Fact]
    public void Present_CarriesTheNotForNavigationMark()
    {
        var report = new ObjectInfoReport(
            1, 2, null, [new(Cell with { NotForNavigation = true }, "S-101", [Feature("a")])], []);

        Assert.True(ObjectInfoPresenter.Present(report).Items.Single().NotForNavigation);
        Assert.False(ObjectInfoPresenter.Present(Report(Feature("a"))).Items.Single().NotForNavigation);
    }

    [Fact]
    public void Present_NotesTheTimeOnlyForCoverageValues()
    {
        var coverage = Feature("(1,2)") with { Kind = ObjectInfoFeatureKind.Coverage };
        var time = new DateTime(2026, 11, 12, 14, 0, 0, DateTimeKind.Utc);

        Assert.Null(ObjectInfoPresenter.Present(Report(Feature("a"))).TimeNote);
        Assert.Equal(Strings.ObjectInfo_TimeDefault, ObjectInfoPresenter.Present(Report(coverage)).TimeNote);
        Assert.Equal(
            string.Format(CultureInfo.CurrentCulture, Strings.ObjectInfo_TimeFormat, ObjectInfoPresenter.FormatUtc(time)),
            ObjectInfoPresenter.Present(Report(coverage) with { Time = time }).TimeNote);
    }

    [Fact]
    public void FormatUtc_FollowsTheBriefsTimeFormat()
    {
        using var culture = new CultureScope("en-GB");

        Assert.Equal("12 Nov 2026 14:00 UTC", ObjectInfoPresenter.FormatUtc(new DateTime(2026, 11, 12, 14, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void Present_ListsProblems()
    {
        var report = new ObjectInfoReport(1, 2, null, [], [new(Cell, "bad header")]);

        Assert.Equal(["Couldn't read AA00DS0003: bad header"], ObjectInfoPresenter.Present(report).Problems);
    }

    [Fact]
    public void Present_ShowsTheLocation()
    {
        using var culture = new CultureScope("en-GB");

        var view = ObjectInfoPresenter.Present(new ObjectInfoReport(36.95, -76.05, null, [], []));

        Assert.Equal("36.9500°, -76.0500°", view.Location);
    }

    [Fact]
    public void States_HaveTheirOwnHeadings()
    {
        Assert.Equal(Strings.ObjectInfo_EmptyTitle, ObjectInfoPresenter.Empty.Heading);
        Assert.Null(ObjectInfoPresenter.Empty.Location);
        Assert.Equal(Strings.ObjectInfo_NoChartLayersTitle, ObjectInfoPresenter.NoChartLayers(1, 2).Heading);
        Assert.Equal(Strings.ObjectInfo_Working, ObjectInfoPresenter.Working(1, 2).Heading);
        Assert.Equal(Strings.ObjectInfo_FailedTitle, ObjectInfoPresenter.Failed.Heading);
    }

    [Fact]
    public void States_SayWhichStateTheyAre()
    {
        Assert.Equal(ObjectInfoState.Empty, ObjectInfoPresenter.Empty.State);
        Assert.Equal(ObjectInfoState.NoChartLayers, ObjectInfoPresenter.NoChartLayers(1, 2).State);
        Assert.Equal(ObjectInfoState.Working, ObjectInfoPresenter.Working(1, 2).State);
        Assert.Equal(ObjectInfoState.Failed, ObjectInfoPresenter.Failed.State);
        Assert.Equal(ObjectInfoState.Results, ObjectInfoPresenter.Present(Report(Feature("a"))).State);
        Assert.Equal(ObjectInfoState.NothingHere, ObjectInfoPresenter.Present(new ObjectInfoReport(1, 2, null, [], [])).State);
    }

    [Fact]
    public void Present_KeepsProblemsWhenNothingIsThere()
    {
        var view = ObjectInfoPresenter.Present(new ObjectInfoReport(1, 2, null, [], [new(Cell, "bad header")]));

        Assert.Equal(ObjectInfoState.NothingHere, view.State);
        Assert.Single(view.Problems);
    }

    [Fact]
    public void Overline_IsTheCountWithResults()
    {
        var view = ObjectInfoPresenter.Present(Report(Feature("a"), Feature("b")));

        Assert.Equal(view.Heading, view.Overline);
    }

    [Fact]
    public void Overline_NamesThePaneOnceThereIsAPosition()
    {
        Assert.Null(ObjectInfoPresenter.Empty.Overline);
        Assert.Null(ObjectInfoPresenter.Failed.Overline);
        Assert.Equal(Strings.ObjectInfo_Overline, ObjectInfoPresenter.Working(1, 2).Overline);
        Assert.Equal(Strings.ObjectInfo_Overline, ObjectInfoPresenter.NoChartLayers(1, 2).Overline);
        Assert.Equal(Strings.ObjectInfo_Overline, ObjectInfoPresenter.Present(new ObjectInfoReport(1, 2, null, [], [])).Overline);
    }

    [Fact]
    public void PositionText_IsInvariantDecimalDegreesLatitudeFirst()
    {
        using var culture = new CultureScope("de-DE");

        var view = ObjectInfoPresenter.Working(36.95, -76.05);

        Assert.Equal("36.950000, -76.050000", view.PositionText);
        Assert.Null(ObjectInfoPresenter.Empty.PositionText);
        Assert.Null(ObjectInfoPresenter.Failed.PositionText);
    }

    [Fact]
    public void Present_SplitsTheGroupIntoProductAndDataset()
    {
        var item = ObjectInfoPresenter.Present(Report(Feature("a"))).Items.Single();

        Assert.Equal("S-101", item.Spec);
        Assert.Equal("AA00DS0003", item.DatasetName);
        Assert.Equal("AA00DS0003 · S-101 · ID a", item.Identity);
    }

    [Fact]
    public void Present_ChoosesTheRowGlyphByKind()
    {
        var coverage = Feature("(1,2)") with { Kind = ObjectInfoFeatureKind.Coverage };
        var warning = new ChartDataset(@"C:\data\navwarn.gml", "navwarn");

        var items = ObjectInfoPresenter.Present(new ObjectInfoReport(
            1,
            2,
            null,
            [new(Cell, "S-101", [Feature("a"), coverage]), new(warning, "S-124", [Feature("part")])],
            [])).Items;

        Assert.Equal(
            [ObjectInfoItemKind.Feature, ObjectInfoItemKind.Coverage, ObjectInfoItemKind.NavigationalWarning],
            items.Select(i => i.Kind));
    }

    [Fact]
    public void Present_TonesAWarningInForceAsACheck()
    {
        var warning = new ChartDataset(@"C:\data\navwarn.gml", "navwarn");
        ObjectInfoItem ItemOf(bool? inForce) => ObjectInfoPresenter.Present(
            new ObjectInfoReport(1, 2, DateTime.UtcNow, [new(warning, "S-124", [Feature("part")], inForce)], [])).Items.Single();

        Assert.Equal(SoundChartsTone.Warning, ItemOf(true).Tone);
        Assert.Equal(SoundChartsTone.Neutral, ItemOf(false).Tone);
        Assert.Equal(SoundChartsTone.Neutral, ItemOf(null).Tone);
        Assert.Equal(SoundChartsTone.Neutral, ObjectInfoPresenter.Present(Report(Feature("a"))).Items.Single().Tone);
    }

    [Fact]
    public void Present_SaysWhetherANavigationalWarningIsInForce()
    {
        var time = new DateTime(2026, 3, 20, 18, 0, 0, DateTimeKind.Utc);
        var warning = new ChartDataset(@"C:\data\navwarn.gml", "navwarn");
        ObjectInfoReport ReportOf(bool? inForce) =>
            new(1, 2, time, [new(warning, "S-124", [Feature("part")], inForce)], []);

        var inForce = ObjectInfoPresenter.Present(ReportOf(true));
        Assert.Equal(Strings.ObjectInfo_InForce, inForce.Items.Single().Validity);
        Assert.Equal(
            string.Format(CultureInfo.CurrentCulture, Strings.ObjectInfo_MapTimeFormat, ObjectInfoPresenter.FormatUtc(time)),
            inForce.TimeNote);
        Assert.Equal(Strings.ObjectInfo_NotInForce, ObjectInfoPresenter.Present(ReportOf(false)).Items.Single().Validity);

        var unknown = ObjectInfoPresenter.Present(ReportOf(null));
        Assert.Null(unknown.Items.Single().Validity);
        Assert.Null(unknown.TimeNote);
    }

    private static ObjectInfoReport Report(params ObjectInfoFeature[] features) =>
        new(1, 2, null, [new(Cell, "S-101", features)], []);

    private static ObjectInfoFeature Feature(string id) =>
        new(id, "LIGHTS", "Light", ObjectInfoFeatureKind.Point, 3.0, [], [], Geometry: null);

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

        public CultureScope(string name)
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _culture;
            CultureInfo.CurrentUICulture = _uiCulture;
        }
    }
}
