using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts.Tests.Server;

/// <summary>
/// Answers health checks for <see cref="FakeServerProcess"/>es: healthy when
/// the process serving the endpoint runs and is <see cref="FakeServerProcess.Healthy"/>.
/// </summary>
internal sealed class FakeHealthProbe(FakeServerProcessLauncher launcher) : IHealthProbe
{
    private int _checks;

    public int Checks => Volatile.Read(ref _checks);

    public Task<bool> IsHealthyAsync(ServerEndpoint endpoint, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _checks);
        var process = launcher.Started.LastOrDefault(p => p.Endpoint == endpoint);
        return Task.FromResult(process is { IsRunning: true, Healthy: true });
    }
}
