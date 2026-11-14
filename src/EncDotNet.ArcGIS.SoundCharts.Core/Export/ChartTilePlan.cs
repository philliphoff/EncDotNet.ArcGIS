using System.Globalization;

namespace EncDotNet.SoundCharts.Export;

/// <summary>
/// How a map frame is rendered from Web Mercator tiles: the zoom level the
/// frame's display shows, and how many image pixels each tile pixel gets for
/// the export's resolution.
/// </summary>
/// <remarks>
/// <para>
/// The zoom is the one Pro's WMTS layer draws for the frame on screen, at
/// 96 dpi: the WebMercatorQuad level whose resolution is nearest the frame's
/// resolution. Content gated by scale (SCAMIN) is decided per zoom level, so
/// the export shows what the screen shows at every resolution, not what a
/// WMS request of more pixels would (step 0: a 300 dpi WMS export moves
/// about one and a half zoom levels).
/// </para>
/// <para>
/// The pixel ratio renders the same zoom's tiles with every symbol, line and
/// pattern scaled together (<c>--tile-size 512</c> is a ratio of 2), so a
/// symbol keeps its size on paper at any resolution. Above the renderer's
/// highest ratio, Pro resamples the tiles to the export resolution.
/// </para>
/// </remarks>
/// <param name="Zoom">The WebMercatorQuad zoom level.</param>
/// <param name="PixelRatio">Image pixels per tile pixel: 1 for 256-pixel tiles, 2 for 512.</param>
/// <param name="West">The western edge, in degrees of longitude.</param>
/// <param name="South">The southern edge, in degrees of latitude.</param>
/// <param name="East">The eastern edge, in degrees of longitude.</param>
/// <param name="North">The northern edge, in degrees of latitude.</param>
/// <param name="TileCount">The number of tiles that cover the frame at <paramref name="Zoom"/>.</param>
/// <param name="RenderedDpi">The resolution the tiles have on the page, in dots per inch.</param>
public sealed record ChartTilePlan(
    int Zoom,
    int PixelRatio,
    double West,
    double South,
    double East,
    double North,
    long TileCount,
    double RenderedDpi)
{
    /// <summary>The Web Mercator sphere's radius, in metres (EPSG:3857).</summary>
    public const double EarthRadius = 6_378_137.0;

    /// <summary>The edge of a WebMercatorQuad tile, in tile pixels.</summary>
    public const int TileSize = 256;

    /// <summary>The highest zoom level the chart server renders.</summary>
    public const int MaxZoom = 24;

    /// <summary>The most tiles one export renders for a frame.</summary>
    public const long MaxTiles = 16_384;

    /// <summary>The latitude limit of Web Mercator, in degrees.</summary>
    public const double MaxLatitude = 85.05112878;

    /// <summary>Gets the Web Mercator metres a zoom level's tile pixel covers.</summary>
    /// <param name="zoom">The zoom level.</param>
    public static double MetresPerTilePixel(int zoom) => 2 * Math.PI * EarthRadius / TileSize / Math.Pow(2, zoom);

    /// <summary>
    /// Gets the zoom level a WMTS layer draws for a view of
    /// <paramref name="metresPerPixel"/> Web Mercator metres per screen pixel:
    /// the level of the nearest resolution, on a logarithmic scale.
    /// </summary>
    /// <param name="metresPerPixel">The view's resolution at 96 dpi.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="metresPerPixel"/> isn't positive.</exception>
    public static int ZoomFor(double metresPerPixel)
    {
        if (!double.IsFinite(metresPerPixel) || metresPerPixel <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(metresPerPixel), metresPerPixel, "The resolution must be positive.");
        }

        var zoom = Math.Log2(MetresPerTilePixel(0) / metresPerPixel);
        return Math.Clamp((int)Math.Round(zoom, MidpointRounding.AwayFromZero), 0, MaxZoom);
    }

    /// <summary>
    /// Plans the tiles for <paramref name="frame"/> exported at
    /// <paramref name="dpi"/>.
    /// </summary>
    /// <param name="frame">What the map frame shows.</param>
    /// <param name="dpi">The export resolution, in dots per inch.</param>
    /// <param name="maxPixelRatio">The highest pixel ratio the renderer offers.</param>
    /// <exception cref="ArgumentException">The frame has no area.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dpi"/> or <paramref name="maxPixelRatio"/> isn't positive.</exception>
    /// <exception cref="InvalidOperationException">The frame needs more than <see cref="MaxTiles"/> tiles.</exception>
    public static ChartTilePlan For(ExportFrame frame, int dpi, int maxPixelRatio = 2)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dpi);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxPixelRatio);
        frame.Validate();

        var zoom = ZoomFor(frame.MetresPerPixel(96));
        var ratio = Math.Clamp((int)Math.Ceiling(dpi / 96.0 - 0.01), 1, maxPixelRatio);

        var tileMetres = MetresPerTilePixel(zoom) * TileSize;
        var world = Math.PI * EarthRadius;
        var minX = Math.Max(frame.MinX, -world);
        var maxX = Math.Min(frame.MaxX, world);
        var minY = Math.Max(frame.MinY, -world);
        var maxY = Math.Min(frame.MaxY, world);
        var columns = (long)Math.Ceiling((maxX + world) / tileMetres) - (long)Math.Floor((minX + world) / tileMetres);
        var rows = (long)Math.Ceiling((world - minY) / tileMetres) - (long)Math.Floor((world - maxY) / tileMetres);
        var tiles = Math.Max(1, columns) * Math.Max(1, rows);
        if (tiles > MaxTiles)
        {
            throw new InvalidOperationException(string.Create(
                CultureInfo.InvariantCulture,
                $"The map frame needs {tiles} tiles at zoom {zoom}; the most an export renders is {MaxTiles}."));
        }

        var (west, south) = ToLonLat(minX, minY);
        var (east, north) = ToLonLat(maxX, maxY);
        var renderedDpi = 96 * ratio * frame.MetresPerPixel(96) / MetresPerTilePixel(zoom);
        return new ChartTilePlan(zoom, ratio, west, south, east, north, tiles, renderedDpi);
    }

    /// <summary>Gets the plan's bounds as <c>s100</c>'s <c>--bbox</c>: west,south,east,north.</summary>
    public string BoundingBox => string.Create(
        CultureInfo.InvariantCulture,
        $"{West:R},{South:R},{East:R},{North:R}");

    /// <summary>Converts Web Mercator metres to degrees of longitude and latitude.</summary>
    /// <param name="x">The easting.</param>
    /// <param name="y">The northing.</param>
    public static (double Longitude, double Latitude) ToLonLat(double x, double y)
    {
        var longitude = x / EarthRadius * 180 / Math.PI;
        var latitude = (2 * Math.Atan(Math.Exp(y / EarthRadius)) - Math.PI / 2) * 180 / Math.PI;
        return (longitude, Math.Clamp(latitude, -MaxLatitude, MaxLatitude));
    }

    /// <summary>Converts degrees of longitude and latitude to Web Mercator metres.</summary>
    /// <param name="longitude">The longitude.</param>
    /// <param name="latitude">The latitude.</param>
    public static (double X, double Y) FromLonLat(double longitude, double latitude)
    {
        var lat = Math.Clamp(latitude, -MaxLatitude, MaxLatitude) * Math.PI / 180;
        return (EarthRadius * longitude * Math.PI / 180, EarthRadius * Math.Log(Math.Tan(Math.PI / 4 + lat / 2)));
    }
}
