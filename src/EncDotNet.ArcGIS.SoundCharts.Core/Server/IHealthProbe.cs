namespace EncDotNet.SoundCharts.Server;

/// <summary>
/// Asks a chart server whether it's answering. <see cref="HttpHealthProbe"/>
/// asks its <c>/healthz</c>; tests substitute fakes.
/// </summary>
public interface IHealthProbe
{
    /// <summary>Gets whether the server at <paramref name="endpoint"/> answers its health check.</summary>
    /// <param name="endpoint">The server's endpoint.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns><see langword="true"/> when it answered <c>200 OK</c>; never throws for a failed check.</returns>
    Task<bool> IsHealthyAsync(ServerEndpoint endpoint, CancellationToken cancellationToken);
}
