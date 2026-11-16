using EncDotNet.SoundCharts.Resources;
using EncDotNet.SoundCharts.Settings;

namespace EncDotNet.SoundCharts.Tests.Settings;

public class SettingLabelsTests
{
    [Fact]
    public void TheDefaults_HaveNoProblems()
    {
        var settings = MarinerSettings.Default;

        Assert.Null(SettingLabels.DisplayProblems(settings));
        Assert.Null(SettingLabels.DepthProblems(settings));
        Assert.Null(SettingLabels.RouteCheckProblems(settings));
    }

    [Fact]
    public void ContoursOutOfOrder_ShowInTheDepthsSectionOnly()
    {
        var settings = MarinerSettings.Default with { ShallowContour = 40 };

        Assert.Equal(Strings.Settings_Problem_ContoursOutOfOrder, SettingLabels.DepthProblems(settings));
        Assert.Null(SettingLabels.DisplayProblems(settings));
        Assert.Null(SettingLabels.RouteCheckProblems(settings));
    }

    [Fact]
    public void ADepthOutOfRange_ShowsInTheDepthsSection()
    {
        var settings = MarinerSettings.Default with { SafetyDepth = -1 };

        Assert.Equal(Strings.Settings_Problem_DepthOutOfRange, SettingLabels.DepthProblems(settings));
        Assert.Null(SettingLabels.RouteCheckProblems(settings));
    }

    [Theory]
    [InlineData(0.0, null)]
    [InlineData(101.0, null)]
    [InlineData(null, -1.0)]
    public void ABadDraftOrMargin_ShowsInTheRouteCheckSectionOnly(double? draft, double? margin)
    {
        var settings = MarinerSettings.Default with { Draft = draft, UkcMargin = margin };

        Assert.Equal(Strings.Settings_Problem_RouteCheckOutOfRange, SettingLabels.RouteCheckProblems(settings));
        Assert.Null(SettingLabels.DepthProblems(settings));
    }

    [Fact]
    public void ABadDraft_DoesNotHideContoursOutOfOrder()
    {
        var settings = MarinerSettings.Default with { Draft = 0, ShallowContour = 40 };

        Assert.Equal(Strings.Settings_Problem_ContoursOutOfOrder, SettingLabels.DepthProblems(settings));
        Assert.NotNull(SettingLabels.RouteCheckProblems(settings));
    }

    [Fact]
    public void ScaleAndLanguage_ShowInTheDisplaySection_OneALine()
    {
        var settings = MarinerSettings.Default with { SymbolScale = 5, NationalLanguage = "fr" };

        var problems = SettingLabels.DisplayProblems(settings);

        Assert.NotNull(problems);
        Assert.Equal(
            [Strings.Settings_Problem_ScaleOutOfRange, Strings.Settings_Problem_NationalLanguageInvalid],
            problems.Split(Environment.NewLine));
        Assert.Null(SettingLabels.DepthProblems(settings));
    }
}
