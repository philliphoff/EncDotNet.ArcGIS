using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>Mariner Settings...</b> (<c>SoundCharts_MarinerSettingsButton</c>,
/// D5): opens Pro's Options at the project's <b>SoundCharts</b> page.
/// </summary>
internal sealed class MarinerSettingsButton : Button
{
    /// <summary>Pro's Options property sheet.</summary>
    public const string OptionsSheet = "esri_core_optionsPropertySheet";

    /// <inheritdoc/>
    protected override void OnClick()
    {
        try
        {
            PropertySheet.ShowDialog(OptionsSheet, ProjectOptionsViewModel.PageId);
        }
        catch (Exception ex)
        {
            // The boundary with Pro (D22).
            AddInLog.Write(LogLevel.Error, $"Opening the SoundCharts Options page failed: {ex}");
        }
    }
}
