using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace EncDotNet.SoundCharts.Server;

/// <summary>
/// Starts real chart server processes: hidden, with standard output and
/// error read on background threads, in the <see cref="KillOnCloseJob"/>.
/// </summary>
public sealed class ProcessServerLauncher : IServerProcessLauncher
{
    /// <summary>Gets whether a started process joined the kill-on-close job; for the log.</summary>
    public bool LastJoinedJob { get; private set; }

    /// <inheritdoc/>
    public IServerProcess Start(ServerProcessStartInfo startInfo, Action<ServerOutputLine> output)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        ArgumentNullException.ThrowIfNull(output);

        var info = new ProcessStartInfo(startInfo.FileName)
        {
            WorkingDirectory = startInfo.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var argument in startInfo.Arguments)
        {
            info.ArgumentList.Add(argument);
        }

        foreach (var (name, value) in startInfo.Environment)
        {
            info.Environment[name] = value;
        }

        var process = new Process { StartInfo = info, EnableRaisingEvents = true };
        var running = new RunningProcess(process);
        process.OutputDataReceived += (_, e) => Forward(e.Data, isError: false);
        process.ErrorDataReceived += (_, e) => Forward(e.Data, isError: true);
        process.Exited += (_, _) => running.OnExited();

        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            process.Dispose();
            throw new ServerStartException($"Couldn't start {startInfo.FileName}: {ex.Message}", ex);
        }

        LastJoinedJob = KillOnCloseJob.TryAssign(process);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return running;

        void Forward(string? line, bool isError)
        {
            if (line is null)
            {
                return;
            }

            try
            {
                output(new ServerOutputLine(line, isError));
            }
            catch (Exception)
            {
                // The callback must not throw; if it does, the line is lost,
                // not the reader thread.
            }
        }
    }

    private sealed class RunningProcess(Process process) : IServerProcess
    {
        private readonly TaskCompletionSource<int> _exited = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Id => SafeId(process);

        public Task<int> Exited => _exited.Task;

        public void Kill()
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
            {
                // Already gone.
            }
        }

        public void Dispose()
        {
            Kill();
            process.Dispose();
        }

        public void OnExited()
        {
            try
            {
                // Let the asynchronous readers drain, so the last lines (the
                // reason for an exit) are logged before the exit is reported.
                process.WaitForExit();
                _exited.TrySetResult(process.ExitCode);
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
            {
                _exited.TrySetResult(-1);
            }
        }

        private static int SafeId(Process process)
        {
            try
            {
                return process.Id;
            }
            catch (InvalidOperationException)
            {
                return 0;
            }
        }
    }
}
