using EncDotNet.SoundCharts.Charts;
using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts.Server;

/// <summary>
/// The chart servers of a Pro session, one per <see cref="DatasetSet"/>,
/// shared by the layers that use the same set (design brief §4.1, D17).
/// </summary>
/// <remarks>
/// A layer holds a <see cref="ChartServerLease"/> on its set's server; the
/// server stops when the last lease is released. Changing a layer's
/// datasets is <see cref="ReplaceAsync"/>: the new server starts before the
/// old one stops, so the map shows the old chart until the new one answers
/// (D20).
/// </remarks>
public sealed class ChartServerPool : IAsyncDisposable
{
    private readonly Lock _gate = new();
    private readonly Dictionary<DatasetSet, Entry> _servers = [];

    /// <summary>Creates an empty pool.</summary>
    /// <param name="host">What the servers share.</param>
    public ChartServerPool(ChartServerHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        Host = host;
    }

    /// <summary>Raised when a server joins the pool, before it starts.</summary>
    public event EventHandler<ChartServer>? ServerAdded;

    /// <summary>Raised when a server leaves the pool, after it stops.</summary>
    public event EventHandler<ChartServer>? ServerRemoved;

    /// <summary>Raised when any server in the pool stops after failing; the add-in shows one notification.</summary>
    public event EventHandler<ChartServerFailedEventArgs>? ServerFailed;

    /// <summary>Gets what the servers share.</summary>
    public ChartServerHost Host { get; }

    /// <summary>Gets the servers in the pool.</summary>
    public IReadOnlyList<ChartServer> Servers
    {
        get
        {
            lock (_gate)
            {
                return [.. _servers.Values.Select(entry => entry.Server)];
            }
        }
    }

    /// <summary>Gets the server for <paramref name="datasets"/>, if one is in the pool.</summary>
    /// <param name="datasets">The dataset set.</param>
    public ChartServer? Find(DatasetSet datasets)
    {
        ArgumentNullException.ThrowIfNull(datasets);
        lock (_gate)
        {
            return _servers.TryGetValue(datasets, out var entry) ? entry.Server : null;
        }
    }

    /// <summary>
    /// Takes a lease on the server for <paramref name="datasets"/>, adding
    /// and starting it if the pool doesn't have it. Returns when the server
    /// answers or has failed: check the lease's <see cref="ChartServer.State"/>.
    /// </summary>
    /// <param name="datasets">The datasets to serve.</param>
    /// <param name="cancellationToken">Stops waiting; the lease is then released.</param>
    public async Task<ChartServerLease> AcquireAsync(DatasetSet datasets, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(datasets);

        ChartServer server;
        var added = false;
        lock (_gate)
        {
            if (!_servers.TryGetValue(datasets, out var entry))
            {
                entry = new Entry(new ChartServer(datasets, Host));
                entry.Server.Failed += OnServerFailed;
                _servers.Add(datasets, entry);
                added = true;
            }

            entry.Leases++;
            server = entry.Server;
        }

        var lease = new ChartServerLease(this, server);
        if (added)
        {
            Host.Log.Write(LogLevel.Info, server.LogSource, $"New chart: {Describe(datasets)}.");
            Raise(() => ServerAdded?.Invoke(this, server));
        }

        try
        {
            await server.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await lease.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        return lease;
    }

    /// <summary>
    /// Moves a lease to another dataset set: starts (or shares) the server
    /// for <paramref name="datasets"/>, then releases <paramref name="lease"/>.
    /// </summary>
    /// <param name="lease">The lease to give up.</param>
    /// <param name="datasets">The new dataset set.</param>
    /// <param name="cancellationToken">Stops waiting; <paramref name="lease"/> is then kept.</param>
    /// <returns>The lease on the new set's server.</returns>
    public async Task<ChartServerLease> ReplaceAsync(ChartServerLease lease, DatasetSet datasets, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lease);
        ArgumentNullException.ThrowIfNull(datasets);

        var next = await AcquireAsync(datasets, cancellationToken).ConfigureAwait(false);
        await lease.DisposeAsync().ConfigureAwait(false);
        return next;
    }

    /// <summary>Stops every server and empties the pool. Never throws.</summary>
    public async Task StopAllAsync()
    {
        foreach (var server in TakeAll())
        {
            await server.StopAsync().ConfigureAwait(false);
            Raise(() => ServerRemoved?.Invoke(this, server));
        }
    }

    /// <summary>
    /// Ends every server process at once and empties the pool, for Pro
    /// shutting down. Never throws or blocks.
    /// </summary>
    public void KillAll()
    {
        foreach (var server in TakeAll())
        {
            server.Kill();
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() => await StopAllAsync().ConfigureAwait(false);

    /// <summary>Releases one lease on <paramref name="server"/>, stopping it after the last.</summary>
    internal async Task ReleaseAsync(ChartServer server)
    {
        lock (_gate)
        {
            if (!_servers.TryGetValue(server.Datasets, out var entry) || !ReferenceEquals(entry.Server, server))
            {
                return;
            }

            if (--entry.Leases > 0)
            {
                return;
            }

            _servers.Remove(server.Datasets);
            server.Failed -= OnServerFailed;
        }

        await server.StopAsync().ConfigureAwait(false);
        Raise(() => ServerRemoved?.Invoke(this, server));
    }

    private List<ChartServer> TakeAll()
    {
        lock (_gate)
        {
            var servers = _servers.Values.Select(entry => entry.Server).ToList();
            foreach (var server in servers)
            {
                server.Failed -= OnServerFailed;
            }

            _servers.Clear();
            return servers;
        }
    }

    private void OnServerFailed(object? sender, ChartServerFailedEventArgs e) => Raise(() => ServerFailed?.Invoke(this, e));

    private void Raise(Action raise)
    {
        try
        {
            raise();
        }
        catch (Exception ex)
        {
            Host.Log.Error($"A chart server pool event handler failed: {ex}");
        }
    }

    private static string Describe(DatasetSet datasets) =>
        string.Join(", ", datasets.Sources.Select(source => source.Name));

    private sealed class Entry(ChartServer server)
    {
        public ChartServer Server { get; } = server;

        public int Leases { get; set; }
    }
}
