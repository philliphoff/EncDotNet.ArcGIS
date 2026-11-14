using System.Globalization;

namespace EncDotNet.SoundCharts.Server;

/// <summary>
/// Where one start of a chart server answers: the loopback port and the
/// token every URL but <see cref="HealthUri"/> starts with. A restart gets a
/// new endpoint (design brief §4.2), so layers are repointed to its URLs.
/// </summary>
public sealed record ServerEndpoint
{
    /// <summary>The address every chart server listens on (D19).</summary>
    public const string Host = "127.0.0.1";

    /// <summary>The WMTS layer identifier <c>s100 tiles serve</c> publishes.</summary>
    public const string WmtsLayer = "s100";

    /// <summary>The WMTS tile matrix set <c>s100 tiles serve</c> publishes.</summary>
    public const string WmtsTileMatrixSet = "WebMercatorQuad";

    /// <summary>Creates an endpoint.</summary>
    /// <param name="port">The loopback port, 1 to 65535.</param>
    /// <param name="token">The URL token (<see cref="ServerToken.IsValid"/>).</param>
    public ServerEndpoint(int port, string token)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(port, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 65535);
        if (!ServerToken.IsValid(token))
        {
            throw new ArgumentException($"'{token}' isn't a valid server token: use letters, digits, '-' and '_'.", nameof(token));
        }

        Port = port;
        Token = token;
    }

    /// <summary>Gets the loopback port.</summary>
    public int Port { get; }

    /// <summary>Gets the URL token.</summary>
    public string Token { get; }

    /// <summary>Gets the base of every served URL, <c>http://127.0.0.1:{port}/{token}/</c>.</summary>
    public Uri BaseUri => new(string.Create(CultureInfo.InvariantCulture, $"http://{Host}:{Port}/{Token}/"));

    /// <summary>Gets the health check, <c>http://127.0.0.1:{port}/healthz</c>, which has no token.</summary>
    public Uri HealthUri => new(string.Create(CultureInfo.InvariantCulture, $"http://{Host}:{Port}/healthz"));

    /// <summary>Gets the WMTS 1.0.0 capabilities document, the URL a Pro WMTS connection uses.</summary>
    public Uri WmtsCapabilitiesUri => new(BaseUri, "wmts/1.0.0/WMTSCapabilities.xml");

    /// <summary>Gets the WMS 1.3.0 endpoint.</summary>
    public Uri WmsUri => new(BaseUri, "wms");

    /// <summary>Gets the TileJSON document.</summary>
    public Uri TileJsonUri => new(BaseUri, "tiles.json");

    /// <summary>
    /// Gets the RESTful WMTS tile template for <paramref name="style"/> (a
    /// palette: <c>day</c>, <c>dusk</c> or <c>night</c>), with the OGC
    /// <c>{TileMatrix}</c>, <c>{TileRow}</c> and <c>{TileCol}</c> placeholders.
    /// </summary>
    /// <param name="style">The WMTS style.</param>
    public string WmtsTileTemplate(string style)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(style);
        return $"{BaseUri.AbsoluteUri}wmts/1.0.0/{WmtsLayer}/{Uri.EscapeDataString(style)}/{WmtsTileMatrixSet}/{{TileMatrix}}/{{TileRow}}/{{TileCol}}.png";
    }

    /// <inheritdoc/>
    public override string ToString() => BaseUri.AbsoluteUri;
}
