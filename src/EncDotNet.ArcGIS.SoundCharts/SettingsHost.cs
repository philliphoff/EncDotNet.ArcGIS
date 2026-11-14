using System.IO;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Settings;

namespace EncDotNet.SoundCharts;

/// <summary>
/// Connects <see cref="SoundChartsSession.Settings"/> to Pro (design brief
/// §5, D23–D26): reads and writes the project's settings with the
/// <c>.aprx</c>, starts a project without settings from the application
/// defaults, follows Pro's theme, and saves application settings to the
/// user's settings file.
/// </summary>
/// <remarks>
/// Pro has no theme-changed event: a theme change takes effect when Pro
/// restarts, so reading the theme at start-up is enough for Follow Pro
/// theme.
/// </remarks>
internal static class SettingsHost
{
    /// <summary>The module settings key that holds the project's settings, as JSON.</summary>
    public const string ProjectKey = "SoundCharts.MarinerSettings";

    private static bool _started;
    private static bool _readForProject;

    private static ChartSettings Settings => SoundChartsSession.Settings;

    /// <summary>Reads Pro's theme and starts following project and settings changes.</summary>
    public static void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        Settings.SetDarkTheme(FrameworkApplication.ApplicationTheme == ApplicationTheme.Dark);
        Settings.Changed += OnChanged;

        // Pro reads a project's module settings as it loads, before
        // ProjectOpenedEvent. A project that brought none starts from the
        // application defaults; this runs before the chart layer manager
        // connects the project's layers, as it subscribes first.
        ProjectOpenedEvent.Subscribe(_ => Guard(() =>
        {
            if (!_readForProject)
            {
                Settings.ResetProject();
            }
        }));
        ProjectClosedEvent.Subscribe(_ => _readForProject = false);
    }

    /// <summary>Reads the project's settings when it opens; a project without them takes the defaults.</summary>
    /// <param name="settings">The project's module settings.</param>
    public static void Read(ModuleSettingsReader settings)
    {
        Guard(() =>
        {
            _readForProject = true;
            var json = settings.Get(ProjectKey) as string;
            if (SettingsJson.ReadMariner(json, Settings.Application.ProjectDefaults) is { } project)
            {
                Settings.SetProject(project);
                AddInLog.Write(LogLevel.Debug, "Read the project's mariner settings.");
            }
            else
            {
                Settings.ResetProject();
            }
        });
    }

    /// <summary>Writes the project's settings when it's saved.</summary>
    /// <param name="settings">The project's module settings.</param>
    public static void Write(ModuleSettingsWriter settings) =>
        Guard(() => settings.Add(ProjectKey, SettingsJson.ToJson(Settings.Project)));

    /// <summary>Sets the application settings and saves them to the user's settings file.</summary>
    /// <param name="application">The new settings.</param>
    /// <returns>Whether the file was saved; the settings apply either way.</returns>
    public static bool SaveApplication(ApplicationSettings application)
    {
        Settings.SetApplication(application);
        return SaveApplicationFile();
    }

    /// <summary>
    /// Chooses the project's palette by hand. That turns Follow Pro theme
    /// off when it's on (D26), and saves that to the settings file.
    /// </summary>
    /// <param name="palette">The palette.</param>
    public static void ChoosePalette(ChartPalette palette)
    {
        var following = Settings.Application.FollowProTheme;
        Settings.ChoosePalette(palette);
        if (following)
        {
            SaveApplicationFile();
        }
    }

    /// <summary>Changes the project's settings, as a ribbon toggle does; an invalid change is logged and dropped.</summary>
    /// <param name="change">Gives the new settings from the current ones.</param>
    public static void UpdateProject(Func<MarinerSettings, MarinerSettings> change) =>
        Guard(() => Settings.UpdateProject(change));

    /// <summary>Turns Follow Pro theme on or off, and saves it.</summary>
    /// <param name="follow">Whether the palette follows Pro's theme.</param>
    public static void SetFollowProTheme(bool follow)
    {
        if (Settings.Application.FollowProTheme != follow)
        {
            SaveApplication(Settings.Application with { FollowProTheme = follow });
        }
    }

    private static bool SaveApplicationFile()
    {
        try
        {
            SoundChartsSession.SettingsFile.Save(Settings.Application);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AddInLog.Write(LogLevel.Error, $"The SoundCharts settings couldn't be saved to {SoundChartsSession.SettingsFile.Path}: {ex.Message}");
            return false;
        }
    }

    private static void OnChanged(object? sender, ChartSettingsChangedEventArgs args)
    {
        if (args.ProjectChanged && Project.Current is { } project)
        {
            Guard(() => project.SetDirty(true));
        }
    }

    private static void Guard(Action work)
    {
        try
        {
            work();
        }
        catch (Exception ex)
        {
            // The boundary with Pro (D22).
            AddInLog.Write(LogLevel.Error, $"SoundCharts settings failed: {ex}");
        }
    }
}
