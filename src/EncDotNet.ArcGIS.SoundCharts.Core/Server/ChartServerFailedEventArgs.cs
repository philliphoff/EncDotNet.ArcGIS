namespace EncDotNet.SoundCharts.Server;

/// <summary>A chart server stopped after failing (<see cref="ChartServerState.Failed"/>).</summary>
/// <param name="server">The server.</param>
/// <param name="reason">Why, in a sentence for the notification and the log.</param>
public sealed class ChartServerFailedEventArgs(ChartServer server, string reason) : EventArgs
{
    /// <summary>Gets the server.</summary>
    public ChartServer Server { get; } = server;

    /// <summary>Gets why it stopped.</summary>
    public string Reason { get; } = reason;
}
