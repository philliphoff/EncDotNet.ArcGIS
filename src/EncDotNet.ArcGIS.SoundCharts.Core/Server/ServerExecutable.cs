using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EncDotNet.SoundCharts.Server;

/// <summary>
/// The <c>s100</c> program that runs chart servers: the pinned build bundled
/// in the add-in for this machine's architecture, or a path that overrides
/// it (design brief §4.3, D21).
/// </summary>
/// <param name="Path">The full path of <c>s100.exe</c>.</param>
/// <param name="RuntimeIdentifier">The runtime it was built for, such as <c>win-arm64</c>, or <see langword="null"/> for an override.</param>
/// <param name="IsOverride">Whether the path came from the override rather than the bundle.</param>
public sealed record ServerExecutable(string Path, string? RuntimeIdentifier, bool IsOverride)
{
    /// <summary>The program's file name.</summary>
    public const string FileName = "s100.exe";

    /// <summary>The folder, beside the add-in assembly, that holds one folder per runtime.</summary>
    public const string BundleFolder = "server";

    /// <summary>
    /// The environment variable that overrides the bundled server with
    /// another <c>s100.exe</c>, for testing a newer build. Slice A5's Options
    /// page replaces it.
    /// </summary>
    public const string OverrideVariable = "SOUNDCHARTS_S100_PATH";

    /// <summary>
    /// Gets the program's version, its informational version such as
    /// <c>0.27.1-main.20261113+c62d2eb…</c>, or <see langword="null"/> when
    /// it can't be read.
    /// </summary>
    public string? Version => ReadVersion(Path);

    /// <summary>
    /// Gets the runtime to prefer on a machine of <paramref name="osArchitecture"/>:
    /// <c>win-arm64</c> on Arm64 Windows, where the server runs natively
    /// although Pro runs as x64, and <c>win-x64</c> everywhere else.
    /// </summary>
    /// <param name="osArchitecture">The operating system's architecture.</param>
    public static string PreferredRuntimeIdentifier(Architecture osArchitecture) =>
        osArchitecture == Architecture.Arm64 ? "win-arm64" : "win-x64";

    /// <summary>
    /// Finds the program: <paramref name="overridePath"/> when given, else
    /// <c>server\{rid}\s100.exe</c> under <paramref name="baseDirectory"/>
    /// for the preferred runtime, else the win-x64 build, which also runs on
    /// Arm64 under emulation.
    /// </summary>
    /// <param name="baseDirectory">The folder the add-in runs from.</param>
    /// <param name="osArchitecture">The operating system's architecture.</param>
    /// <param name="overridePath">A path to use instead of the bundle, or <see langword="null"/>.</param>
    /// <returns>The program, or <see langword="null"/> when none exists.</returns>
    public static ServerExecutable? Locate(string baseDirectory, Architecture osArchitecture, string? overridePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            var full = System.IO.Path.GetFullPath(Environment.ExpandEnvironmentVariables(overridePath.Trim().Trim('"')));
            return File.Exists(full) ? new ServerExecutable(full, null, IsOverride: true) : null;
        }

        var preferred = PreferredRuntimeIdentifier(osArchitecture);
        string[] runtimes = preferred == "win-x64" ? ["win-x64"] : [preferred, "win-x64"];
        foreach (var rid in runtimes)
        {
            var candidate = System.IO.Path.Combine(baseDirectory, BundleFolder, rid, FileName);
            if (File.Exists(candidate))
            {
                return new ServerExecutable(System.IO.Path.GetFullPath(candidate), rid, IsOverride: false);
            }
        }

        return null;
    }

    /// <summary>Finds the program the add-in in <paramref name="baseDirectory"/> uses on this machine.</summary>
    /// <param name="baseDirectory">The folder the add-in runs from.</param>
    public static ServerExecutable? LocateForCurrentMachine(string baseDirectory) =>
        Locate(baseDirectory, RuntimeInformation.OSArchitecture, Environment.GetEnvironmentVariable(OverrideVariable));

    private static string? ReadVersion(string path)
    {
        try
        {
            var version = FileVersionInfo.GetVersionInfo(path).ProductVersion;
            return string.IsNullOrWhiteSpace(version) ? null : version;
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }
}
