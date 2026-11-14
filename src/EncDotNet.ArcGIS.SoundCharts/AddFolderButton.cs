using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Resources;
using Microsoft.Win32;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>Add SoundCharts Folder</b> (<c>SoundCharts_AddFolder</c> in
/// Config.daml): chooses an exchange-set folder, or any folder of datasets,
/// and adds it to the active map.
/// </summary>
internal sealed class AddFolderButton : Button
{
    protected override async void OnClick()
    {
        try
        {
            if (MapView.Active is not { } view)
            {
                return;
            }

            var dialog = new OpenFolderDialog
            {
                Title = Strings.AddData_FolderDialogTitle,
                Multiselect = true,
            };
            if (dialog.ShowDialog(FrameworkApplication.Current.MainWindow) == true)
            {
                await AddSoundChartsData.RunAsync(view, dialog.FolderNames);
            }
        }
        catch (Exception ex)
        {
            // The boundary with Pro: log rather than let the exception reach Pro.
            AddInLog.Write(LogLevel.Error, $"Add SoundCharts Folder failed: {ex}");
        }
    }
}
