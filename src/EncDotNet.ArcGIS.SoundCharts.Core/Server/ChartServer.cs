using System.Globalization;
using EncDotNet.SoundCharts.Charts;
using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts.Server;

/// <summary>
/// One supervised <c>s100 tiles serve</c> process for one
/// <see cref="DatasetSet"/> (design brief §4, D17–D22).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="StartAsync"/> starts the process on a free loopback port with a
/// new token, in its own long-path working folder, and waits until it
/// answers <c>/healthz</c>. While it runs, the server's health is checked
/// every <see cref="ChartServerOptions.HealthInterval"/>; a server that exits
/// or fails <see cref="ChartServerOptions.FailedChecksBeforeRestart"/> checks
/// in a row is started again, at most
/// <see cref="ChartServerOptions.MaxRestarts"/> times in
/// <see cref="ChartServerOptions.RestartWindow"/>. After that it's
/// <see cref="ChartServerState.Failed"/>, <see cref="Failed"/> is raised once,
/// and <see cref="RestartAsync"/> tries again.
/// </para>
/// <para>
/// Every start has a new endpoint, announced by <see cref="EndpointChanged"/>
/// so the layers it serves can be repointed (D20).
/// </para>
/// <para>
/// Nothing the process does throws into the caller (D22): failures become
/// log entries, <see cref="State"/>, <see cref="FailureReason"/> and
/// <see cref="Failed"/>. Events are raised on background threads, and an
/// exception from a handler is logged, not rethrown.
/// </para>
/// </remarks>
public sealed class ChartServer : IAsyncDisposable
{
    private readonly ChartServerHost _host;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Queue<DateTimeOffset> _restarts = new();
    private readonly Lock _outputGate = new();
    private readonly Queue<ServerOutputLine> _recentOutput = new();

    private CancellationTokenSource? _run;
    private IServerProcess? _process;
    private volatile ChartServerState _state;
    private volatile ServerEndpoint? _endpoint;
    private volatile string? _failureReason;
    private volatile string? _renderKey;
    private int _generation;
    private int _starts;

    /// <summary>Creates a stopped server for <paramref name="datasets"/>.</summary>
    /// <param name="datasets">What it serves.</param>
    /// <param name="host">What servers share.</param>
    public ChartServer(DatasetSet datasets, ChartServerHost host)
    {
        ArgumentNullException.ThrowIfNull(datasets);
        ArgumentNullException.ThrowIfNull(host);

        Datasets = datasets;
        _host = host;
    }

    /// <summary>Raised when <see cref="State"/> changes.</summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// Raised after every successful start, the first included, with the new
    /// endpoint. Layers served by this server are repointed to it.
    /// </summary>
    public event EventHandler<ServerEndpoint>? EndpointChanged;

    /// <summary>Raised once each time the server stops after failing.</summary>
    public event EventHandler<ChartServerFailedEventArgs>? Failed;

    /// <summary>Gets what the server serves.</summary>
    public DatasetSet Datasets { get; }

    /// <summary>Gets the server's id, the id of its <see cref="Datasets"/>.</summary>
    public string Id => Datasets.Id;

    /// <summary>Gets the <see cref="LogEntry.Source"/> of this server's log lines, <c>chart {id}</c>.</summary>
    public string LogSource => $"chart {Id}";

    /// <summary>Gets where the server is in its life.</summary>
    public ChartServerState State => _state;

    /// <summary>Gets where the server answers while it's running, otherwise <see langword="null"/>.</summary>
    public ServerEndpoint? Endpoint => _state == ChartServerState.Running ? _endpoint : null;

    /// <summary>Gets the id of the server process while it's running, otherwise <see langword="null"/>.</summary>
    public int? ProcessId => _state == ChartServerState.Running ? Volatile.Read(ref _process)?.Id : null;

    /// <summary>Gets why the server is <see cref="ChartServerState.Failed"/>, otherwise <see langword="null"/>.</summary>
    public string? FailureReason => _failureReason;

    /// <summary>
    /// Gets the render key of the last start (D27): the start-up options,
    /// the <c>s100</c> version and the data revision, hashed. It's the suffix
    /// of the endpoint's token, so every URL shows which settings and data
    /// drew it; <see langword="null"/> before the first start.
    /// </summary>
    public string? RenderKey => _renderKey;

    /// <summary>Gets the number of times the process has been started, restarts included.</summary>
    public int StartCount => Volatile.Read(ref _starts);

    /// <summary>
    /// Starts the server, unless it's running. Returns when it answers or
    /// has failed.
    /// </summary>
    /// <param name="cancellationToken">Stops waiting, and stops the server.</param>
    /// <returns>Whether the server is running; a <see cref="ChartServerState.Failed"/> server stays failed (use <see cref="RestartAsync"/>).</returns>
    public async Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            switch (_state)
            {
                case ChartServerState.Running:
                    return true;
                case ChartServerState.Failed:
                    return false;
            }

            _restarts.Clear();
            return await RunAsync(ChartServerState.Starting, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Stops the server and starts it again with a new endpoint, whatever its
    /// state: the <b>Restart</b> command. Resets the restart budget.
    /// </summary>
    /// <param name="cancellationToken">Stops waiting, and stops the server.</param>
    /// <returns>Whether the server is running.</returns>
    public async Task<bool> RestartAsync(CancellationToken cancellationToken = default)
    {
        CancelRun();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await StopProcessAsync().ConfigureAwait(false);
            _restarts.Clear();
            _host.Log.Write(LogLevel.Info, LogSource, "Restarting the chart server.");
            return await RunAsync(ChartServerState.Restarting, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Stops the server and deletes its working folder. Never throws.</summary>
    public async Task StopAsync()
    {
        CancelRun();
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var wasStopped = _state == ChartServerState.Stopped;
            await StopProcessAsync().ConfigureAwait(false);
            DisposeRun();
            _failureReason = null;
            SetState(ChartServerState.Stopped);
            SoundChartsFolders.TryDelete(Path.Combine(_host.Folders.Session, Id));
            if (!wasStopped)
            {
                _host.Log.Write(LogLevel.Info, LogSource, "Stopped the chart server.");
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Ends the process at once, without waiting: for Pro shutting down,
    /// where nothing may block. Never throws.
    /// </summary>
    public void Kill()
    {
        CancelRun();
        Interlocked.Increment(ref _generation);
        if (Interlocked.Exchange(ref _process, null) is { } process)
        {
            process.Kill();
            process.Dispose();
        }

        _state = ChartServerState.Stopped;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

    /// <inheritdoc/>
    public override string ToString() => $"{LogSource} ({_state})";

    /// <summary>Starts a process for a new run. The gate is held and no process runs.</summary>
    private async Task<bool> RunAsync(ChartServerState transitional, CancellationToken cancellationToken)
    {
        DisposeRun();
        var run = new CancellationTokenSource();
        Volatile.Write(ref _run, run);
        _failureReason = null;
        SetState(transitional);

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, run.Token);
        var reason = await LaunchAsync(linked.Token).ConfigureAwait(false);
        if (reason is null)
        {
            return true;
        }

        if (linked.IsCancellationRequested)
        {
            SetState(ChartServerState.Stopped);
            return false;
        }

        Fail(reason);
        return false;
    }

    /// <summary>
    /// Starts the process and waits until it answers, trying other ports if
    /// one is taken. The gate is held.
    /// </summary>
    /// <returns><see langword="null"/> once running; otherwise why it couldn't start.</returns>
    private async Task<string?> LaunchAsync(CancellationToken cancellationToken)
    {
        if (_host.Executable is not { } executable)
        {
            return "the chart server program (s100) isn't installed with the add-in";
        }

        string workingDirectory;
        try
        {
            workingDirectory = _host.Folders.CreateServerWorkingDirectory(Id);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return $"its working folder couldn't be created: {ex.Message}";
        }

        // Read at each start, so a restart after the files changed gets a new key.
        var renderKey = Charts.RenderKey.Compute(Datasets, executable.Version);
        _renderKey = renderKey;
        _host.Log.Write(LogLevel.Debug, LogSource, $"Render key {renderKey}.");

        for (var attempt = 1; attempt <= _host.Options.MaxPortAttempts; attempt++)
        {
            ServerEndpoint endpoint;
            IReadOnlyList<string> arguments;
            try
            {
                endpoint = new ServerEndpoint(LoopbackPort.FindFree(), ServerToken.Create(renderKey));
                arguments = ServerCommandLine.Build(Datasets, endpoint, _host.Folders.TileCache, _host.Options.TileCacheMegabytes);
            }
            catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException or System.Net.Sockets.SocketException)
            {
                return ex.Message;
            }

            ClearRecentOutput();
            _host.Log.Write(
                LogLevel.Info,
                LogSource,
                string.Create(CultureInfo.InvariantCulture, $"Starting the chart server on port {endpoint.Port}: {DescribeDatasets()}."));
            _host.Log.Write(LogLevel.Debug, LogSource, $"{executable.Path} {string.Join(' ', arguments.Select(Quote))}");

            var generation = Interlocked.Increment(ref _generation);
            IServerProcess process;
            try
            {
                process = _host.Launcher.Start(
                    new ServerProcessStartInfo(executable.Path, arguments, workingDirectory, ChartServerHost.ServerEnvironment),
                    line => OnOutput(line, generation));
            }
            catch (ServerStartException ex)
            {
                return ex.Message;
            }

            Interlocked.Increment(ref _starts);
            var outcome = await WaitUntilReadyAsync(process, endpoint, cancellationToken).ConfigureAwait(false);
            switch (outcome)
            {
                case StartOutcome.Ready:
                    _process = process;
                    _endpoint = endpoint;
                    SetState(ChartServerState.Running);
                    _host.Log.Write(
                        LogLevel.Info,
                        LogSource,
                        string.Create(CultureInfo.InvariantCulture, $"The chart server is running (process {process.Id}) at {endpoint.WmtsCapabilitiesUri.AbsoluteUri}"));
                    Raise(() => EndpointChanged?.Invoke(this, endpoint));
                    var run = Volatile.Read(ref _run);
                    _ = MonitorAsync(process, endpoint, generation, run?.Token ?? CancellationToken.None);
                    return null;

                case StartOutcome.Cancelled:
                    await EndAsync(process).ConfigureAwait(false);
                    return "the start was cancelled";

                case StartOutcome.TimedOut:
                    await EndAsync(process).ConfigureAwait(false);
                    return string.Create(
                        CultureInfo.InvariantCulture,
                        $"it didn't answer within {_host.Options.StartupTimeout.TotalSeconds:0} seconds");

                default:
                    var code = await process.Exited.ConfigureAwait(false);
                    process.Dispose();
                    if (RecentOutput().Any(line => ServerOutput.IsAddressInUse(line.Text)) && attempt < _host.Options.MaxPortAttempts)
                    {
                        _host.Log.Write(
                            LogLevel.Warning,
                            LogSource,
                            string.Create(CultureInfo.InvariantCulture, $"Port {endpoint.Port} was taken; trying another."));
                        continue;
                    }

                    return ExitReason(code);
            }
        }

        return "no free port could be bound";
    }

    private async Task<StartOutcome> WaitUntilReadyAsync(IServerProcess process, ServerEndpoint endpoint, CancellationToken cancellationToken)
    {
        var started = _host.Time.GetTimestamp();
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return StartOutcome.Cancelled;
            }

            if (process.Exited.IsCompleted)
            {
                return StartOutcome.Exited;
            }

            if (await _host.HealthProbe.IsHealthyAsync(endpoint, cancellationToken).ConfigureAwait(false))
            {
                // It answered, unless it exited meanwhile and something else did.
                return process.Exited.IsCompleted ? StartOutcome.Exited : StartOutcome.Ready;
            }

            if (_host.Time.GetElapsedTime(started) >= _host.Options.StartupTimeout)
            {
                return StartOutcome.TimedOut;
            }

            await WaitAsync(Task.Delay(_host.Options.StartupPollInterval, _host.Time, cancellationToken), process.Exited).ConfigureAwait(false);
        }
    }

    /// <summary>Checks a running process's health until it fails, exits or the run ends.</summary>
    private async Task MonitorAsync(IServerProcess process, ServerEndpoint endpoint, int generation, CancellationToken cancellationToken)
    {
        try
        {
            var failures = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                await WaitAsync(Task.Delay(_host.Options.HealthInterval, _host.Time, cancellationToken), process.Exited).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested || Volatile.Read(ref _generation) != generation)
                {
                    return;
                }

                if (process.Exited.IsCompleted)
                {
                    var code = await process.Exited.ConfigureAwait(false);
                    await RecoverAsync(generation, ExitReason(code)).ConfigureAwait(false);
                    return;
                }

                if (await _host.HealthProbe.IsHealthyAsync(endpoint, cancellationToken).ConfigureAwait(false))
                {
                    failures = 0;
                    continue;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                failures++;
                _host.Log.Write(
                    LogLevel.Warning,
                    LogSource,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"The chart server didn't answer its health check ({failures} of {_host.Options.FailedChecksBeforeRestart})."));
                if (failures >= _host.Options.FailedChecksBeforeRestart)
                {
                    await RecoverAsync(generation, "it stopped answering").ConfigureAwait(false);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            // The boundary with the caller: a supervision failure is a log
            // entry, never an exception in Pro.
            _host.Log.Write(LogLevel.Error, LogSource, $"Supervising the chart server failed: {ex}");
        }
    }

    /// <summary>Restarts a server whose process exited or stopped answering, within the budget.</summary>
    private async Task RecoverAsync(int generation, string reason)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var run = Volatile.Read(ref _run);
            if (Volatile.Read(ref _generation) != generation || _state != ChartServerState.Running || run is null || run.IsCancellationRequested)
            {
                return;
            }

            await StopProcessAsync().ConfigureAwait(false);
            while (true)
            {
                var now = _host.Time.GetUtcNow();
                while (_restarts.Count > 0 && now - _restarts.Peek() >= _host.Options.RestartWindow)
                {
                    _restarts.Dequeue();
                }

                if (_restarts.Count >= _host.Options.MaxRestarts)
                {
                    Fail(string.Create(
                        CultureInfo.InvariantCulture,
                        $"{reason}, after {_restarts.Count} restarts in {_host.Options.RestartWindow.TotalMinutes:0} minutes"));
                    return;
                }

                _restarts.Enqueue(now);
                _host.Log.Write(LogLevel.Warning, LogSource, $"The chart server stopped: {reason}. Restarting it.");
                SetState(ChartServerState.Restarting);
                var failure = await LaunchAsync(run.Token).ConfigureAwait(false);
                if (failure is null)
                {
                    return;
                }

                if (run.IsCancellationRequested)
                {
                    SetState(ChartServerState.Stopped);
                    return;
                }

                reason = failure;
            }
        }
        catch (Exception ex)
        {
            _host.Log.Write(LogLevel.Error, LogSource, $"Restarting the chart server failed: {ex}");
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Marks the server failed and says so once. The gate is held.</summary>
    private void Fail(string reason)
    {
        _failureReason = reason;
        _endpoint = null;
        SetState(ChartServerState.Failed);
        _host.Log.Write(LogLevel.Error, LogSource, $"The chart server stopped: {reason}.");
        var args = new ChartServerFailedEventArgs(this, reason);
        Raise(() => Failed?.Invoke(this, args));
    }

    /// <summary>Ends the current process, if any, and waits briefly for it to exit. The gate is held.</summary>
    private async Task StopProcessAsync()
    {
        Interlocked.Increment(ref _generation);
        _endpoint = null;
        if (Interlocked.Exchange(ref _process, null) is { } process)
        {
            await EndAsync(process).ConfigureAwait(false);
        }
    }

    private async Task EndAsync(IServerProcess process)
    {
        process.Kill();
        await WaitAsync(Task.Delay(_host.Options.StopTimeout, _host.Time), process.Exited).ConfigureAwait(false);
        process.Dispose();
    }

    private void CancelRun()
    {
        try
        {
            Volatile.Read(ref _run)?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already over.
        }
    }

    private void DisposeRun() => Interlocked.Exchange(ref _run, null)?.Dispose();

    private void SetState(ChartServerState state)
    {
        if (_state == state)
        {
            return;
        }

        _state = state;
        Raise(() => StateChanged?.Invoke(this, EventArgs.Empty));
    }

    private void Raise(Action raise)
    {
        try
        {
            raise();
        }
        catch (Exception ex)
        {
            _host.Log.Write(LogLevel.Error, LogSource, $"A chart server event handler failed: {ex}");
        }
    }

    private void OnOutput(ServerOutputLine line, int generation)
    {
        var text = ServerOutput.StripAnsi(line.Text).TrimEnd();
        if (text.Length == 0)
        {
            return;
        }

        var clean = line with { Text = text };
        lock (_outputGate)
        {
            if (generation == Volatile.Read(ref _generation))
            {
                _recentOutput.Enqueue(clean);
                while (_recentOutput.Count > _host.Options.OutputLinesKept)
                {
                    _recentOutput.Dequeue();
                }
            }
        }

        _host.Log.Write(ServerOutput.Classify(clean), LogSource, text);
    }

    private void ClearRecentOutput()
    {
        lock (_outputGate)
        {
            _recentOutput.Clear();
        }
    }

    private List<ServerOutputLine> RecentOutput()
    {
        lock (_outputGate)
        {
            return [.. _recentOutput];
        }
    }

    /// <summary>Explains an exit with the server's last error line, if it wrote one.</summary>
    private string ExitReason(int exitCode)
    {
        var recent = RecentOutput();
        var detail = recent.LastOrDefault(line => ServerOutput.Classify(line) == LogLevel.Error && !line.Text.StartsWith(' ') && !line.Text.StartsWith('\t'))
            ?? recent.LastOrDefault(line => line.IsError);
        var reason = string.Create(CultureInfo.InvariantCulture, $"s100 exited with code {exitCode}");
        return detail is null ? reason : $"{reason}: {detail.Text}";
    }

    private string DescribeDatasets() => Datasets.Sources.Count == 1
        ? Datasets.Sources[0].Path
        : string.Create(CultureInfo.InvariantCulture, $"{Datasets.Sources.Count} sources");

    private static string Quote(string argument) =>
        argument.Length == 0 || argument.Any(char.IsWhiteSpace) ? $"\"{argument}\"" : argument;

    private static async Task WaitAsync(Task delay, Task exited)
    {
        try
        {
            await Task.WhenAny(delay, exited).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Task.WhenAny doesn't throw; the caller checks its token.
        }
    }

    private enum StartOutcome
    {
        Ready,
        Exited,
        TimedOut,
        Cancelled,
    }
}
