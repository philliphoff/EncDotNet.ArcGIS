using System.IO;
using System.Windows;
using EncDotNet.SoundCharts.Resources;
using Microsoft.Win32;

namespace EncDotNet.SoundCharts.Export;

/// <summary>The <b>Export SoundCharts Chart</b> window: file, format and resolution.</summary>
internal partial class ExportChartWindow
{
    private readonly ExportChartViewModel _model;

    /// <summary>Creates the window over <paramref name="model"/>.</summary>
    public ExportChartWindow(ExportChartViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        _model = model;
        DataContext = model;
        InitializeComponent();
    }

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        string? folder = null;
        try
        {
            folder = Path.GetDirectoryName(_model.OutputPath);
        }
        catch (ArgumentException)
        {
            // Not a path yet; the dialog starts where it likes.
        }

        var dialog = new SaveFileDialog
        {
            Title = Strings.Export_FileDialogTitle,
            Filter = _model.FileFilter,
            FileName = Path.GetFileName(_model.OutputPath),
            InitialDirectory = Directory.Exists(folder) ? folder : string.Empty,
            AddExtension = true,
            OverwritePrompt = true,
        };
        if (dialog.ShowDialog(this) == true)
        {
            _model.OutputPath = dialog.FileName;
        }
    }

    private void OnExportClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
