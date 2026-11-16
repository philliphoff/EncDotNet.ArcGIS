using System.IO;
using ArcGIS.Desktop.Framework;
using EncDotNet.SoundCharts.Resources;
using Microsoft.Win32;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The <b>Browse...</b> beside the <c>s100</c> program on the application
/// Options page: an Open dialog for the <c>s100.exe</c> to run.
/// </summary>
internal static class ServerPathPicker
{
    /// <summary>
    /// Asks for the <c>s100.exe</c> to run, starting in the folder of
    /// <paramref name="current"/>.
    /// </summary>
    /// <param name="current">The path set now, or empty for the bundled one.</param>
    /// <returns>The chosen path, or <see langword="null"/> when cancelled.</returns>
    public static string? Pick(string? current)
    {
        var dialog = new OpenFileDialog
        {
            Title = Strings.Settings_ServerPathDialogTitle,
            Filter = Strings.Settings_ServerPathFilter,
            InitialDirectory = InitialDirectory(current) ?? string.Empty,
            CheckFileExists = true,
            Multiselect = false,
        };
        return dialog.ShowDialog(FrameworkApplication.Current.MainWindow) == true ? dialog.FileName : null;
    }

    /// <summary>
    /// Gets the folder the dialog starts in: the folder of
    /// <paramref name="current"/>, with environment variables expanded, when
    /// it exists.
    /// </summary>
    /// <param name="current">The path set now, or empty for the bundled one.</param>
    /// <returns>The folder, or <see langword="null"/> to let the dialog choose.</returns>
    public static string? InitialDirectory(string? current)
    {
        if (string.IsNullOrWhiteSpace(current))
        {
            return null;
        }

        try
        {
            var path = Path.GetFullPath(Environment.ExpandEnvironmentVariables(current.Trim().Trim('"')));
            var folder = Path.GetDirectoryName(path);
            return Directory.Exists(folder) ? folder : null;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            // Not a path yet; the dialog starts where it likes.
            return null;
        }
    }
}
