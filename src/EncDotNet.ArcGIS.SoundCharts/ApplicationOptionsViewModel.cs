using System.IO;
using ArcGIS.Desktop.Framework.Contracts;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Resources;
using EncDotNet.SoundCharts.Settings;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The <b>SoundCharts</b> page under <b>Application</b> in Pro's Options
/// (<c>SoundCharts_ApplicationOptionsPage</c>, D24): Follow Pro theme, Time
/// Format, Depth Unit, the <c>s100</c> program and the tile cache size. The
/// mariner settings new projects start with are set from the project page's
/// <b>Use for New Projects</b>.
/// </summary>
internal sealed class ApplicationOptionsViewModel : Page
{
    private ApplicationSettings _draft = ApplicationSettings.Default;

    /// <summary>Gets the time formats.</summary>
    public IReadOnlyList<SettingChoice<TimeFormat>> TimeFormats => SettingLabels.TimeFormats;

    /// <summary>Gets the depth units.</summary>
    public IReadOnlyList<SettingChoice<DepthUnit>> DepthUnits => SettingLabels.DepthUnits;

    /// <summary>Gets whether the chart server takes a depth unit.</summary>
    public bool DepthUnitEnabled => ChartServerSupport.Supports(MarinerSetting.SafetyContour);

    /// <summary>Gets or sets Follow Pro theme.</summary>
    public bool FollowProTheme
    {
        get => _draft.FollowProTheme;
        set => Edit(_draft with { FollowProTheme = value });
    }

    /// <summary>Gets or sets the time format.</summary>
    public SettingChoice<TimeFormat> TimeFormat
    {
        get => TimeFormats.First(t => t.Value == _draft.TimeFormat);
        set => Edit(_draft with { TimeFormat = value?.Value ?? _draft.TimeFormat });
    }

    /// <summary>Gets or sets the depth unit.</summary>
    public SettingChoice<DepthUnit> DepthUnit
    {
        get => DepthUnits.First(d => d.Value == _draft.DepthUnit);
        set => Edit(_draft with { DepthUnit = value?.Value ?? _draft.DepthUnit });
    }

    /// <summary>Gets or sets the <c>s100.exe</c> to run, or empty for the bundled one.</summary>
    public string ServerPath
    {
        get => _draft.ServerPath ?? string.Empty;
        set => Edit(_draft with { ServerPath = string.IsNullOrWhiteSpace(value) ? null : value.Trim().Trim('"') });
    }

    /// <summary>Gets or sets the tile cache size in megabytes.</summary>
    public int TileCacheMegabytes
    {
        get => _draft.TileCacheMegabytes;
        set => Edit(_draft with { TileCacheMegabytes = value });
    }

    /// <summary>Gets what's wrong with the draft, or <see langword="null"/>.</summary>
    public string? Problems
    {
        get
        {
            List<string> problems = [];
            if (_draft.TileCacheMegabytes is < ApplicationSettings.MinTileCacheMegabytes or > ApplicationSettings.MaxTileCacheMegabytes)
            {
                problems.Add(Strings.Settings_Problem_TileCache);
            }

            if (_draft.ServerPath is { } path && !File.Exists(Environment.ExpandEnvironmentVariables(path)))
            {
                problems.Add(Strings.Settings_Problem_ServerPath);
            }

            return problems.Count == 0 ? null : string.Join(Environment.NewLine, problems);
        }
    }

    /// <inheritdoc/>
    protected override Task InitializeAsync()
    {
        _draft = SoundChartsSession.Settings.Application;
        NotifyPropertyChanged(string.Empty);
        Validate();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task CommitAsync()
    {
        try
        {
            if (Problems is null)
            {
                // Keep the project defaults Use for New Projects may have saved meanwhile.
                SettingsHost.SaveApplication(_draft with { ProjectDefaults = SoundChartsSession.Settings.Application.ProjectDefaults });
            }
        }
        catch (Exception ex)
        {
            // The boundary with Pro (D22).
            AddInLog.Write(LogLevel.Error, $"Saving the SoundCharts settings failed: {ex}");
        }

        return Task.CompletedTask;
    }

    private void Edit(ApplicationSettings draft)
    {
        if (draft == _draft)
        {
            return;
        }

        _draft = draft;
        NotifyPropertyChanged(string.Empty);
        Validate();
        IsModified = true;
    }

    private void Validate()
    {
        var problems = Problems;
        IsValid = problems is null;
        ValidationMessage = problems ?? string.Empty;
    }
}
