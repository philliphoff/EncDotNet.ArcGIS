namespace EncDotNet.SoundCharts.Server;

/// <summary>
/// Starts chart server processes. <see cref="ProcessServerLauncher"/> starts
/// real ones; tests substitute fakes, so the supervisor runs without
/// <c>s100</c> or Pro.
/// </summary>
public interface IServerProcessLauncher
{
    /// <summary>Starts a process.</summary>
    /// <param name="startInfo">What to run, and where.</param>
    /// <param name="output">
    /// Called with each line the process writes, on a background thread,
    /// from the moment it starts. It must not throw.
    /// </param>
    /// <returns>The running process.</returns>
    /// <exception cref="ServerStartException">The process couldn't be started.</exception>
    IServerProcess Start(ServerProcessStartInfo startInfo, Action<ServerOutputLine> output);
}
