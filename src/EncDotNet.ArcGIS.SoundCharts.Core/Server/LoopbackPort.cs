using System.Net;
using System.Net.Sockets;

namespace EncDotNet.SoundCharts.Server;

/// <summary>Finds free loopback ports for chart servers.</summary>
public static class LoopbackPort
{
    /// <summary>
    /// Asks Windows for a free port on 127.0.0.1 and releases it at once.
    /// </summary>
    /// <remarks>
    /// Another process can take the port before the server binds it; the
    /// supervisor then starts again on another port (design brief §4.3). The
    /// add-in picks the port rather than passing <c>--port 0</c>, because the
    /// server prints its bound URLs only as wrapped console text (U4).
    /// </remarks>
    public static int FindFree()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}
