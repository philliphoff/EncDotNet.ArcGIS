using System.IO;

namespace EncDotNet.SoundCharts;

/// <summary>
/// Appends timestamped lines to the add-in's log file,
/// <c>%LOCALAPPDATA%\SoundCharts for ArcGIS\Logs\addin.log</c>.
/// </summary>
/// <remarks>
/// Writing never throws an I/O error: a logging failure must not stop the
/// add-in or Pro. The file is started afresh once it passes 1 MB.
/// </remarks>
internal static class AddInLog
{
    private const long MaxLength = 1024 * 1024;

    /// <summary>Gets the path of the log file.</summary>
    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SoundCharts for ArcGIS",
        "Logs",
        "addin.log");

    /// <summary>Appends <paramref name="message"/> with a UTC timestamp.</summary>
    public static void Write(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath) ?? ".");
            var file = new FileInfo(FilePath);
            if (file.Exists && file.Length > MaxLength)
            {
                file.Delete();
            }

            File.AppendAllText(FilePath, $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // There is nowhere to report it; carry on without the log.
        }
    }
}
