using System.Globalization;
using EncDotNet.SoundCharts.Settings;

namespace EncDotNet.SoundCharts.Tests.Settings;

public class ApplicationSettingsFileTests
{
    [Fact]
    public void WithoutAFile_TheDefaultsLoad_WithoutAProblem()
    {
        using var temp = new TempFolder();

        var settings = new ApplicationSettingsFile(temp.Path).Load(out var problem);

        Assert.Equal(ApplicationSettings.Default, settings);
        Assert.Null(problem);
    }

    [Fact]
    public void SavedSettings_LoadAgain()
    {
        using var temp = new TempFolder();
        var file = new ApplicationSettingsFile(temp.Path);
        var settings = new ApplicationSettings
        {
            FollowProTheme = true,
            ProjectDefaults = MarinerSettings.Default with { Palette = ChartPalette.Dusk },
        };

        file.Save(settings);

        Assert.Equal(Path.Combine(temp.Path, "settings.json"), file.Path);
        Assert.Equal(settings, new ApplicationSettingsFile(temp.Path).Load(out _));
        Assert.False(File.Exists(file.Path + ".tmp"));
    }

    [Fact]
    public void ACorruptFile_GivesTheDefaults_AndSaysWhy()
    {
        using var temp = new TempFolder();
        temp.File("settings.json", "{ not json");

        var settings = new ApplicationSettingsFile(temp.Path).Load(out var problem);

        Assert.Equal(ApplicationSettings.Default, settings);
        Assert.NotNull(problem);
    }

    [Theory]
    [InlineData(TimeFormat.Utc, false, "12 Nov 2026 14:05 UTC")]
    [InlineData(TimeFormat.Utc, true, "12 Nov 2026 14:05:09 UTC")]
    public void UtcTimes_AreWrittenTheBriefsWay(TimeFormat format, bool seconds, string expected)
    {
        var time = new DateTimeOffset(2026, 11, 12, 9, 5, 9, TimeSpan.FromHours(-5));

        Assert.Equal(expected, TimeText.Format(time, format, seconds, CultureInfo.GetCultureInfo("fr-FR")));
    }

    [Fact]
    public void LocalTimes_UseTheCulture()
    {
        var time = new DateTimeOffset(2026, 11, 12, 14, 5, 9, TimeSpan.Zero);
        var culture = CultureInfo.GetCultureInfo("en-US");

        Assert.Equal(time.ToLocalTime().DateTime.ToString("g", culture), TimeText.Format(time, TimeFormat.Local, culture: culture));
    }
}
