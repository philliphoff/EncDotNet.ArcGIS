using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EncDotNet.S100.Datasets.Grib;
using EncDotNet.SoundCharts.Charts;

namespace EncDotNet.SoundCharts.Server;

/// <summary>
/// Writes a GRIB2 model run (<see cref="DatasetSource.IsRunFolder"/>) as one
/// file, for a chart server or tile export that has it beside other datasets
/// (issue #73).
/// </summary>
/// <remarks>
/// <para>
/// <c>s100 tiles serve</c> and <c>tiles export</c> read a run as one dataset
/// only from a folder given as their input, and refuse a folder as a
/// <c>--layer</c>. A GRIB2 file is a sequence of self-contained messages, so
/// the run's files written one after another, in the name order
/// EncDotNet.S100 reads them in (<see cref="GribRunFolder.Files"/>), are the
/// same run: it's what a NOMADS GRIB filter download of several forecast
/// hours is, and the server gives the same time steps and tiles.
/// </para>
/// <para>
/// The file is named after the folder, in a sub-folder named by the run's
/// files, sizes and write times, so a newer hour saved in the folder gives a
/// new file and an unchanged run is written once. It lives in the server's
/// working folder, which goes with the session.
/// </para>
/// </remarks>
public static class RunFolderInput
{
    /// <summary>The folder, under the working folder, that holds the runs' files.</summary>
    public const string FolderName = "runs";

    /// <summary>
    /// Writes <paramref name="folder"/>'s run into one file under
    /// <paramref name="workingDirectory"/>, unless it's there already, and
    /// returns its path.
    /// </summary>
    /// <param name="folder">The run's folder.</param>
    /// <param name="workingDirectory">The server's or export's working folder.</param>
    /// <returns>The file's full path.</returns>
    /// <exception cref="IOException">The folder holds no GRIB2 file, or the file couldn't be written.</exception>
    public static string Write(string folder, string workingDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);

        var files = GribRunFolder.Files(folder);
        if (files.Count == 0)
        {
            throw new IOException($"The folder {folder} holds no GRIB2 file.");
        }

        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)));
        var target = Path.Combine(workingDirectory, FolderName, KeyOf(files), name + ".grib2");
        if (File.Exists(target))
        {
            return target;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var partial = target + ".partial";
        try
        {
            using (var output = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                foreach (var file in files)
                {
                    using var input = File.OpenRead(file);
                    input.CopyTo(output);
                }
            }

            File.Move(partial, target, overwrite: true);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new IOException(ex.Message, ex);
        }
        finally
        {
            if (File.Exists(partial))
            {
                File.Delete(partial);
            }
        }

        return target;
    }

    /// <summary>
    /// Gets whether <paramref name="folder"/> has sub-folders, which a chart
    /// server given the folder as its input would read too.
    /// </summary>
    /// <param name="folder">The run's folder.</param>
    public static bool HasSubfolders(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        try
        {
            return Directory.EnumerateDirectories(folder).Any();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    // The run's files, sizes and write times, as a short hex key.
    private static string KeyOf(IReadOnlyList<string> files)
    {
        var text = new StringBuilder();
        foreach (var file in files)
        {
            var info = new FileInfo(file);
            text.Append(Path.GetFullPath(file).ToUpperInvariant())
                .Append('|').Append(info.Length.ToString(CultureInfo.InvariantCulture))
                .Append('|').Append(info.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture))
                .Append('\n');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())))[..16].ToLowerInvariant();
    }
}
