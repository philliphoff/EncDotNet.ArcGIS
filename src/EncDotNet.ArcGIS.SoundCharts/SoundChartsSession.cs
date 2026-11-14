using System.IO;
using EncDotNet.SoundCharts.ChartLayers;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Server;
using EncDotNet.SoundCharts.Settings;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The add-in's shared state for this Pro process: its folders, the
/// SoundCharts log and the chart server pool. Created on first use, so the
/// log works before the module has loaded.
/// </summary>
internal static class SoundChartsSession
{
    private static readonly Lazy<SoundChartsFolders> LazyFolders = new(CreateFolders);
    private static readonly Lazy<SoundChartsLog> LazyLog = new(() => new SoundChartsLog(LazyFolders.Value.Logs));
    private static readonly Lazy<ChartServerPool> LazyChartServers = new(CreateChartServers);
    private static readonly Lazy<ApplicationSettingsFile> LazySettingsFile = new(() => new ApplicationSettingsFile(LazyFolders.Value.Root));
    private static readonly Lazy<ChartSettings> LazySettings = new(CreateSettings);

    /// <summary>Gets the folder the add-in runs from, which holds the bundled server.</summary>
    public static string AddInFolder { get; } =
        Path.GetDirectoryName(typeof(SoundChartsSession).Assembly.Location) ?? AppContext.BaseDirectory;

    /// <summary>Gets the session, tile cache and log folders.</summary>
    public static SoundChartsFolders Folders => LazyFolders.Value;

    /// <summary>Gets the SoundCharts log.</summary>
    public static SoundChartsLog Log => LazyLog.Value;

    /// <summary>
    /// Gets the chart servers. The chart layer manager takes a lease per
    /// SoundCharts layer and repoints the layer on <see cref="ChartServer.EndpointChanged"/>.
    /// </summary>
    public static ChartServerPool ChartServers => LazyChartServers.Value;

    /// <summary>
    /// Gets the SoundCharts layers of the open project, by their provenance
    /// markers (A3). Object information resolves datasets through it.
    /// </summary>
    public static ChartLayerRegistry ChartLayers { get; } = new();

    /// <summary>
    /// Gets the palette and mariner settings SoundCharts layers draw with:
    /// the open project's, seeded from the user's application settings (A5).
    /// </summary>
    public static ChartSettings Settings => LazySettings.Value;

    /// <summary>Gets the user's application settings file.</summary>
    public static ApplicationSettingsFile SettingsFile => LazySettingsFile.Value;

    /// <summary>Gets whether <see cref="ChartServers"/> has been created.</summary>
    public static bool HasChartServers => LazyChartServers.IsValueCreated;

    private static SoundChartsFolders CreateFolders()
    {
        try
        {
            return SoundChartsFolders.ForCurrentProcess();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // %LOCALAPPDATA% isn't writable; fall back to the temporary folder.
            return new SoundChartsFolders(Path.Combine(Path.GetTempPath(), "SoundCharts", "ArcGIS"), Environment.ProcessId);
        }
    }

    private static ChartServerPool CreateChartServers()
    {
        // The s100 path and cache size are read once: changing them on the
        // Options page takes effect when Pro restarts.
        var application = Settings.Application;
        var overridePath = Environment.GetEnvironmentVariable(ServerExecutable.OverrideVariable);
        var executable = ServerExecutable.Locate(
            AddInFolder,
            System.Runtime.InteropServices.RuntimeInformation.OSArchitecture,
            string.IsNullOrWhiteSpace(overridePath) ? application.ServerPath : overridePath);
        var options = new ChartServerOptions { TileCacheMegabytes = application.TileCacheMegabytes };
        var host = new ChartServerHost(Log, Folders, executable, new ProcessServerLauncher(), new HttpHealthProbe(), options);
        return new ChartServerPool(host);
    }

    private static ChartSettings CreateSettings()
    {
        var application = LazySettingsFile.Value.Load(out var problem);
        if (problem is not null)
        {
            Log.Write(LogLevel.Warning, SoundChartsLog.AddInSource, problem);
        }

        return new ChartSettings(application);
    }
}
