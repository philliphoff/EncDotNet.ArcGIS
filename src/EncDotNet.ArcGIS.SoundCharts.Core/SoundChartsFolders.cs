using System.Globalization;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The folders SoundCharts keeps on this machine, under
/// <c>%LOCALAPPDATA%\SoundCharts\ArcGIS\</c> on the long path (design brief
/// §4.3, D19).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>sessions\{Pro process id}\</c>: one working folder per chart
/// server, which nothing else writes to; deleted when Pro exits, and on the
/// next start if Pro crashed.</item>
/// <item><c>tile-cache\</c>: the <c>--cache-dir</c> every server shares.</item>
/// <item><c>logs\</c>: the SoundCharts log, one file a day.</item>
/// </list>
/// </remarks>
public sealed class SoundChartsFolders
{
    /// <summary>Creates the folders under <paramref name="root"/> for the process <paramref name="processId"/>.</summary>
    /// <param name="root">The root folder; created, and expanded to its long path.</param>
    /// <param name="processId">The id of the process the session folder belongs to.</param>
    public SoundChartsFolders(string root, int processId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);

        Directory.CreateDirectory(root);
        Root = LongPath.Expand(root);
        ProcessId = processId;
    }

    /// <summary>Gets the root folder, <c>%LOCALAPPDATA%\SoundCharts\ArcGIS</c> by default.</summary>
    public string Root { get; }

    /// <summary>Gets the process the session folder belongs to.</summary>
    public int ProcessId { get; }

    /// <summary>Gets the folder that holds every process's session folder.</summary>
    public string Sessions => Path.Combine(Root, "sessions");

    /// <summary>Gets this process's session folder.</summary>
    public string Session => Path.Combine(Sessions, ProcessId.ToString(CultureInfo.InvariantCulture));

    /// <summary>Gets the tile cache every chart server shares.</summary>
    public string TileCache => Path.Combine(Root, "tile-cache");

    /// <summary>Gets the log folder.</summary>
    public string Logs => Path.Combine(Root, "logs");

    /// <summary>Gets the default folders for the running process.</summary>
    public static SoundChartsFolders ForCurrentProcess() => new(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SoundCharts", "ArcGIS"),
        Environment.ProcessId);

    /// <summary>Gets the working folder of the chart server <paramref name="serverId"/>, creating it.</summary>
    /// <param name="serverId">The server's id, such as its dataset set id.</param>
    public string CreateServerWorkingDirectory(string serverId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverId);

        var folder = Path.Combine(Session, serverId);
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>Deletes this process's session folder. Never throws.</summary>
    public void DeleteSession() => TryDelete(Session);

    /// <summary>
    /// Deletes session folders left by processes that no longer run, such as
    /// a Pro that crashed. Never throws.
    /// </summary>
    /// <param name="isRunning">Whether a process id belongs to a running process.</param>
    /// <returns>The number of folders deleted.</returns>
    public int DeleteStaleSessions(Func<int, bool> isRunning)
    {
        ArgumentNullException.ThrowIfNull(isRunning);

        var deleted = 0;
        try
        {
            if (!Directory.Exists(Sessions))
            {
                return 0;
            }

            foreach (var folder in Directory.EnumerateDirectories(Sessions))
            {
                if (int.TryParse(Path.GetFileName(folder), NumberStyles.None, CultureInfo.InvariantCulture, out var id)
                    && id != ProcessId
                    && !isRunning(id)
                    && TryDelete(folder))
                {
                    deleted++;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best effort: a folder in use stays until the next start.
        }

        return deleted;
    }

    /// <summary>Deletes <paramref name="folder"/> and its contents. Never throws.</summary>
    /// <param name="folder">The folder.</param>
    /// <returns>Whether the folder is gone.</returns>
    internal static bool TryDelete(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
