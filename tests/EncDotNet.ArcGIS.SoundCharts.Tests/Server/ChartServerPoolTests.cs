using EncDotNet.SoundCharts.Charts;
using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts.Tests.Server;

public class ChartServerPoolTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Acquire_TheSameSet_SharesOneServer()
    {
        using var context = new ServerTestContext();
        await using var pool = new ChartServerPool(context.Host);
        var a = context.Datasets("101AA00DS0001.000", "102AA00DS0001.h5");
        var b = new DatasetSet(a.Sources.Reverse());

        await using var first = await pool.AcquireAsync(a, Token);
        await using var second = await pool.AcquireAsync(b, Token);

        Assert.Same(first.Server, second.Server);
        Assert.Single(pool.Servers);
        Assert.Single(context.Launcher.Started);
    }

    [Fact]
    public async Task Acquire_AnotherSet_StartsAnotherServer()
    {
        using var context = new ServerTestContext();
        await using var pool = new ChartServerPool(context.Host);

        await using var chart = await pool.AcquireAsync(context.Datasets("101AA00DS0001.000"), Token);
        await using var waterLevel = await pool.AcquireAsync(context.Datasets("104AA00DS0001.h5"), Token);

        Assert.NotSame(chart.Server, waterLevel.Server);
        Assert.Equal(2, pool.Servers.Count);
        Assert.NotEqual(chart.Server.Endpoint?.Port, waterLevel.Server.Endpoint?.Port);
    }

    [Fact]
    public async Task Release_StopsTheServerAfterTheLastLease()
    {
        using var context = new ServerTestContext();
        await using var pool = new ChartServerPool(context.Host);
        var set = context.Datasets("101AA00DS0001.000");
        var first = await pool.AcquireAsync(set, Token);
        var second = await pool.AcquireAsync(set, Token);

        await first.DisposeAsync();
        Assert.Equal(ChartServerState.Running, second.Server.State);

        await second.DisposeAsync();
        await second.DisposeAsync();
        Assert.Equal(ChartServerState.Stopped, second.Server.State);
        Assert.Empty(pool.Servers);
        Assert.True(context.Launcher.Last.WasKilled);
    }

    [Fact]
    public async Task Replace_StartsTheNewServerBeforeStoppingTheOld()
    {
        using var context = new ServerTestContext();
        await using var pool = new ChartServerPool(context.Host);
        var lease = await pool.AcquireAsync(context.Datasets("101AA00DS0001.000"), Token);
        var old = context.Launcher.Last;
        var oldRanWhileNewStarted = false;
        context.Launcher.OnNextStart(_ => oldRanWhileNewStarted = old.IsRunning);

        var next = await pool.ReplaceAsync(lease, context.Datasets("101AA00DS0001.000", "101AA00DS0002.000"), Token);

        Assert.True(oldRanWhileNewStarted);
        Assert.True(old.WasKilled);
        Assert.True(lease.IsReleased);
        Assert.Equal(ChartServerState.Running, next.Server.State);
        Assert.Equal(2, next.Server.Datasets.Sources.Count);
        Assert.Single(pool.Servers);
        await next.DisposeAsync();
    }

    [Fact]
    public async Task AFailedServer_IsReportedByThePool()
    {
        using var context = new ServerTestContext();
        await using var pool = new ChartServerPool(context.Host);
        var reasons = new List<string>();
        pool.ServerFailed += (_, e) => reasons.Add(e.Reason);
        context.Launcher.OnNextStart(process =>
        {
            process.Write("Error: The datasets have no geometry to tile.");
            process.Exit(2);
        });

        await using var lease = await pool.AcquireAsync(context.Datasets("101AA00DS0001.000"), Token);

        Assert.Equal(ChartServerState.Failed, lease.Server.State);
        Assert.Equal(["s100 exited with code 2: Error: The datasets have no geometry to tile."], reasons);
    }

    [Fact]
    public async Task StopAll_StopsEveryServer()
    {
        using var context = new ServerTestContext();
        var pool = new ChartServerPool(context.Host);
        await pool.AcquireAsync(context.Datasets("101AA00DS0001.000"), Token);
        await pool.AcquireAsync(context.Datasets("104AA00DS0001.h5"), Token);

        await pool.StopAllAsync();

        Assert.Empty(pool.Servers);
        Assert.All(context.Launcher.Started, process => Assert.True(process.WasKilled));
    }

    [Fact]
    public async Task KillAll_EndsEveryProcess()
    {
        using var context = new ServerTestContext();
        var pool = new ChartServerPool(context.Host);
        await pool.AcquireAsync(context.Datasets("101AA00DS0001.000"), Token);
        await pool.AcquireAsync(context.Datasets("104AA00DS0001.h5"), Token);

        pool.KillAll();

        Assert.Empty(pool.Servers);
        Assert.All(context.Launcher.Started, process => Assert.True(process.WasKilled));
    }
}
