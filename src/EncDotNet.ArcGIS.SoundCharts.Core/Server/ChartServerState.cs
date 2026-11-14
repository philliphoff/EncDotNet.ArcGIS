namespace EncDotNet.SoundCharts.Server;

/// <summary>Where a chart server is in its life (design brief §2.2: the Server status).</summary>
public enum ChartServerState
{
    /// <summary>Not started, or stopped because nothing uses it.</summary>
    Stopped,

    /// <summary>Started, not yet answering.</summary>
    Starting,

    /// <summary>Answering at its <see cref="ChartServer.Endpoint"/>.</summary>
    Running,

    /// <summary>Exited or stopped answering, and being started again.</summary>
    Restarting,

    /// <summary>
    /// Stopped after failing: it couldn't start, or it failed more often than
    /// the restart budget allows. <see cref="ChartServer.RestartAsync"/> tries again.
    /// </summary>
    Failed,
}
