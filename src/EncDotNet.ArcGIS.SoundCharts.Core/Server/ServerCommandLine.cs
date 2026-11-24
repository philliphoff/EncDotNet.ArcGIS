using System.Globalization;
using EncDotNet.S100.Datasets.TropicalCyclones;
using EncDotNet.SoundCharts.Charts;

namespace EncDotNet.SoundCharts.Server;

/// <summary>
/// Builds the <c>s100 tiles serve</c> arguments for one start of a chart
/// server (design brief §4.3, D19).
/// </summary>
public static class ServerCommandLine
{
    /// <summary>
    /// Gets the arguments that serve <paramref name="datasets"/> at
    /// <paramref name="endpoint"/>: loopback only, the endpoint's port and
    /// token, no file watching (<c>--refresh 0</c>, the add-in restarts on a
    /// change), no preview page, and the shared tile cache.
    /// </summary>
    /// <param name="datasets">What to serve.</param>
    /// <param name="endpoint">The port and token.</param>
    /// <param name="tileCache">The <c>--cache-dir</c> folder, or <see langword="null"/> for none.</param>
    /// <param name="tileCacheMegabytes">The <c>--cache-dir-mb</c> cap.</param>
    /// <param name="workingDirectory">
    /// The server's working folder, where a GRIB2 run served beside other
    /// datasets is written as one file (see <see cref="Inputs"/>);
    /// <see langword="null"/> for none.
    /// </param>
    /// <exception cref="NotSupportedException">The server can't take this combination of sources (see <see cref="Inputs"/>).</exception>
    public static IReadOnlyList<string> Build(
        DatasetSet datasets, ServerEndpoint endpoint, string? tileCache, int tileCacheMegabytes, string? workingDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(datasets);
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentOutOfRangeException.ThrowIfLessThan(tileCacheMegabytes, 1);

        List<string> arguments = ["tiles", "serve", .. Inputs(datasets, workingDirectory)];
        arguments.AddRange(
        [
            "--host", ServerEndpoint.Host,
            "--port", endpoint.Port.ToString(CultureInfo.InvariantCulture),
            "--token", endpoint.Token,
            "--refresh", "0",
            "--no-viewer",
        ]);
        if (tileCache is not null)
        {
            arguments.AddRange(["--cache-dir", tileCache, "--cache-dir-mb", tileCacheMegabytes.ToString(CultureInfo.InvariantCulture)]);
        }

        arguments.AddRange(datasets.Options.ToArguments());
        return arguments;
    }

    /// <summary>
    /// Gets the input arguments for <paramref name="datasets"/>: one source
    /// as the positional input, whatever its kind; several dataset files as
    /// repeated <c>--layer</c>. Each source is given by its
    /// <see cref="InputPath"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>tiles serve</c> takes one exchange set or folder, or any number of
    /// files, but not both, nor two exchange sets. Until it can (U2), a set
    /// like that can't be served by one server.
    /// </para>
    /// <para>
    /// A GRIB2 model run (<see cref="DatasetSource.IsRunFolder"/>, issue
    /// #73) is read as one dataset only from a folder given as the input:
    /// <c>tiles serve</c> refuses a folder as a <c>--layer</c>, and each of
    /// the run's files as a <c>--layer</c> of its own would be drawn at every
    /// time. Served alone, the run is the input, with <c>--only GRIB2</c> so
    /// that a storm's advisory zips or charts beside it aren't drawn. Served
    /// beside other datasets, or when the folder has sub-folders the server
    /// would also read, the run's files are written one after another into
    /// one file in <paramref name="workingDirectory"/>
    /// (<see cref="RunFolderInput"/>), which the server reads as the same
    /// run.
    /// </para>
    /// </remarks>
    /// <param name="datasets">What to serve.</param>
    /// <param name="workingDirectory">
    /// The folder a run served beside other datasets is written to;
    /// <see langword="null"/> when there is none, and such a set can't be
    /// served.
    /// </param>
    /// <exception cref="NotSupportedException">
    /// The set mixes an exchange set or folder with other sources, or has a
    /// GRIB2 run among several datasets without a
    /// <paramref name="workingDirectory"/>.
    /// </exception>
    /// <exception cref="IOException">A run's file couldn't be written.</exception>
    public static IReadOnlyList<string> Inputs(DatasetSet datasets, string? workingDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(datasets);

        if (datasets.Sources.Count == 1)
        {
            var source = datasets.Sources[0];
            if (IsRun(source) && (workingDirectory is null || !RunFolderInput.HasSubfolders(source.Path)))
            {
                return [source.Path, "--only", S100.Core.SpecName.Grib2];
            }

            return [InputPath(source, workingDirectory)];
        }

        if (datasets.Sources.Any(source => source.Kind != DatasetSourceKind.Dataset))
        {
            throw new NotSupportedException(
                "The chart server takes one exchange set or folder, or several dataset files, but not a mix of them.");
        }

        return [.. datasets.Sources.SelectMany(source => new[] { "--layer", InputPath(source, workingDirectory) })];
    }

    /// <summary>
    /// Gets the path the server is given for <paramref name="source"/>: its
    /// own path, except for a dataset that is an NHC tropical cyclone
    /// advisory folder, which is given as its latest forecast zip, and one
    /// that is a GRIB2 model run, which is given as the run's files written
    /// into one file in <paramref name="workingDirectory"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Library lists a folder of one storm's zips as one dataset at the
    /// folder (issue #69). <c>tiles serve</c> refuses a folder as a
    /// <c>--layer</c>, and reads a folder given as its input as a folder of
    /// datasets, any charts beside the storm's zips included. The forecast
    /// zip is read with the wind radii and best track zips beside it, so the
    /// server draws the same advisory. <see cref="DataRevision"/> still
    /// covers the whole folder, so a newer advisory saved there restarts the
    /// server on it.
    /// </para>
    /// <para>
    /// The Library lists a GRIB2 run as one dataset at its folder too (issue
    /// #73). A file of the run's files, one after another, is what a NOMADS
    /// GRIB filter download of several hours is, and the server reads it as
    /// the same run (<see cref="RunFolderInput"/>). A newer forecast hour
    /// saved in the folder is a new revision, and the server restarts on a
    /// new file.
    /// </para>
    /// </remarks>
    /// <param name="source">The source.</param>
    /// <param name="workingDirectory">The folder a run's file is written to; <see langword="null"/> for none.</param>
    /// <exception cref="NotSupportedException"><paramref name="source"/> is a GRIB2 run and there is no <paramref name="workingDirectory"/>.</exception>
    /// <exception cref="IOException">The run's file couldn't be written.</exception>
    public static string InputPath(DatasetSource source, string? workingDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Kind != DatasetSourceKind.Dataset || !Directory.Exists(source.Path))
        {
            return source.Path;
        }

        if (DatasetSource.IsAdvisoryFolder(source.Path) && NhcAdvisoryFiles.Find(source.Path) is { } advisory)
        {
            return advisory.ForecastZip;
        }

        if (DatasetSource.IsRunFolder(source.Path))
        {
            return workingDirectory is null
                ? throw new NotSupportedException(
                    $"The chart server reads the GRIB2 run {source.Name} beside other datasets only from one file, and there is no folder to write it to.")
                : RunFolderInput.Write(source.Path, workingDirectory);
        }

        return source.Path;
    }

    private static bool IsRun(DatasetSource source) =>
        source.Kind == DatasetSourceKind.Dataset && DatasetSource.IsRunFolder(source.Path);
}
