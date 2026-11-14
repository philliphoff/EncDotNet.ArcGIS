using System.Globalization;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Resources;
using EncDotNet.SoundCharts.Settings;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The <b>SoundCharts</b> page under <b>Project</b> in Pro's Options
/// (<c>SoundCharts_ProjectOptionsPage</c>, D24): the project's palette and
/// mariner settings, with the viewer's labels and defaults (D25).
/// </summary>
/// <remarks>
/// The page edits a draft. <b>OK</b> or <b>Apply</b> commits it to the
/// project, and the layers redraw at once (§5.5). Settings the chart server
/// doesn't take yet are shown, disabled, under a note saying so.
/// </remarks>
internal sealed class ProjectOptionsViewModel : Page
{
    /// <summary>The page's DAML id.</summary>
    public const string PageId = "SoundCharts_ProjectOptionsPage";

    private MarinerSettings _draft = MarinerSettings.Default;
    private string? _note;

    /// <summary>Creates the page.</summary>
    public ProjectOptionsViewModel()
    {
        ResetToDefaultsCommand = new RelayCommand(() => Load(SoundChartsSession.Settings.Application.ProjectDefaults, modified: true));
        UseForNewProjectsCommand = new RelayCommand(UseForNewProjects, () => _draft.Validate().Count == 0);
    }

    /// <summary>Gets the palettes.</summary>
    public IReadOnlyList<SettingChoice<ChartPalette>> Palettes => SettingLabels.Palettes;

    /// <summary>Gets the display categories.</summary>
    public IReadOnlyList<SettingChoice<DisplayCategory>> DisplayCategories => SettingLabels.DisplayCategories;

    /// <summary>Gets whether the chart server takes the display category.</summary>
    public bool DisplayCategoryEnabled => ChartServerSupport.Supports(MarinerSetting.DisplayCategory);

    /// <summary>Gets whether the chart server takes the contours, depth shades and S-101 switches.</summary>
    public bool PortrayalEnabled => ChartServerSupport.Supports(MarinerSetting.SafetyContour);

    /// <summary>Gets whether the chart server can hide soundings.</summary>
    public bool SoundingsEnabled => ChartServerSupport.Supports(MarinerSetting.Soundings);

    /// <summary>Gets whether any setting on the page waits for a newer chart server.</summary>
    public bool ShowNeedsServer => !DisplayCategoryEnabled || !PortrayalEnabled || !SoundingsEnabled;

    /// <summary>Gets or sets the palette.</summary>
    public SettingChoice<ChartPalette> Palette
    {
        get => Palettes.First(p => p.Value == _draft.Palette);
        set => Edit(_draft with { Palette = value?.Value ?? _draft.Palette });
    }

    /// <summary>Gets or sets the display category.</summary>
    public SettingChoice<DisplayCategory> DisplayCategory
    {
        get => DisplayCategories.First(c => c.Value == _draft.DisplayCategory);
        set => Edit(_draft with { DisplayCategory = value?.Value ?? _draft.DisplayCategory });
    }

    /// <summary>Gets or sets the safety contour in metres.</summary>
    public double SafetyContour
    {
        get => _draft.SafetyContour;
        set => Edit(_draft with { SafetyContour = value });
    }

    /// <summary>Gets or sets the safety depth in metres.</summary>
    public double SafetyDepth
    {
        get => _draft.SafetyDepth;
        set => Edit(_draft with { SafetyDepth = value });
    }

    /// <summary>Gets or sets the shallow contour in metres.</summary>
    public double ShallowContour
    {
        get => _draft.ShallowContour;
        set => Edit(_draft with { ShallowContour = value });
    }

    /// <summary>Gets or sets the deep contour in metres.</summary>
    public double DeepContour
    {
        get => _draft.DeepContour;
        set => Edit(_draft with { DeepContour = value });
    }

    /// <summary>Gets or sets the four-shade depth scheme.</summary>
    public bool FourShades
    {
        get => _draft.FourShades;
        set => Edit(_draft with { FourShades = value });
    }

    /// <summary>Gets or sets highlighting shallow-water dangers.</summary>
    public bool ShallowWaterDangers
    {
        get => _draft.ShallowWaterDangers;
        set => Edit(_draft with { ShallowWaterDangers = value });
    }

    /// <summary>Gets or sets plain boundaries.</summary>
    public bool PlainBoundaries
    {
        get => _draft.PlainBoundaries;
        set => Edit(_draft with { PlainBoundaries = value });
    }

    /// <summary>Gets or sets simplified symbols.</summary>
    public bool SimplifiedSymbols
    {
        get => _draft.SimplifiedSymbols;
        set => Edit(_draft with { SimplifiedSymbols = value });
    }

    /// <summary>Gets or sets full light lines.</summary>
    public bool FullLightLines
    {
        get => _draft.FullLightLines;
        set => Edit(_draft with { FullLightLines = value });
    }

    /// <summary>Gets or sets ignoring the scale minimum.</summary>
    public bool IgnoreScaleMinimum
    {
        get => _draft.IgnoreScaleMinimum;
        set => Edit(_draft with { IgnoreScaleMinimum = value });
    }

    /// <summary>Gets or sets showing soundings.</summary>
    public bool ShowSoundings
    {
        get => _draft.ShowSoundings;
        set => Edit(_draft with { ShowSoundings = value });
    }

    /// <summary>Gets or sets showing text.</summary>
    public bool ShowText
    {
        get => _draft.ShowText;
        set => Edit(_draft with { ShowText = value });
    }

    /// <summary>Gets or sets the symbol scale.</summary>
    public double SymbolScale
    {
        get => _draft.SymbolScale;
        set => Edit(_draft with { SymbolScale = Math.Round(value, 1) });
    }

    /// <summary>Gets or sets the text scale.</summary>
    public double TextScale
    {
        get => _draft.TextScale;
        set => Edit(_draft with { TextScale = Math.Round(value, 1) });
    }

    /// <summary>Gets the symbol scale as text, such as <c>1.0×</c>.</summary>
    public string SymbolScaleText => string.Format(CultureInfo.CurrentCulture, Strings.Settings_ScaleFormat, _draft.SymbolScale);

    /// <summary>Gets the text scale as text, such as <c>1.0×</c>.</summary>
    public string TextScaleText => string.Format(CultureInfo.CurrentCulture, Strings.Settings_ScaleFormat, _draft.TextScale);

    /// <summary>Gets or sets the national language code.</summary>
    public string NationalLanguage
    {
        get => _draft.NationalLanguage;
        set => Edit(_draft with { NationalLanguage = value?.Trim() ?? string.Empty });
    }

    /// <summary>Gets what's wrong with the draft, one problem a line, or <see langword="null"/>.</summary>
    public string? Problems => _draft.Validate() is { Count: > 0 } problems
        ? string.Join(Environment.NewLine, problems.Select(SettingLabels.Message))
        : null;

    /// <summary>Gets a line about the last action, such as after <b>Use for New Projects</b>.</summary>
    public string? Note
    {
        get => _note;
        private set => SetProperty(ref _note, value);
    }

    /// <summary>Gets the command that puts the draft back to the defaults for new projects.</summary>
    public ICommand ResetToDefaultsCommand { get; }

    /// <summary>Gets the command that makes the draft the defaults for new projects.</summary>
    public ICommand UseForNewProjectsCommand { get; }

    /// <inheritdoc/>
    protected override Task InitializeAsync()
    {
        Load(SoundChartsSession.Settings.Project, modified: false);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task CommitAsync()
    {
        try
        {
            if (_draft.Validate().Count == 0)
            {
                SoundChartsSession.Settings.SetProject(_draft);
            }
        }
        catch (Exception ex)
        {
            // The boundary with Pro (D22).
            AddInLog.Write(LogLevel.Error, $"Applying the mariner settings failed: {ex}");
        }

        return Task.CompletedTask;
    }

    private void UseForNewProjects()
    {
        var application = SoundChartsSession.Settings.Application with { ProjectDefaults = _draft };
        if (SettingsHost.SaveApplication(application))
        {
            Note = Strings.Settings_DefaultsSaved;
        }
    }

    private void Load(MarinerSettings settings, bool modified)
    {
        _draft = settings;
        Note = null;
        NotifyPropertyChanged(string.Empty);
        Validate();
        if (modified)
        {
            IsModified = true;
        }
    }

    private void Edit(MarinerSettings draft)
    {
        if (draft == _draft)
        {
            return;
        }

        _draft = draft;
        Note = null;
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
