namespace EncDotNet.SoundCharts.Settings;

/// <summary>
/// The settings SoundCharts layers draw with now: the open project's
/// mariner settings, the application settings, and Pro's theme (design
/// brief §5). The add-in keeps one; ribbon controls and Options pages change
/// it, and the chart layer manager applies <see cref="Changed"/>.
/// </summary>
/// <remarks>Thread-safe. <see cref="Changed"/> is raised outside the lock.</remarks>
public sealed class ChartSettings
{
    private readonly Lock _gate = new();
    private MarinerSettings _project;
    private ApplicationSettings _application;
    private bool _darkTheme;

    /// <summary>Creates settings with <paramref name="application"/>'s project defaults.</summary>
    /// <param name="application">The application settings; <see cref="ApplicationSettings.Default"/> when <see langword="null"/>.</param>
    public ChartSettings(ApplicationSettings? application = null)
    {
        _application = application ?? ApplicationSettings.Default;
        _project = _application.ProjectDefaults;
    }

    /// <summary>Raised after any setting changes, with what that means for the layers.</summary>
    public event EventHandler<ChartSettingsChangedEventArgs>? Changed;

    /// <summary>Gets the project's palette and mariner settings.</summary>
    public MarinerSettings Project
    {
        get
        {
            lock (_gate)
            {
                return _project;
            }
        }
    }

    /// <summary>Gets the application settings.</summary>
    public ApplicationSettings Application
    {
        get
        {
            lock (_gate)
            {
                return _application;
            }
        }
    }

    /// <summary>Gets whether Pro shows its dark theme.</summary>
    public bool DarkTheme
    {
        get
        {
            lock (_gate)
            {
                return _darkTheme;
            }
        }
    }

    /// <summary>Gets what the layers draw with now.</summary>
    public EffectiveChartSettings Effective
    {
        get
        {
            lock (_gate)
            {
                return EffectiveChartSettings.From(_project, _application, _darkTheme);
            }
        }
    }

    /// <summary>Sets the project's settings.</summary>
    /// <param name="project">The new settings.</param>
    /// <exception cref="ArgumentException"><paramref name="project"/> isn't valid (<see cref="MarinerSettings.Validate"/>).</exception>
    public void SetProject(MarinerSettings project)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (project.Validate() is { Count: > 0 } problems)
        {
            throw new ArgumentException($"The mariner settings aren't valid: {string.Join(", ", problems)}.", nameof(project));
        }

        Update(() => _project = project, isProject: true);
    }

    /// <summary>Changes the project's settings with <paramref name="change"/>.</summary>
    /// <param name="change">Gives the new settings from the current ones.</param>
    public void UpdateProject(Func<MarinerSettings, MarinerSettings> change)
    {
        ArgumentNullException.ThrowIfNull(change);
        SetProject(change(Project));
    }

    /// <summary>
    /// Chooses a palette by hand. While <see cref="ApplicationSettings.FollowProTheme"/>
    /// is on, that turns it off: the hand choice wins (D26).
    /// </summary>
    /// <param name="palette">The palette.</param>
    public void ChoosePalette(ChartPalette palette)
    {
        Update(
            () =>
            {
                _project = _project with { Palette = palette };
                if (_application.FollowProTheme)
                {
                    _application = _application with { FollowProTheme = false };
                }
            },
            isProject: true);
    }

    /// <summary>Sets the application settings; the project's settings stay.</summary>
    /// <param name="application">The new settings.</param>
    public void SetApplication(ApplicationSettings application)
    {
        ArgumentNullException.ThrowIfNull(application);
        Update(() => _application = application, isProject: false);
    }

    /// <summary>Starts a project without settings of its own: it takes the application's project defaults.</summary>
    public void ResetProject() => Update(() => _project = _application.ProjectDefaults, isProject: true);

    /// <summary>Sets whether Pro shows its dark theme.</summary>
    /// <param name="dark">Whether the theme is dark.</param>
    public void SetDarkTheme(bool dark) => Update(() => _darkTheme = dark, isProject: false);

    private void Update(Action change, bool isProject)
    {
        EffectiveChartSettings before;
        EffectiveChartSettings after;
        lock (_gate)
        {
            var project = _project;
            var application = _application;
            var dark = _darkTheme;
            before = EffectiveChartSettings.From(project, application, dark);
            change();
            if (Equals(project, _project) && Equals(application, _application) && dark == _darkTheme)
            {
                return;
            }

            after = EffectiveChartSettings.From(_project, _application, _darkTheme);
            isProject = isProject && !Equals(project, _project);
        }

        Changed?.Invoke(this, new ChartSettingsChangedEventArgs(before, after, isProject));
    }
}
