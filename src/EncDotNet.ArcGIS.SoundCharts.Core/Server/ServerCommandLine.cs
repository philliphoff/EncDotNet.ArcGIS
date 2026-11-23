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
    /// <exception cref="NotSupportedException">The server can't take this combination of sources (see <see cref="Inputs"/>).</exception>
    public static IReadOnlyList<string> Build(DatasetSet datasets, ServerEndpoint endpoint, string? tileCache, int tileCacheMegabytes)
    {
        ArgumentNullException.ThrowIfNull(datasets);
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentOutOfRangeException.ThrowIfLessThan(tileCacheMegabytes, 1);

        List<string> arguments = ["tiles", "serve", .. Inputs(datasets)];
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
    /// <c>tiles serve</c> takes one exchange set or folder, or any number of
    /// files, but not both, nor two exchange sets. Until it can (U2), a set
    /// like that can't be served by one server.
    /// </remarks>
    /// <param name="datasets">What to serve.</param>
    /// <exception cref="NotSupportedException">The set mixes an exchange set or folder with other sources.</exception>
    public static IReadOnlyList<string> Inputs(DatasetSet datasets)
    {
        ArgumentNullException.ThrowIfNull(datasets);

        if (datasets.Sources.Count == 1)
        {
            return [InputPath(datasets.Sources[0])];
        }

        if (datasets.Sources.Any(source => source.Kind != DatasetSourceKind.Dataset))
        {
            throw new NotSupportedException(
                "The chart server takes one exchange set or folder, or several dataset files, but not a mix of them.");
        }

        return [.. datasets.Sources.SelectMany(source => new[] { "--layer", InputPath(source) })];
    }

    /// <summary>
    /// Gets the path the server is given for <paramref name="source"/>: its
    /// own path, except for a dataset that is an NHC tropical cyclone
    /// advisory folder, which is given as its latest forecast zip.
    /// </summary>
    /// <remarks>
    /// The Library lists a folder of one storm's zips as one dataset at the
    /// folder (issue #69). <c>tiles serve</c> refuses a folder as a
    /// <c>--layer</c>, and reads a folder given as its input as a folder of
    /// datasets, any charts beside the storm's zips included. The forecast
    /// zip is read with the wind radii and best track zips beside it, so the
    /// server draws the same advisory. <see cref="DataRevision"/> still
    /// covers the whole folder, so a newer advisory saved there restarts the
    /// server on it.
    /// </remarks>
    /// <param name="source">The source.</param>
    public static string InputPath(DatasetSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Kind == DatasetSourceKind.Dataset
            && DatasetSource.IsAdvisoryFolder(source.Path)
            && NhcAdvisoryFiles.Find(source.Path) is { } advisory
                ? advisory.ForecastZip
                : source.Path;
    }
}
