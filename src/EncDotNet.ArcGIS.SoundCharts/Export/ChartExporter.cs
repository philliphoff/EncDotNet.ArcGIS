using System.Globalization;
using System.IO;
using System.Net.Http;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.ChartLayers;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts.Export;

/// <summary>
/// <b>Export SoundCharts Chart...</b> (issue #12, D10): exports a layout
/// with each SoundCharts layer rendered at the layout's scale and
/// resolution instead of Pro's screen tiles.
/// </summary>
/// <remarks>
/// <para>
/// For every visible SoundCharts chart layer in a visible map frame, the
/// renderer draws the frame's extent at the export resolution into a Web
/// Mercator GeoTIFF. The GeoTIFF goes into the map as a raster layer just
/// above the chart layer, the chart layer is hidden, Pro exports the layout,
/// and then the raster layers go and the chart layers come back, whether
/// the export worked or not.
/// </para>
/// <para>
/// A layer that can't be rendered stays as it is, so the export still shows
/// it from the chart server's tiles; the outcome says which.
/// </para>
/// </remarks>
internal sealed class ChartExporter
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    private readonly IChartRasterRenderer _renderer;
    private readonly string _workFolder;

    /// <summary>Creates an exporter.</summary>
    /// <param name="renderer">Renders each layer.</param>
    /// <param name="workFolder">Where rendered images are kept until the export is done.</param>
    public ChartExporter(IChartRasterRenderer renderer, string workFolder)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentException.ThrowIfNullOrWhiteSpace(workFolder);
        _renderer = renderer;
        _workFolder = workFolder;
    }

    /// <summary>
    /// Creates the add-in's exporter: the bundled <c>s100 tiles export</c>,
    /// working in this Pro session's folder.
    /// </summary>
    public static ChartExporter CreateDefault()
    {
        var folders = SoundChartsSession.Folders;
        var renderer = new TilesExportRenderer(
            SoundChartsSession.ChartServers.Host.Executable,
            new ProcessServerLauncher(),
            SoundChartsSession.Log,
            Path.Combine(folders.Session, "export"));
        return new ChartExporter(renderer, Path.Combine(folders.Session, "export"));
    }

    /// <summary>
    /// Finds the SoundCharts layers a layout's export draws, and whether the
    /// layout already says "Not for navigation". Runs on the MCT.
    /// </summary>
    /// <param name="layout">The layout.</param>
    public static LayoutSurvey Survey(Layout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var elements = layout.GetElementsAsFlattenedList();
        var hasNotice = NotForNavigationNotice.IsPresent(
            elements.OfType<TextElement>().Select(e => e.TextProperties?.Text));

        var pageUnits = layout.GetPage()?.Units ?? LinearUnit.Inches;
        var layers = new List<ExportLayer>();
        MapFrame? largest = null;
        var largestArea = 0.0;
        foreach (var frame in elements.OfType<MapFrame>().Where(f => f.IsVisible && f.Map is not null))
        {
            var frameLayers = LayersOf(frame, pageUnits);
            layers.AddRange(frameLayers);
            var area = frame.GetWidth() * frame.GetHeight();
            if (frameLayers.Count > 0 && area > largestArea)
            {
                largest = frame;
                largestArea = area;
            }
        }

        var georeference = largest ?? elements.OfType<MapFrame>().FirstOrDefault();
        return new LayoutSurvey(layers, hasNotice, georeference?.Name);
    }

    /// <summary>
    /// Renders the surveyed layers and exports the layout with them swapped in.
    /// </summary>
    /// <param name="layout">The layout.</param>
    /// <param name="survey">What <see cref="Survey"/> found.</param>
    /// <param name="settings">The file, format and resolution.</param>
    /// <param name="cancellationToken">Cancels rendering; the export itself runs to the end.</param>
    public async Task<ExportOutcome> ExportAsync(
        Layout layout,
        LayoutSurvey survey,
        ChartExportSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(survey);
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        var folder = Path.Combine(_workFolder, Guid.NewGuid().ToString("N"));
        List<string> failures = [];
        try
        {
            var rendered = new List<(ExportLayer Layer, ChartRaster Raster)>();
            var index = 0;
            foreach (var layer in survey.Layers)
            {
                var request = new ChartRasterRequest(
                    layer.Name,
                    layer.Datasets,
                    layer.Palette,
                    layer.Frame,
                    settings.Dpi,
                    Path.Combine(folder, string.Create(CultureInfo.InvariantCulture, $"layer-{index++}.tif")))
                {
                    Time = layer.Time,
                    TimeStep = layer.Time is null ? null : ExportTime.NearestStep(await ReadTimeStepsAsync(layer, cancellationToken), layer.Time),
                };

                try
                {
                    rendered.Add((layer, await _renderer.RenderAsync(request, cancellationToken)));
                }
                catch (ChartRasterException ex)
                {
                    AddInLog.Write(LogLevel.Warning, $"{ex.Message} The export draws it from the chart server's tiles.");
                    failures.Add(ex.Message);
                }
            }

            var exported = await QueuedTask.Run(() => SwapAndExport(layout, rendered, survey.GeoreferenceFrameName, settings));
            return new ExportOutcome(exported, rendered.Count, failures);
        }
        finally
        {
            TryDelete(folder);
        }
    }

    /// <summary>Adds the rendered images, hides the chart layers, exports, and restores. Runs on the MCT.</summary>
    private static bool SwapAndExport(
        Layout layout,
        IReadOnlyList<(ExportLayer Layer, ChartRaster Raster)> rendered,
        string? georeferenceFrameName,
        ChartExportSettings settings)
    {
        var added = new List<RasterLayer>();
        var hidden = new List<Layer>();
        try
        {
            foreach (var (layer, raster) in rendered)
            {
                if (SwapIn(layer, raster) is { } rasterLayer)
                {
                    added.Add(rasterLayer);
                    layer.Layer.SetVisibility(false);
                    hidden.Add(layer.Layer);
                }
            }

            var format = ExportFormatFor(settings, georeferenceFrameName);
            AddInLog.Write(
                LogLevel.Info,
                $"Exporting the layout '{layout.Name}' to {settings.OutputPath} at {settings.Dpi} dpi with {added.Count} SoundCharts layer(s) rendered for it.");
            var started = DateTime.UtcNow.AddSeconds(-2);
            layout.Export(format);
            var written = new FileInfo(settings.OutputPath);
            return written.Exists && written.LastWriteTimeUtc >= started;
        }
        finally
        {
            foreach (var layer in hidden)
            {
                layer.SetVisibility(true);
            }

            foreach (var rasterLayer in added)
            {
                rasterLayer.Map?.RemoveLayer(rasterLayer);
            }
        }
    }

    /// <summary>Adds a rendered image just above its chart layer, drawn as it is. Runs on the MCT.</summary>
    private static RasterLayer? SwapIn(ExportLayer layer, ChartRaster raster)
    {
        if (layer.Layer.Parent is not ILayerContainerEdit container)
        {
            return null;
        }

        var position = container is ILayerContainer parent ? Math.Max(0, parent.Layers.ToList().IndexOf(layer.Layer)) : 0;
        var parameters = new RasterLayerCreationParams(new Uri(raster.Path))
        {
            Name = layer.Name,
            MapMemberIndex = position,
            IsVisible = true,
        };
        var rasterLayer = LayerFactory.Instance.CreateLayer<RasterLayer>(parameters, container);
        if (rasterLayer is null)
        {
            return null;
        }

        // The image is the chart's own colours with an alpha band: no
        // stretch and no gamma (Pro's default RGB colorizer has both, and
        // its gamma alone turns the day palette's blue to cyan), and
        // bilinear resampling where Pro scales it to the page.
        if (rasterLayer.GetColorizer() is CIMRasterRGBColorizer rgb)
        {
            rgb.StretchType = RasterStretchType.None;
            rgb.UseDefaultMapping = false;
            rgb.UseGammaStretch = false;
            rgb.GammaR = 1;
            rgb.GammaG = 1;
            rgb.GammaB = 1;
            rgb.UseAlphaBand = true;
            rgb.AlphaBandIndex = 3;
            rgb.ResamplingType = RasterResamplingType.BilinearInterpolation;
            if (rasterLayer.CanSetColorizer(rgb))
            {
                rasterLayer.SetColorizer(rgb);
            }
        }

        rasterLayer.SetTransparency(layer.Layer.Transparency);
        return rasterLayer;
    }

    private static ExportFormat ExportFormatFor(ChartExportSettings settings, string? georeferenceFrameName) => settings.Format switch
    {
        ChartExportFormat.Pdf => new PDFFormat
        {
            OutputFileName = settings.OutputPath,
            Resolution = settings.Dpi,
            HasGeoRefInfo = true,
        },
        ChartExportFormat.Png => new PNGFormat
        {
            OutputFileName = settings.OutputPath,
            Resolution = settings.Dpi,
            HasWorldFile = georeferenceFrameName is not null,
            GeoReferenceMapFrameName = georeferenceFrameName,
        },
        ChartExportFormat.Jpeg => new JPEGFormat
        {
            OutputFileName = settings.OutputPath,
            Resolution = settings.Dpi,
            HasWorldFile = georeferenceFrameName is not null,
            GeoReferenceMapFrameName = georeferenceFrameName,
        },
        ChartExportFormat.Tiff => new TIFFFormat
        {
            OutputFileName = settings.OutputPath,
            Resolution = settings.Dpi,
            HasGeoTiffTags = georeferenceFrameName is not null,
            GeoReferenceMapFrameName = georeferenceFrameName,
        },
        _ => throw new ArgumentOutOfRangeException(nameof(settings), settings.Format, "Unknown export format."),
    };

    /// <summary>The visible SoundCharts chart layers of a map frame. Runs on the MCT.</summary>
    private static List<ExportLayer> LayersOf(MapFrame frame, LinearUnit pageUnits)
    {
        var map = frame.Map;
        var scale = frame.Camera?.Scale ?? 0;
        if (map is null || frame.GetViewExtent() is not { IsEmpty: false } view)
        {
            return [];
        }

        var extent = (GeometryEngine.Instance.Project(view, SpatialReferences.WebMercator) as Envelope ?? view).Extent;
        var bounds = frame.GetBounds(false);
        var geometry = new ExportFrame(
            extent.XMin,
            extent.YMin,
            extent.XMax,
            extent.YMax,
            pageUnits.ConvertTo(bounds.Width, LinearUnit.Inches),
            pageUnits.ConvertTo(bounds.Height, LinearUnit.Inches));

        var layers = new List<ExportLayer>();
        foreach (var layer in map.GetLayersAsFlattenedList())
        {
            if (ChartLayerManager.ExportSource(layer) is not { } source
                || layer.GetDefinition() is not CIMTiledServiceLayer { ServiceConnection: CIMWMTSServiceConnection connection } cim
                || !IsDrawn(layer, scale)
                || IsHiddenByTime(cim))
            {
                continue;
            }

            layers.Add(new ExportLayer(
                layer,
                frame.Name,
                layer.Name,
                source.Marker,
                source.Datasets,
                string.IsNullOrWhiteSpace(connection.Style) ? ChartLayerDefinition.DefaultStyle : connection.Style,
                ReadTime(connection),
                geometry));
        }

        return layers;
    }

    /// <summary>Whether the layer and every group above it are visible at the frame's scale.</summary>
    private static bool IsDrawn(Layer layer, double scale)
    {
        for (var current = layer; current is not null; current = current.Parent as Layer)
        {
            if (!current.IsVisible
                || (scale > 0 && current.MinScale > 0 && scale > current.MinScale)
                || (scale > 0 && current.MaxScale > 0 && scale < current.MaxScale))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Whether map time hides the layer: A8 sets an empty visibility time extent when no step is near.</summary>
    private static bool IsHiddenByTime(CIMBaseLayer cim) =>
        cim.UseVisibilityTimeExtent && cim.VisibilityTimeExtent?.EndTime is { Year: < 1901 };

    private static DateTime? ReadTime(CIMWMTSServiceConnection connection) =>
        connection.Dimensions is { } dimensions && dimensions.TryGetValue("Time", out var value) ? ExportTime.TryParse(value) : null;

    /// <summary>Reads the layer's time steps from its chart server's TileJSON.</summary>
    private static async Task<IReadOnlyList<DateTime>> ReadTimeStepsAsync(ExportLayer layer, CancellationToken cancellationToken)
    {
        try
        {
            await using var lease = await SoundChartsSession.ChartServers.AcquireAsync(layer.Datasets, cancellationToken).ConfigureAwait(false);
            if (lease.Server.Endpoint is not { } endpoint)
            {
                return [];
            }

            var json = await Http.GetStringAsync(endpoint.TileJsonUri, cancellationToken).ConfigureAwait(false);
            return ExportTime.ReadTileJsonSteps(json);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or NotSupportedException)
        {
            AddInLog.Write(LogLevel.Warning, $"Couldn't read the time steps of {layer.Name}; the export draws its first step: {ex.Message}");
            return [];
        }
    }

    private static void TryDelete(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Pro may still hold an image open; the session folder goes when Pro closes.
        }
    }
}
