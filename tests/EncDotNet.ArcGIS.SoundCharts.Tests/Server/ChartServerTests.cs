using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts.Tests.Server;

public class ChartServerTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Start_RunsTheServer_AndAnnouncesItsEndpoint()
    {
        using var context = new ServerTestContext();
        await using var server = context.Server();
        var endpoints = new List<ServerEndpoint>();
        server.EndpointChanged += (_, endpoint) => endpoints.Add(endpoint);

        Assert.True(await server.StartAsync(Token));

        Assert.Equal(ChartServerState.Running, server.State);
        Assert.NotNull(server.Endpoint);
        Assert.Equal([server.Endpoint], endpoints);
        Assert.Equal(context.Launcher.Last.Id, server.ProcessId);
        Assert.Single(context.Launcher.Started);
    }

    [Fact]
    public async Task Start_PassesThePortAndTokenOfTheEndpointToTheServer()
    {
        using var context = new ServerTestContext();
        await using var server = context.Server();

        await server.StartAsync(Token);

        var process = context.Launcher.Last;
        var endpoint = Assert.IsType<ServerEndpoint>(server.Endpoint);
        Assert.Equal(endpoint, process.Endpoint);
        Assert.Equal("127.0.0.1", process.Argument("--host"));
        Assert.Equal("0", process.Argument("--refresh"));
        Assert.Contains("--no-viewer", process.StartInfo.Arguments);
        Assert.Equal(context.Folders.TileCache, process.Argument("--cache-dir"));
        Assert.Equal(["tiles", "serve", server.Datasets.Sources[0].Path], process.StartInfo.Arguments.Take(3));
        Assert.Contains($":{endpoint.Port}/{endpoint.Token}/", endpoint.WmtsCapabilitiesUri.AbsoluteUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Start_RunsTheServerInItsOwnLongPathFolder()
    {
        using var context = new ServerTestContext();
        await using var server = context.Server();

        await server.StartAsync(Token);

        var startInfo = context.Launcher.Last.StartInfo;
        Assert.Equal(Path.Combine(context.Folders.Session, server.Id), startInfo.WorkingDirectory);
        Assert.True(Directory.Exists(startInfo.WorkingDirectory));
        Assert.False(LongPath.LooksShort(startInfo.WorkingDirectory));
        Assert.Equal("false", startInfo.Environment["DOTNET_hostBuilder__reloadConfigOnChange"]);
    }

    [Fact]
    public async Task Start_WhenRunning_DoesNothing()
    {
        using var context = new ServerTestContext();
        await using var server = context.Server();

        await server.StartAsync(Token);
        await server.StartAsync(Token);

        Assert.Single(context.Launcher.Started);
    }

    [Fact]
    public async Task Stop_EndsTheProcess_AndDeletesItsFolder()
    {
        using var context = new ServerTestContext();
        var server = context.Server();
        await server.StartAsync(Token);
        var process = context.Launcher.Last;

        await server.StopAsync();

        Assert.Equal(ChartServerState.Stopped, server.State);
        Assert.Null(server.Endpoint);
        Assert.True(process.WasKilled);
        Assert.False(Directory.Exists(process.StartInfo.WorkingDirectory));
    }

    [Fact]
    public async Task Restart_StartsANewProcessWithANewEndpoint()
    {
        using var context = new ServerTestContext();
        await using var server = context.Server();
        var endpoints = new List<ServerEndpoint>();
        server.EndpointChanged += (_, endpoint) => endpoints.Add(endpoint);
        await server.StartAsync(Token);
        var first = context.Launcher.Last;

        Assert.True(await server.RestartAsync(Token));

        Assert.True(first.WasKilled);
        Assert.Equal(2, context.Launcher.Started.Count);
        Assert.Equal(2, endpoints.Count);
        Assert.NotEqual(endpoints[0].Token, endpoints[1].Token);
        Assert.Equal(endpoints[1], server.Endpoint);
    }

    [Fact]
    public async Task AnExit_RestartsTheServer_AndRepointsIt()
    {
        using var context = new ServerTestContext();
        await using var server = context.Server();
        var endpoints = new List<ServerEndpoint>();
        server.EndpointChanged += (_, endpoint) => endpoints.Add(endpoint);
        await server.StartAsync(Token);

        context.Launcher.Last.Write("Unhandled exception. System.ArgumentOutOfRangeException", isError: true);
        context.Launcher.Last.Exit(-532462766);

        await ServerTestContext.WaitUntilAsync(() => endpoints.Count == 2 && server.State == ChartServerState.Running, "the server restarted");
        Assert.Equal(2, context.Launcher.Started.Count);
        Assert.Equal(endpoints[1], server.Endpoint);
        Assert.Contains(context.Log.Snapshot(), entry =>
            entry.Level == LogLevel.Warning && entry.Message.Contains("ArgumentOutOfRangeException", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FailedHealthChecks_RestartTheServer()
    {
        using var context = new ServerTestContext();
        await using var server = context.Server();
        await server.StartAsync(Token);
        var first = context.Launcher.Last;

        first.Healthy = false;

        await ServerTestContext.WaitUntilAsync(() => context.Launcher.Started.Count == 2 && server.State == ChartServerState.Running, "the server restarted");
        Assert.True(first.WasKilled);
        Assert.Equal(3, context.Log.Snapshot().Count(entry => entry.Message.Contains("didn't answer its health check", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task TooManyFailures_StopTheServer_AndSaySoOnce()
    {
        using var context = new ServerTestContext();
        await using var server = context.Server();
        var failures = new List<ChartServerFailedEventArgs>();
        server.Failed += (_, e) => failures.Add(e);
        await server.StartAsync(Token);

        for (var crash = 1; crash <= 4; crash++)
        {
            var process = context.Launcher.Last;
            process.Write("Error: out of memory");
            process.Exit(1);
            if (crash < 4)
            {
                await ServerTestContext.WaitUntilAsync(() => context.Launcher.Started.Count == crash + 1 && server.State == ChartServerState.Running, $"restart {crash}");
            }
        }

        await ServerTestContext.WaitUntilAsync(() => server.State == ChartServerState.Failed, "the server failed");
        Assert.Equal(4, context.Launcher.Started.Count);
        var failure = Assert.Single(failures);
        Assert.Same(server, failure.Server);
        Assert.Contains("Error: out of memory", failure.Reason, StringComparison.Ordinal);
        Assert.Contains("after 3 restarts", failure.Reason, StringComparison.Ordinal);
        Assert.Equal(failure.Reason, server.FailureReason);
        Assert.Null(server.Endpoint);
    }

    [Fact]
    public async Task Restart_AfterFailing_TriesAgain()
    {
        using var context = new ServerTestContext();
        await using var server = context.Server();
        context.Launcher.OnNextStart(process => process.Exit(2));
        Assert.False(await server.StartAsync(Token));

        Assert.False(await server.StartAsync(Token));
        Assert.True(await server.RestartAsync(Token));

        Assert.Equal(ChartServerState.Running, server.State);
        Assert.Null(server.FailureReason);
    }

    [Fact]
    public async Task AStartFailure_GivesTheServersError()
    {
        using var context = new ServerTestContext();
        await using var server = context.Server();
        var failures = 0;
        server.Failed += (_, _) => failures++;
        context.Launcher.OnNextStart(process =>
        {
            process.Write("Portraying 1 dataset(s) (S-101).");
            process.Write("Error: 'C:\\charts\\missing.000' does not exist.");
            process.Exit(2);
        });

        Assert.False(await server.StartAsync(Token));

        Assert.Equal(ChartServerState.Failed, server.State);
        Assert.Equal("s100 exited with code 2: Error: 'C:\\charts\\missing.000' does not exist.", server.FailureReason);
        Assert.Equal(1, failures);
        Assert.Single(context.Launcher.Started);
    }

    [Fact]
    public async Task ATakenPort_IsRetriedOnAnotherPort()
    {
        using var context = new ServerTestContext();
        await using var server = context.Server();
        context.Launcher.OnNextStart(process =>
        {
            process.Write("crit: Failed to bind to address http://127.0.0.1:50000: address already in use.", isError: true);
            process.Exit(1);
        });

        Assert.True(await server.StartAsync(Token));

        Assert.Equal(2, context.Launcher.Started.Count);
        Assert.Equal(context.Launcher.Last.Endpoint, server.Endpoint);
    }

    [Fact]
    public async Task AServerThatNeverAnswers_FailsAfterTheStartupTimeout()
    {
        using var context = new ServerTestContext(ServerTestContext.FastOptions with { StartupTimeout = TimeSpan.FromMilliseconds(100) });
        await using var server = context.Server();
        context.Launcher.OnNextStart(process => process.Healthy = false);

        Assert.False(await server.StartAsync(Token));

        Assert.True(context.Launcher.Last.WasKilled);
        Assert.Contains("didn't answer", server.FailureReason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WithoutAServerProgram_FailsWithAReason()
    {
        using var context = new ServerTestContext(withExecutable: false);
        await using var server = context.Server();

        Assert.False(await server.StartAsync(Token));

        Assert.Equal(ChartServerState.Failed, server.State);
        Assert.Contains("isn't installed", server.FailureReason, StringComparison.Ordinal);
        Assert.Empty(context.Launcher.Started);
    }

    [Fact]
    public async Task AProcessThatCantStart_FailsWithAReason()
    {
        using var context = new ServerTestContext();
        await using var server = context.Server();
        context.Launcher.FailNextStart = new ServerStartException("Couldn't start s100.exe: access denied");

        Assert.False(await server.StartAsync(Token));

        Assert.Equal("Couldn't start s100.exe: access denied", server.FailureReason);
    }

    [Fact]
    public async Task AFailingEventHandler_DoesntReachTheCaller()
    {
        using var context = new ServerTestContext();
        await using var server = context.Server();
        server.StateChanged += (_, _) => throw new InvalidOperationException("handler bug");
        server.EndpointChanged += (_, _) => throw new InvalidOperationException("handler bug");

        Assert.True(await server.StartAsync(Token));

        Assert.Contains(context.Log.Snapshot(), entry => entry.Level == LogLevel.Error && entry.Message.Contains("handler bug", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ServerOutput_IsLoggedUnderTheChart()
    {
        using var context = new ServerTestContext();
        await using var server = context.Server();
        await server.StartAsync(Token);

        context.Launcher.Last.Write("Serving 1 dataset(s) (S-101) (rendered on demand, png, zoom 12-13):");
        context.Launcher.Last.Write("Skipped unsupported dataset: placeholder.dat", isError: true);

        var entries = context.Log.Snapshot().Where(entry => entry.Source == server.LogSource).ToList();
        Assert.Contains(entries, entry => entry.Level == LogLevel.Info && entry.Message.StartsWith("Serving 1 dataset", StringComparison.Ordinal));
        Assert.Contains(entries, entry => entry.Level == LogLevel.Warning && entry.Message.StartsWith("Skipped", StringComparison.Ordinal));
        Assert.Equal($"chart {server.Id}", server.LogSource);
    }

    [Fact]
    public async Task Kill_EndsTheProcessAtOnce()
    {
        using var context = new ServerTestContext();
        await using var server = context.Server();
        await server.StartAsync(Token);

        server.Kill();

        Assert.True(context.Launcher.Last.WasKilled);
        Assert.Equal(ChartServerState.Stopped, server.State);
        await Task.Delay(100, Token);
        Assert.Single(context.Launcher.Started);
    }
}
