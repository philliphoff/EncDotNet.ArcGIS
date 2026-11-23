using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts.Tests.Server;

public class ServerEndpointTests
{
    private static readonly ServerEndpoint Endpoint = new(52448, "Zm9vYmFyYmF6cXV4cXV1eA-3f9a1c02");

    [Fact]
    public void TheUrls_CarryThePortAndToken()
    {
        Assert.Equal("http://127.0.0.1:52448/Zm9vYmFyYmF6cXV4cXV1eA-3f9a1c02/", Endpoint.BaseUri.AbsoluteUri);
        Assert.Equal(
            "http://127.0.0.1:52448/Zm9vYmFyYmF6cXV4cXV1eA-3f9a1c02/wmts/1.0.0/WMTSCapabilities.xml",
            Endpoint.WmtsCapabilitiesUri.AbsoluteUri);
        Assert.Equal("http://127.0.0.1:52448/Zm9vYmFyYmF6cXV4cXV1eA-3f9a1c02/wms", Endpoint.WmsUri.AbsoluteUri);
        Assert.Equal("http://127.0.0.1:52448/Zm9vYmFyYmF6cXV4cXV1eA-3f9a1c02/tiles.json", Endpoint.TileJsonUri.AbsoluteUri);
    }

    [Fact]
    public void TheHealthCheck_HasNoToken()
    {
        Assert.Equal("http://127.0.0.1:52448/healthz", Endpoint.HealthUri.AbsoluteUri);
    }

    [Fact]
    public void TheWmtsTileTemplate_NamesTheStyle()
    {
        Assert.Equal(
            "http://127.0.0.1:52448/Zm9vYmFyYmF6cXV4cXV1eA-3f9a1c02/wmts/1.0.0/s100/night/WebMercatorQuad/{TileMatrix}/{TileRow}/{TileCol}.png",
            Endpoint.WmtsTileTemplate("night"));
    }

    [Theory]
    [InlineData(0, "token")]
    [InlineData(65536, "token")]
    public void APortOutOfRange_IsRejected(int port, string token)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ServerEndpoint(port, token));
    }

    [Theory]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("a/b")]
    [InlineData("a+b")]
    public void ATokenTheServerRejects_IsRejected(string token)
    {
        Assert.Throws<ArgumentException>(() => new ServerEndpoint(8200, token));
    }

    [Fact]
    public void EndpointsWithTheSamePortAndToken_AreEqual()
    {
        Assert.Equal(new ServerEndpoint(8200, "abc"), new ServerEndpoint(8200, "abc"));
        Assert.NotEqual(new ServerEndpoint(8200, "abc"), new ServerEndpoint(8200, "abd"));
    }
}
