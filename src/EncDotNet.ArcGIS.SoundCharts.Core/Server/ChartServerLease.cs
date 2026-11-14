namespace EncDotNet.SoundCharts.Server;

/// <summary>
/// A hold on a pooled <see cref="ChartServer"/>: the server keeps running
/// while any lease on it is undisposed (<see cref="ChartServerPool"/>).
/// </summary>
public sealed class ChartServerLease : IAsyncDisposable
{
    private readonly ChartServerPool _pool;
    private int _disposed;

    internal ChartServerLease(ChartServerPool pool, ChartServer server)
    {
        _pool = pool;
        Server = server;
    }

    /// <summary>Gets the leased server.</summary>
    public ChartServer Server { get; }

    /// <summary>Gets whether the lease has been released.</summary>
    public bool IsReleased => Volatile.Read(ref _disposed) != 0;

    /// <summary>Releases the lease; the server stops if it was the last. Releasing twice does nothing.</summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            await _pool.ReleaseAsync(Server).ConfigureAwait(false);
        }
    }
}
