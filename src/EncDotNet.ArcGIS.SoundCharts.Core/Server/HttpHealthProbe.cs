namespace EncDotNet.SoundCharts.Server;

/// <summary>
/// Asks a chart server's <c>/healthz</c>, which answers <c>200 OK</c> with
/// <c>ok</c> while the server runs.
/// </summary>
public sealed class HttpHealthProbe : IHealthProbe, IDisposable
{
    private readonly HttpClient _client;
    private readonly TimeSpan _timeout;

    /// <summary>Creates a probe.</summary>
    /// <param name="timeout">How long one check may take; 5 seconds when <see langword="null"/>.</param>
    public HttpHealthProbe(TimeSpan? timeout = null)
    {
        _timeout = timeout ?? TimeSpan.FromSeconds(5);

        // Loopback only: never through a system proxy.
        _client = new HttpClient(new SocketsHttpHandler { UseProxy = false, PooledConnectionLifetime = TimeSpan.FromMinutes(1) })
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };
    }

    /// <inheritdoc/>
    public async Task<bool> IsHealthyAsync(ServerEndpoint endpoint, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);
        try
        {
            using var response = await _client.GetAsync(endpoint.HealthUri, timeout.Token).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public void Dispose() => _client.Dispose();
}
