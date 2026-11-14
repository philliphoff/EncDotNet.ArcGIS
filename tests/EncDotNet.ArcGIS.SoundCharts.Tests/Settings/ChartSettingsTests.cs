using EncDotNet.SoundCharts.Settings;

namespace EncDotNet.SoundCharts.Tests.Settings;

public class ChartSettingsTests
{
    [Fact]
    public void ANewSession_StartsFromTheApplicationDefaults()
    {
        var defaults = MarinerSettings.Default with { Palette = ChartPalette.Dusk, SymbolScale = 1.5 };

        var settings = new ChartSettings(new ApplicationSettings { ProjectDefaults = defaults });

        Assert.Equal(defaults, settings.Project);
    }

    [Fact]
    public void APaletteChange_IsARestyle_WithTheNewStyle()
    {
        var settings = new ChartSettings();
        var changes = Record(settings);

        settings.UpdateProject(s => s with { Palette = ChartPalette.Night });

        var change = Assert.Single(changes);
        Assert.Equal(ChartSettingsChange.Restyle, change.Change);
        Assert.True(change.ProjectChanged);
        Assert.Equal("night", change.After.Style);
        Assert.Equal("day", change.Before.Style);
    }

    [Fact]
    public void AStartUpOptionChange_IsARestart()
    {
        var settings = new ChartSettings();
        var changes = Record(settings);

        settings.UpdateProject(s => s with { ShowText = false });

        Assert.Equal(ChartSettingsChange.Restart, Assert.Single(changes).Change);
        Assert.False(settings.Effective.RenderOptions.ShowText);
    }

    [Fact]
    public void ASettingTheServerDoesntTake_ChangesNoLayer_ButIsSaved()
    {
        var settings = new ChartSettings();
        var changes = Record(settings);

        settings.UpdateProject(s => s with { SafetyContour = 10, ShallowContour = 2, DeepContour = 20 });

        var change = Assert.Single(changes);
        Assert.Equal(ChartSettingsChange.None, change.Change);
        Assert.True(change.ProjectChanged);
    }

    [Fact]
    public void TheSameSettings_RaiseNothing()
    {
        var settings = new ChartSettings();
        var changes = Record(settings);

        settings.SetProject(MarinerSettings.Default);
        settings.ChoosePalette(ChartPalette.Day);

        Assert.Empty(changes);
    }

    [Fact]
    public void InvalidSettings_AreRejected_AndNothingChanges()
    {
        var settings = new ChartSettings();

        Assert.Throws<ArgumentException>(() => settings.SetProject(MarinerSettings.Default with { SymbolScale = 10 }));
        Assert.Equal(MarinerSettings.Default, settings.Project);
    }

    [Theory]
    [InlineData(false, ChartPalette.Day)]
    [InlineData(true, ChartPalette.Dusk)]
    public void FollowProTheme_GivesDayInLightAndDuskInDark(bool dark, ChartPalette expected)
    {
        var settings = new ChartSettings(new ApplicationSettings { FollowProTheme = true });
        settings.SetProject(MarinerSettings.Default with { Palette = ChartPalette.Night });

        settings.SetDarkTheme(dark);

        Assert.Equal(expected, settings.Effective.Palette);
    }

    [Fact]
    public void WithoutFollowProTheme_TheThemeDoesntMatter()
    {
        var settings = new ChartSettings();
        settings.SetProject(MarinerSettings.Default with { Palette = ChartPalette.Night });

        settings.SetDarkTheme(true);

        Assert.Equal(ChartPalette.Night, settings.Effective.Palette);
    }

    [Fact]
    public void ChoosingAPaletteByHand_TurnsFollowProThemeOff()
    {
        var settings = new ChartSettings(new ApplicationSettings { FollowProTheme = true });
        settings.SetDarkTheme(true);
        var changes = Record(settings);

        settings.ChoosePalette(ChartPalette.Night);

        Assert.False(settings.Application.FollowProTheme);
        Assert.Equal(ChartPalette.Night, settings.Effective.Palette);
        Assert.Equal(ChartSettingsChange.Restyle, Assert.Single(changes).Change);
    }

    [Fact]
    public void TurningFollowProThemeOn_IsARestyle_ThatLeavesTheProjectAlone()
    {
        var settings = new ChartSettings();
        settings.SetProject(MarinerSettings.Default with { Palette = ChartPalette.Night });
        var changes = Record(settings);

        settings.SetApplication(settings.Application with { FollowProTheme = true });

        var change = Assert.Single(changes);
        Assert.Equal(ChartSettingsChange.Restyle, change.Change);
        Assert.False(change.ProjectChanged);
        Assert.Equal(ChartPalette.Night, settings.Project.Palette);
    }

    [Fact]
    public void ResetProject_TakesTheApplicationDefaults()
    {
        var defaults = MarinerSettings.Default with { TextScale = 2 };
        var settings = new ChartSettings(new ApplicationSettings { ProjectDefaults = defaults });
        settings.SetProject(MarinerSettings.Default with { ShowText = false });

        settings.ResetProject();

        Assert.Equal(defaults, settings.Project);
    }

    private static List<ChartSettingsChangedEventArgs> Record(ChartSettings settings)
    {
        var changes = new List<ChartSettingsChangedEventArgs>();
        settings.Changed += (_, args) => changes.Add(args);
        return changes;
    }
}
