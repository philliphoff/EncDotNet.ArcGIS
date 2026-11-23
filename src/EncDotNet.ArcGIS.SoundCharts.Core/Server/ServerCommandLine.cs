using System.Globalization;
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
    /// repeated <c>--layer</c>.
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
            return [datasets.Sources[0].Path];
        }

        if (datasets.Sources.Any(source => source.Kind != DatasetSourceKind.Dataset))
        {
            throw new NotSupportedException(
                "The chart server takes one exchange set or folder, or several dataset files, but not a mix of them.");
        }

        return [.. datasets.Sources.SelectMany(source => new[] { "--layer", source.Path })];
    }
}
