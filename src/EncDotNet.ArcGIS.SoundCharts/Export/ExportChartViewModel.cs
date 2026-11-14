using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts.Export;

/// <summary>
/// The <b>Export SoundCharts Chart</b> window's choices: file, format and
/// resolution. No Pro types, so it's tested without Pro.
/// </summary>
internal sealed class ExportChartViewModel : INotifyPropertyChanged
{
    private string _outputPath;
    private ExportFormatOption _format;
    private ExportResolutionOption _resolution;

    /// <summary>Creates the choices, starting from <paramref name="outputPath"/>.</summary>
    /// <param name="layoutName">The layout being exported, for the heading.</param>
    /// <param name="outputPath">The suggested file; its extension picks the format (PDF otherwise).</param>
    /// <param name="layerCount">The number of SoundCharts layers the export renders.</param>
    public ExportChartViewModel(string layoutName, string outputPath, int layerCount)
    {
        ArgumentNullException.ThrowIfNull(layoutName);
        ArgumentNullException.ThrowIfNull(outputPath);

        Heading = string.Format(CultureInfo.CurrentCulture, Strings.Export_Heading, layoutName);
        Summary = layerCount == 1
            ? Strings.Export_OneLayer
            : string.Format(CultureInfo.CurrentCulture, Strings.Export_LayerCount, layerCount);
        Formats = [.. ChartExportFormats.All.Select(f => new ExportFormatOption(f, LabelOf(f)))];
        Resolutions = [.. ChartExportFormats.Resolutions.Select(dpi => new ExportResolutionOption(dpi))];

        var format = ChartExportFormats.FromPath(outputPath) ?? ChartExportFormat.Pdf;
        _format = Formats.First(f => f.Format == format);
        _resolution = Resolutions.First(r => r.Dpi == ChartExportFormats.DefaultResolution);
        _outputPath = outputPath.Length == 0 ? outputPath : ChartExportFormats.WithExtension(outputPath, format);
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the window's heading.</summary>
    public string Heading { get; }

    /// <summary>Gets how many SoundCharts layers the export renders.</summary>
    public string Summary { get; }

    /// <summary>Gets the formats offered.</summary>
    public IReadOnlyList<ExportFormatOption> Formats { get; }

    /// <summary>Gets the resolutions offered.</summary>
    public IReadOnlyList<ExportResolutionOption> Resolutions { get; }

    /// <summary>Gets or sets the chosen format; changing it changes the file's extension.</summary>
    public ExportFormatOption Format
    {
        get => _format;
        set
        {
            if (value is null || value == _format)
            {
                return;
            }

            _format = value;
            OnPropertyChanged();
            if (_outputPath.Length > 0)
            {
                OutputPath = ChartExportFormats.WithExtension(_outputPath, value.Format);
            }
        }
    }

    /// <summary>Gets or sets the chosen resolution.</summary>
    public ExportResolutionOption Resolution
    {
        get => _resolution;
        set
        {
            if (value is null || value == _resolution)
            {
                return;
            }

            _resolution = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the file to write; a known extension picks the format.</summary>
    public string OutputPath
    {
        get => _outputPath;
        set
        {
            value ??= string.Empty;
            if (value == _outputPath)
            {
                return;
            }

            _outputPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanExport));
            if (ChartExportFormats.FromPath(value) is { } format && format != _format.Format)
            {
                _format = Formats.First(f => f.Format == format);
                OnPropertyChanged(nameof(Format));
            }
        }
    }

    /// <summary>Gets whether the choices can be exported: a full path in an existing folder.</summary>
    public bool CanExport
    {
        get
        {
            try
            {
                return Path.IsPathFullyQualified(_outputPath)
                    && Path.GetFileName(_outputPath).Length > 0
                    && Directory.Exists(Path.GetDirectoryName(_outputPath));
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }

    /// <summary>Gets the save dialog's filter for the chosen format.</summary>
    public string FileFilter => FilterOf(_format.Format);

    /// <summary>Gets the settings to export with, the path given the format's extension.</summary>
    public ChartExportSettings ToSettings() =>
        new(ChartExportFormats.WithExtension(_outputPath, _format.Format), _format.Format, _resolution.Dpi);

    private static string LabelOf(ChartExportFormat format) => format switch
    {
        ChartExportFormat.Pdf => Strings.Export_FormatPdf,
        ChartExportFormat.Png => Strings.Export_FormatPng,
        ChartExportFormat.Jpeg => Strings.Export_FormatJpeg,
        ChartExportFormat.Tiff => Strings.Export_FormatTiff,
        _ => format.ToString(),
    };

    private static string FilterOf(ChartExportFormat format) => format switch
    {
        ChartExportFormat.Pdf => Strings.Export_FilterPdf,
        ChartExportFormat.Png => Strings.Export_FilterPng,
        ChartExportFormat.Jpeg => Strings.Export_FilterJpeg,
        ChartExportFormat.Tiff => Strings.Export_FilterTiff,
        _ => string.Empty,
    };

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
