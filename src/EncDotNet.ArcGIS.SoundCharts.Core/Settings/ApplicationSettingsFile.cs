namespace EncDotNet.SoundCharts.Settings;

/// <summary>
/// The user's SoundCharts application settings file,
/// <c>%LOCALAPPDATA%\SoundCharts\ArcGIS\settings.json</c>.
/// </summary>
public sealed class ApplicationSettingsFile
{
    /// <summary>The file name.</summary>
    public const string FileName = "settings.json";

    /// <summary>Creates the file in <paramref name="folder"/>.</summary>
    /// <param name="folder">The folder, such as <see cref="SoundChartsFolders.Root"/>.</param>
    public ApplicationSettingsFile(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        Path = System.IO.Path.Combine(folder, FileName);
    }

    /// <summary>Gets the file's path.</summary>
    public string Path { get; }

    /// <summary>
    /// Reads the settings; the defaults when there's no file or it can't be
    /// read. Never throws.
    /// </summary>
    /// <param name="problem">Why the file couldn't be read, or <see langword="null"/>.</param>
    public ApplicationSettings Load(out string? problem)
    {
        problem = null;
        try
        {
            if (!File.Exists(Path))
            {
                return ApplicationSettings.Default;
            }

            if (SettingsJson.ReadApplication(File.ReadAllText(Path)) is { } settings)
            {
                return settings;
            }

            problem = $"{Path} isn't a SoundCharts settings file; using the defaults.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            problem = $"{Path} couldn't be read: {ex.Message}";
        }

        return ApplicationSettings.Default;
    }

    /// <summary>Writes <paramref name="settings"/>, replacing the file whole.</summary>
    /// <param name="settings">The settings.</param>
    /// <exception cref="IOException">The file couldn't be written.</exception>
    /// <exception cref="UnauthorizedAccessException">The folder isn't writable.</exception>
    public void Save(ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path) ?? ".");
        var temporary = Path + ".tmp";
        File.WriteAllText(temporary, SettingsJson.ToJson(settings));
        File.Move(temporary, Path, overwrite: true);
    }
}
