using System.Globalization;
using EncDotNet.SoundCharts.ObjectInfo;
using EncDotNet.SoundCharts.Resources;

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
