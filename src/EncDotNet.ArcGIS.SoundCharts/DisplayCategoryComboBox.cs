using ArcGIS.Desktop.Framework.Contracts;
using EncDotNet.SoundCharts.Settings;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The <b>Display category</b> combo box (<c>SoundCharts_DisplayCategory</c>,
/// D5): <b>Display Base</b>, <b>Standard</b>, <b>Other Information</b>,
/// <b>All</b>. The chart server has no display category yet (U1), so it
/// shows the project's setting but stays disabled until the server takes it.
/// </summary>
internal sealed class DisplayCategoryComboBox : ComboBox
{
    private readonly Dictionary<ComboBoxItem, DisplayCategory> _categories = [];
    private bool _updating;

    /// <summary>Creates the combo box with the four categories.</summary>
    public DisplayCategoryComboBox()
    {
        foreach (var choice in SettingLabels.DisplayCategories)
        {
            var item = new ComboBoxItem(choice.Label);
            _categories[item] = choice.Value;
            Add(item);
        }

        Select(SoundChartsSession.Settings.Project.DisplayCategory);
    }

    /// <inheritdoc/>
    protected override void OnSelectionChange(ComboBoxItem item)
    {
        if (_updating || item is null || !_categories.TryGetValue(item, out var category) || !ChartServerSupport.Supports(MarinerSetting.DisplayCategory))
        {
            return;
        }

        SettingsHost.UpdateProject(settings => settings with { DisplayCategory = category });
    }

    /// <inheritdoc/>
    protected override void OnUpdate()
    {
        Enabled = ChartServerSupport.Supports(MarinerSetting.DisplayCategory);
        Select(SoundChartsSession.Settings.Project.DisplayCategory);
    }

    private void Select(DisplayCategory category)
    {
        var item = _categories.FirstOrDefault(pair => pair.Value == category).Key;
        if (item is null || ReferenceEquals(SelectedItem, item))
        {
            return;
        }

        _updating = true;
        try
        {
            SelectedItem = item;
        }
        finally
        {
            _updating = false;
        }
    }
}
