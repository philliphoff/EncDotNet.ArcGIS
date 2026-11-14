using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using EncDotNet.SoundCharts.Charts;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts.Tests.Server;

/// <summary>
/// Starts, restarts and stops the real bundled <c>s100 tiles serve</c>, as
/// the add-in does in Pro but without Pro. Skipped when the server hasn't
/// been built (<c>tools/build-server.ps1</c>).
/// </summary>
public class BundledServerTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task TheServer_StartsServesRestartsAndStops()
    {
        var executable = ServerExecutable.Locate(AppContext.BaseDirectory, RuntimeInformation.OSArchitecture, overridePath: null);
        Assert.SkipWhen(executable is null, "No bundled server; run tools/build-server.ps1 before building.");

        using var temp = new TempFolder();
        var dataset = Path.Combine(temp.Path, "synthetic-aton.gml");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "TestData", "synthetic-aton.gml"), dataset);
        var log = new SoundChartsLog(null);
        using var probe = new HttpHealthProbe();
        var host = new ChartServerHost(
            log,
            new SoundChartsFolders(Path.Combine(temp.Path, "SoundCharts"), Environment.ProcessId),
            executable,
            new ProcessServerLauncher(),
            probe,
            new ChartServerOptions { HealthInterval = TimeSpan.FromMilliseconds(200), StartupTimeout = TimeSpan.FromSeconds(60) });
        using var http = new HttpClient(new SocketsHttpHandler { UseProxy = false });
        await using var pool = new ChartServerPool(host);

        try
        {
            // Start.
            var lease = await pool.AcquireAsync(new DatasetSet([DatasetSource.Dataset(dataset)]), Token);
            var server = lease.Server;
            Assert.True(server.State == ChartServerState.Running, server.FailureReason);
            var first = Assert.IsType<ServerEndpoint>(server.Endpoint);
            var firstProcess = Assert.IsType<int>(server.ProcessId);

            using (var capabilities = await http.GetAsync(first.WmtsCapabilitiesUri, Token))
            {
                Assert.Equal(HttpStatusCode.OK, capabilities.StatusCode);
                Assert.Contains("<ows:Identifier>s100</ows:Identifier>", await capabilities.Content.ReadAsStringAsync(Token), StringComparison.Ordinal);
            }

            using (var wrongToken = await http.GetAsync(new ServerEndpoint(first.Port, "wrong").WmtsCapabilitiesUri, Token))
            {
                Assert.Equal(HttpStatusCode.NotFound, wrongToken.StatusCode);
            }

            // A crash restarts it with a new endpoint.
            Process.GetProcessById(firstProcess).Kill();
            await ServerTestContext.WaitUntilAsync(
                () => server.State == ChartServerState.Running && server.Endpoint is { } e && e != first,
                "the server restarted after a crash");
            var second = Assert.IsType<ServerEndpoint>(server.Endpoint);
            Assert.True(await probe.IsHealthyAsync(second, Token));
            Assert.False(await probe.IsHealthyAsync(first, Token));

            // Restart on request.
            Assert.True(await server.RestartAsync(Token));
            Assert.NotEqual(second, server.Endpoint);
            var lastProcess = Assert.IsType<int>(server.ProcessId);

            // Stop.
            await lease.DisposeAsync();
            Assert.Equal(ChartServerState.Stopped, server.State);
            Assert.True(HasExited(lastProcess));
            Assert.Contains(log.Snapshot(), entry => entry.Source == server.LogSource && entry.Message.StartsWith("Serving", StringComparison.Ordinal));
        }
        finally
        {
            pool.KillAll();
        }
    }

    private static bool HasExited(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return process.WaitForExit(5000);
        }
        catch (ArgumentException)
        {
            return true;
        }
    }
}
