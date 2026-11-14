using System.Globalization;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts.Export;

/// <summary>
/// Renders a layer with the bundled <c>s100 tiles export</c>: the chart
/// server's own tile renderer, at the zoom the frame shows on screen, with
/// 512-pixel tiles above 96 dpi, written as a Web Mercator GeoTIFF.
/// </summary>
/// <remarks>
/// This is the best the pinned <c>s100</c> offers: WMS GetMap has no DPI
/// parameter (U8), and <c>s100 render</c> neither culls by scale in its
/// composite form nor scales lines and patterns with <c>--symbol-scale</c>.
/// Tiles keep the screen's content and every symbol's size on paper, at up
/// to 192 dpi of detail; Pro resamples them for higher resolutions.
/// </remarks>
public sealed class TilesExportRenderer : IChartRasterRenderer
{
    /// <summary>The log source of export output.</summary>
    public const string LogSource = "export";

    /// <summary>The highest pixel ratio <c>tiles export</c> offers: <c>--tile-size 512</c>.</summary>
    public const int MaxPixelRatio = 2;

    private readonly ServerExecutable? _executable;
    private readonly IServerProcessLauncher _launcher;
    private readonly SoundChartsLog _log;
    private readonly string _workingDirectory;

    /// <summary>Creates the renderer.</summary>
    /// <param name="executable">The <c>s100</c> to run, or <see langword="null"/> when none is installed.</param>
    /// <param name="launcher">Starts the process.</param>
    /// <param name="log">Receives the process's output.</param>
    /// <param name="workingDirectory">A long-path folder to run in (D19).</param>
    public TilesExportRenderer(ServerExecutable? executable, IServerProcessLauncher launcher, SoundChartsLog log, string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(log);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
        _executable = executable;
        _launcher = launcher;
        _log = log;
        _workingDirectory = workingDirectory;
    }

    /// <inheritdoc/>
    public string Name => "s100 tiles export";

    /// <inheritdoc/>
    public async Task<ChartRaster> RenderAsync(ChartRasterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_executable is null)
        {
            throw new ChartRasterException("The SoundCharts chart server (s100) isn't installed, so the chart can't be rendered.");
        }

        ChartTilePlan plan;
        IReadOnlyList<string> arguments;
        try
        {
            plan = ChartTilePlan.For(request.Frame, request.Dpi, MaxPixelRatio);
            arguments = TilesExportCommandLine.Build(request, plan);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException)
        {
            throw new ChartRasterException($"{request.Name} can't be rendered: {ex.Message}", ex);
        }

        var description = string.Create(
            CultureInfo.InvariantCulture,
            $"zoom {plan.Zoom}, {ChartTilePlan.TileSize * plan.PixelRatio}-pixel tiles, {plan.TileCount} tile(s), {plan.RenderedDpi:0} dpi on the page");
        _log.Write(LogLevel.Info, LogSource, $"Rendering {request.Name} for a {request.Dpi} dpi export: {description}.");

        Directory.CreateDirectory(_workingDirectory);
        var outputFolder = Path.GetDirectoryName(request.OutputPath);
        if (!string.IsNullOrEmpty(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        var lastError = string.Empty;
        var startInfo = new ServerProcessStartInfo(_executable.Path, arguments, _workingDirectory, ExportEnvironment);
        IServerProcess process;
        try
        {
            process = _launcher.Start(startInfo, line =>
            {
                if (line.IsError && !string.IsNullOrWhiteSpace(line.Text))
                {
                    lastError = line.Text.Trim();
                }

                _log.Write(LogLevel.Debug, LogSource, line.Text);
            });
        }
        catch (ServerStartException ex)
        {
            throw new ChartRasterException(ex.Message, ex);
        }

        using (process)
        {
            int exitCode;
            try
            {
                exitCode = await process.Exited.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                process.Kill();
                throw;
            }

            if (exitCode != 0 || !File.Exists(request.OutputPath))
            {
                var reason = string.IsNullOrEmpty(lastError) ? string.Create(CultureInfo.InvariantCulture, $"exit code {exitCode}") : lastError;
                throw new ChartRasterException($"{request.Name} couldn't be rendered: {reason}.");
            }
        }

        return new ChartRaster(request.OutputPath, description);
    }

    /// <summary>
    /// Gets the environment of an export: the server's, and no update check
    /// (it would delay every export).
    /// </summary>
    public static IReadOnlyDictionary<string, string> ExportEnvironment { get; } =
        new Dictionary<string, string>(ChartServerHost.ServerEnvironment) { ["S100_NO_UPDATE_CHECK"] = "1" };
}
