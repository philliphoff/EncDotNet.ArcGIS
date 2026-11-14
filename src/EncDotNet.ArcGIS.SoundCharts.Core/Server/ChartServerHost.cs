using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts.Server;

/// <summary>
/// What every chart server shares: the program, the folders, the log, and
/// the means to start processes and check their health.
/// </summary>
/// <remarks>
/// The add-in creates one for Pro's process with
/// <see cref="ProcessServerLauncher"/> and <see cref="HttpHealthProbe"/>;
/// tests create theirs with fakes.
/// </remarks>
public sealed class ChartServerHost
{
    /// <summary>Creates a host.</summary>
    /// <param name="log">The SoundCharts log.</param>
    /// <param name="folders">The session, tile cache and log folders.</param>
    /// <param name="executable">The <c>s100</c> program, or <see langword="null"/> when none is installed; servers then fail to start with a clear reason.</param>
    /// <param name="launcher">Starts server processes.</param>
    /// <param name="healthProbe">Checks a server's health.</param>
    /// <param name="options">The supervision settings, or <see langword="null"/> for the defaults.</param>
    /// <param name="time">The clock, or <see langword="null"/> for the system clock.</param>
    public ChartServerHost(
        SoundChartsLog log,
        SoundChartsFolders folders,
        ServerExecutable? executable,
        IServerProcessLauncher launcher,
        IHealthProbe healthProbe,
        ChartServerOptions? options = null,
        TimeProvider? time = null)
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(folders);
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(healthProbe);

        Log = log;
        Folders = folders;
        Executable = executable;
        Launcher = launcher;
        HealthProbe = healthProbe;
        Options = options ?? new ChartServerOptions();
        Time = time ?? TimeProvider.System;
    }

    /// <summary>Gets the SoundCharts log.</summary>
    public SoundChartsLog Log { get; }

    /// <summary>Gets the session, tile cache and log folders.</summary>
    public SoundChartsFolders Folders { get; }

    /// <summary>Gets the <c>s100</c> program, or <see langword="null"/> when none is installed.</summary>
    public ServerExecutable? Executable { get; }

    /// <summary>Gets what starts server processes.</summary>
    public IServerProcessLauncher Launcher { get; }

    /// <summary>Gets what checks a server's health.</summary>
    public IHealthProbe HealthProbe { get; }

    /// <summary>Gets the supervision settings.</summary>
    public ChartServerOptions Options { get; }

    /// <summary>Gets the clock.</summary>
    public TimeProvider Time { get; }

    /// <summary>
    /// Gets the environment every server runs with. The server's web host
    /// doesn't watch its working folder for configuration changes (the
    /// step 0 crash was in that watcher), and its output has no colour codes.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ServerEnvironment { get; } = new Dictionary<string, string>
    {
        ["DOTNET_hostBuilder__reloadConfigOnChange"] = "false",
        ["NO_COLOR"] = "1",
    };
}
