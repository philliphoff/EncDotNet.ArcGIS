namespace EncDotNet.SoundCharts.Tests;

/// <summary>Finds test data: the committed fixtures, and optional upstream datasets.</summary>
internal static class Fixtures
{
    /// <summary>
    /// The environment variable naming EncDotNet.S100's <c>tests/datasets</c>
    /// folder. Tests that need real S-102, S-104 or S-111 data skip without it.
    /// CI sets it to a sparse checkout of the EncDotNet.S100 release the add-in
    /// pins.
    /// </summary>
    public const string UpstreamDatasetsVariable = "SOUNDCHARTS_S100_TEST_DATASETS";

    /// <summary>Gets the full path of a committed fixture.</summary>
    public static string PathOf(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    /// <summary>
    /// Gets the full path of a file under EncDotNet.S100's <c>tests/datasets</c>
    /// folder, or <c>null</c> when <see cref="UpstreamDatasetsVariable"/> isn't
    /// set or the file isn't there.
    /// </summary>
    public static string? Upstream(string relativePath)
    {
        var root = Environment.GetEnvironmentVariable(UpstreamDatasetsVariable);
        if (string.IsNullOrWhiteSpace(root))
        {
            return null;
        }

        var path = Path.Combine(root, relativePath);
        return File.Exists(path) ? path : null;
    }

    /// <summary>
    /// Copies Hurricane Ian's advisory 20 (EncDotNet.S100's
    /// <c>tests/datasets/TC/al092022</c>: the forecast, wind radii and best
    /// track zips) into <paramref name="folder"/>, creating it, or returns
    /// <see langword="false"/> when <see cref="UpstreamDatasetsVariable"/>
    /// isn't set.
    /// </summary>
    /// <param name="folder">The folder to copy into.</param>
    /// <param name="atcfId">
    /// The storm id to name the copies by, such as <c>ep012022</c>, for a
    /// second storm; <see langword="null"/> keeps <c>al092022</c>.
    /// </param>
    /// <param name="advisory">The advisory number to name the forecast and wind radii zips by; <see langword="null"/> keeps <c>020</c>.</param>
    public static bool CopyUpstreamStorm(string folder, string? atcfId = null, string? advisory = null)
    {
        if (Upstream(Path.Combine("TC", "al092022", "al092022_5day_020.zip")) is not { } forecast)
        {
            return false;
        }

        Directory.CreateDirectory(folder);
        foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(forecast)!, "*.zip"))
        {
            var name = Path.GetFileName(file).Replace("al092022", atcfId ?? "al092022", StringComparison.Ordinal);
            if (advisory is not null)
            {
                name = name.Replace("_020.zip", $"_{advisory}.zip", StringComparison.Ordinal);
            }

            File.Copy(file, Path.Combine(folder, name), overwrite: true);
        }

        return true;
    }
}
