using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Resources;
using Microsoft.Win32;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>Add SoundCharts Data</b> (<c>SoundCharts_AddData</c> in Config.daml):
/// chooses dataset files, exchange-set catalogues or <c>.zip</c> exchange
/// sets and adds them to the active map (design brief §2.1).
/// </summary>
/// <remarks>
/// A Windows file dialog, not Pro's: Pro's browser shows <c>.000</c> cells as
/// its own ENC items, and the add-in claims no file types (epic #1). The
/// filter only helps find files; what each one is comes from its content.
/// </remarks>
internal sealed class AddDataButton : Button
{
    protected override async void OnClick()
    {
        try
        {
            if (MapView.Active is not { } view)
            {
                return;
            }

            var dialog = new OpenFileDialog
            {
                Title = Strings.AddData_FileDialogTitle,
                Filter = Strings.AddData_FileFilter,
                Multiselect = true,
                CheckFileExists = true,
            };
            if (dialog.ShowDialog(FrameworkApplication.Current.MainWindow) == true)
            {
                await AddSoundChartsData.RunAsync(view, dialog.FileNames);
            }
        }
        catch (Exception ex)
        {
            // The boundary with Pro: log rather than let the exception reach Pro.
            AddInLog.Write(LogLevel.Error, $"Add SoundCharts Data failed: {ex}");
        }
    }
}
