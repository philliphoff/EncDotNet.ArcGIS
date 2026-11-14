using EncDotNet.SoundCharts.Charts;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts.Tests.Server;

/// <summary>
/// A chart server host on fakes, with short intervals so supervision runs in
/// milliseconds, and folders in a temporary folder.
/// </summary>
internal sealed class ServerTestContext : IDisposable
{
    public ServerTestContext(ChartServerOptions? options = null, bool withExecutable = true)
    {
        Folders = new SoundChartsFolders(Temp.Path, 4242);
        Log = new SoundChartsLog(null);
        Launcher = new FakeServerProcessLauncher();
        Probe = new FakeHealthProbe(Launcher);
        Host = new ChartServerHost(
            Log,
            Folders,
            withExecutable ? new ServerExecutable(System.IO.Path.Combine(Temp.Path, "s100.exe"), "win-x64", IsOverride: false) : null,
            Launcher,
            Probe,
            options ?? FastOptions);
    }

    public static ChartServerOptions FastOptions { get; } = new()
    {
        HealthInterval = TimeSpan.FromMilliseconds(30),
        StartupPollInterval = TimeSpan.FromMilliseconds(5),
        StartupTimeout = TimeSpan.FromSeconds(5),
        StopTimeout = TimeSpan.FromSeconds(1),
    };

    public TempFolder Temp { get; } = new();

    public SoundChartsFolders Folders { get; }

    public SoundChartsLog Log { get; }

    public FakeServerProcessLauncher Launcher { get; }

    public FakeHealthProbe Probe { get; }

    public ChartServerHost Host { get; }

    /// <summary>Creates a dataset set of empty files with these names.</summary>
    public DatasetSet Datasets(params string[] names) =>
        new(names.Select(name => DatasetSource.Dataset(Temp.File(System.IO.Path.Combine("data", name)))));

    public ChartServer Server(params string[] names) => new(Datasets(names.Length == 0 ? ["101AA00DS0001.000"] : names), Host);

    /// <summary>Waits until <paramref name="condition"/> holds, failing the test after 10 seconds.</summary>
    public static async Task WaitUntilAsync(Func<bool> condition, string what)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                Assert.Fail($"Timed out waiting until {what}.");
            }

            await Task.Delay(5, TestContext.Current.CancellationToken);
        }
    }

    public void Dispose() => Temp.Dispose();
}
