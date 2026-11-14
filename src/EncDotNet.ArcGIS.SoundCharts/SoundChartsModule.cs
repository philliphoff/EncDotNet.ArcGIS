using System.Diagnostics;
using ArcGIS.Desktop.Framework.Contracts;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The SoundCharts add-in module (<c>SoundCharts_Module</c> in Config.daml).
/// Pro creates it when it starts, because the module is <c>autoLoad</c>.
/// </summary>
/// <remarks>
/// It logs the versions, tidies session folders a crashed Pro left, turns
/// chart server failures into Pro notifications, and starts the chart layer
/// manager, which starts a chart server for each SoundCharts layer of an open
/// project. All servers stop when Pro exits; the kill-on-close job stops them
/// even if Pro crashes.
/// </remarks>
internal sealed class SoundChartsModule : Module
{
    protected override bool Initialize()
    {
        try
        {
            AddInLog.Write($"SoundCharts loaded: {AboutInfo.Collect().ToSingleLine()}");
            LogServer();

            var deleted = SoundChartsSession.Folders.DeleteStaleSessions(IsRunning);
            if (deleted > 0)
            {
                AddInLog.Write(LogLevel.Debug, $"Deleted {deleted} session folder(s) left by an earlier Pro.");
            }

            SoundChartsSession.ChartServers.ServerFailed += ChartServerNotification.OnServerFailed;
            ChartLayerManager.Instance.Start();
#if DEBUG
            FeatureLayers.FeaturesSelfCheck.RunIfRequested();
#endif
        }
        catch (Exception ex)
        {
            // The boundary with Pro: a failure here must not stop the module loading.
            AddInLog.Write(LogLevel.Error, $"SoundCharts failed to initialize: {ex}");
        }

        return base.Initialize();
    }

    protected override void Uninitialize()
    {
        try
        {
            if (SoundChartsSession.HasChartServers)
            {
                SoundChartsSession.ChartServers.ServerFailed -= ChartServerNotification.OnServerFailed;
                SoundChartsSession.ChartServers.KillAll();
            }

            SoundChartsSession.Folders.DeleteSession();
        }
        catch (Exception ex)
        {
            AddInLog.Write(LogLevel.Error, $"Stopping the chart servers failed: {ex}");
        }

        base.Uninitialize();
    }

    protected override bool CanUnload() => true;

    private static void LogServer()
    {
        var executable = SoundChartsSession.ChartServers.Host.Executable;
        if (executable is null)
        {
            AddInLog.Write(
                LogLevel.Warning,
                $"No chart server (s100) found in {SoundChartsSession.AddInFolder}\\{ServerExecutable.BundleFolder}; SoundCharts layers can't draw.");
            return;
        }

        AddInLog.Write(
            $"Chart server: s100 {executable.Version ?? VersionText.Unknown} ({executable.RuntimeIdentifier ?? "override"}) at {executable.Path}");
    }

    private static bool IsRunning(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
