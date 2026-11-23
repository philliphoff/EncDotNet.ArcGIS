using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Resources;
using Microsoft.Win32;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>Add as Feature Layers</b> (<c>SoundCharts_AddFeatureLayers</c> in
/// Config.daml): chooses datasets or exchange sets and adds their features
/// to the active map as feature layers (issue #15). A Windows file dialog,
/// as for <b>Add SoundCharts Data</b>: the add-in claims no file types.
/// </summary>
internal sealed class AddFeatureLayersButton : Button
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
                Title = Strings.Features_Title,
                Filter = Strings.AddData_FileFilter,
                Multiselect = true,
                CheckFileExists = true,
            };
            if (dialog.ShowDialog(FrameworkApplication.Current.MainWindow) == true)
            {
                await AddSoundChartsFeatures.RunAsync(view, dialog.FileNames);
            }
        }
        catch (Exception ex)
        {
            // The boundary with Pro: log rather than let the exception reach Pro.
            AddInLog.Write(LogLevel.Error, $"Add as Feature Layers failed: {ex}");
        }
    }
}
