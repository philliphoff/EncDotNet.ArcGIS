using System.Collections.Concurrent;
using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts.Tests.Server;

/// <summary>
/// Starts <see cref="FakeServerProcess"/>es instead of <c>s100</c>, and
/// records what it was asked to run.
/// </summary>
internal sealed class FakeServerProcessLauncher : IServerProcessLauncher
{
    private readonly ConcurrentQueue<Action<FakeServerProcess>> _behaviours = new();
    private int _nextId = 1000;

    /// <summary>Gets every process started, oldest first.</summary>
    public ConcurrentQueue<FakeServerProcess> Started { get; } = new();

    /// <summary>Gets or sets an exception to throw from the next <see cref="Start"/>.</summary>
    public ServerStartException? FailNextStart { get; set; }

    /// <summary>Queues what the next started process does as soon as it starts.</summary>
    public void OnNextStart(Action<FakeServerProcess> behaviour) => _behaviours.Enqueue(behaviour);

    /// <summary>Gets the last started process.</summary>
    public FakeServerProcess Last => Started.Last();

    public IServerProcess Start(ServerProcessStartInfo startInfo, Action<ServerOutputLine> output)
    {
        if (FailNextStart is { } failure)
        {
            FailNextStart = null;
            throw failure;
        }

        var process = new FakeServerProcess(Interlocked.Increment(ref _nextId), startInfo, output);
        Started.Enqueue(process);
        if (_behaviours.TryDequeue(out var behaviour))
        {
            behaviour(process);
        }

        return process;
    }
}

/// <summary>A pretend chart server process that a test drives.</summary>
internal sealed class FakeServerProcess(int id, ServerProcessStartInfo startInfo, Action<ServerOutputLine> output) : IServerProcess
{
    private readonly TaskCompletionSource<int> _exited = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int Id { get; } = id;

    public ServerProcessStartInfo StartInfo { get; } = startInfo;

    public Task<int> Exited => _exited.Task;

    public bool WasKilled { get; private set; }

    public bool IsRunning => !_exited.Task.IsCompleted;

    /// <summary>Gets or sets whether the health probe finds this process healthy while it runs.</summary>
    public bool Healthy { get; set; } = true;

    /// <summary>Gets the port and token the process was told to serve on.</summary>
    public ServerEndpoint Endpoint => new(
        int.Parse(Argument("--port"), System.Globalization.CultureInfo.InvariantCulture),
        Argument("--token"));

    public string Argument(string name)
    {
        var arguments = StartInfo.Arguments;
        for (var i = 0; i < arguments.Count - 1; i++)
        {
            if (arguments[i] == name)
            {
                return arguments[i + 1];
            }
        }

        throw new InvalidOperationException($"No {name} argument.");
    }

    public void Write(string line, bool isError = false) => output(new ServerOutputLine(line, isError));

    public void Exit(int code) => _exited.TrySetResult(code);

    public void Kill()
    {
        WasKilled = true;
        _exited.TrySetResult(-1);
    }

    public void Dispose()
    {
    }
}
