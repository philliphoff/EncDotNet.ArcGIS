namespace EncDotNet.SoundCharts.Server;

/// <summary>A running chart server process.</summary>
public interface IServerProcess : IDisposable
{
    /// <summary>Gets the process id.</summary>
    int Id { get; }

    /// <summary>Gets a task that completes with the exit code when the process exits.</summary>
    Task<int> Exited { get; }

    /// <summary>Ends the process, if it's still running. Never throws.</summary>
    void Kill();
}
