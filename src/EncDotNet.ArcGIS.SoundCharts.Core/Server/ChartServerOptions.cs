namespace EncDotNet.SoundCharts.Server;

/// <summary>How chart servers are supervised (design brief §4.2, D18).</summary>
public sealed record ChartServerOptions
{
    /// <summary>Gets the time between health checks of a running server. 10 seconds.</summary>
    public TimeSpan HealthInterval { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Gets the number of failed health checks in a row that restart a server. 3.</summary>
    public int FailedChecksBeforeRestart { get; init; } = 3;

    /// <summary>Gets the number of restarts allowed within <see cref="RestartWindow"/>. 3.</summary>
    public int MaxRestarts { get; init; } = 3;

    /// <summary>Gets the window <see cref="MaxRestarts"/> counts restarts in. 5 minutes.</summary>
    public TimeSpan RestartWindow { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets how long a starting server may take to answer its health check.
    /// The server opens and portrays its datasets before it listens, so a
    /// large set takes a while. 2 minutes.
    /// </summary>
    public TimeSpan StartupTimeout { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>Gets the time between health checks while a server starts. 250 ms.</summary>
    public TimeSpan StartupPollInterval { get; init; } = TimeSpan.FromMilliseconds(250);

    /// <summary>Gets the number of ports to try when another process takes the chosen one. 3.</summary>
    public int MaxPortAttempts { get; init; } = 3;

    /// <summary>Gets how long a stopping server may take to exit after it's ended. 5 seconds.</summary>
    public TimeSpan StopTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Gets the cap on the shared tile cache (<c>--cache-dir-mb</c>). 2048 MB.</summary>
    public int TileCacheMegabytes { get; init; } = 2048;

    /// <summary>Gets the number of a server's last output lines kept to explain a failure. 20.</summary>
    public int OutputLinesKept { get; init; } = 20;
}
