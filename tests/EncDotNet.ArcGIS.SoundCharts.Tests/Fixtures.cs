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
}
