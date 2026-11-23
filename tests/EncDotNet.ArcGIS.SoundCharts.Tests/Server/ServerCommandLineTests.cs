using System.Globalization;
using EncDotNet.SoundCharts.Charts;
using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts.Tests.Server;

public class ServerCommandLineTests
{
    private static readonly ServerEndpoint Endpoint = new(52448, "tok-en_1");

    [Fact]
    public void Build_ServesOneExchangeSet_OnLoopbackWithThePortAndToken()
    {
        var set = new DatasetSet([DatasetSource.ExchangeSet(@"C:\Charts\US_ENC")]);

        var arguments = ServerCommandLine.Build(set, Endpoint, @"C:\Cache\tile-cache", 2048);

        Assert.Equal(
            [
                "tiles", "serve", @"C:\Charts\US_ENC",
                "--host", "127.0.0.1",
                "--port", "52448",
                "--token", "tok-en_1",
                "--refresh", "0",
                "--no-viewer",
                "--cache-dir", @"C:\Cache\tile-cache",
                "--cache-dir-mb", "2048",
            ],
            arguments);
    }

    [Fact]
    public void Build_ServesSeveralDatasets_AsLayers()
    {
        var set = new DatasetSet([DatasetSource.Dataset(@"C:\Charts\b.000"), DatasetSource.Dataset(@"C:\Charts\a.h5")]);

        var arguments = ServerCommandLine.Build(set, Endpoint, tileCache: null, 1024);

        Assert.Equal(
            ["tiles", "serve", "--layer", @"C:\Charts\a.h5", "--layer", @"C:\Charts\b.000"],
            arguments.Take(6));
        Assert.DoesNotContain("--cache-dir", arguments);
    }

    [Fact]
    public void Build_RejectsAnExchangeSetMixedWithDatasets()
    {
        var set = new DatasetSet([DatasetSource.ExchangeSet(@"C:\Charts\US_ENC"), DatasetSource.Dataset(@"C:\Charts\a.000")]);

        Assert.Throws<NotSupportedException>(() => ServerCommandLine.Build(set, Endpoint, null, 1024));
    }

    [Fact]
    public void Build_AddsTheRenderOptions()
    {
        var set = new DatasetSet(
            [DatasetSource.Dataset(@"C:\Charts\a.000")],
            new ChartRenderOptions(SymbolScale: 1.5, ShowText: false));

        var arguments = ServerCommandLine.Build(set, Endpoint, null, 1024);

        Assert.Equal(["--symbol-scale", "1.5", "--no-text"], arguments.TakeLast(3));
    }

    [Fact]
    public void Build_WritesNumbersInvariantly()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var set = new DatasetSet([DatasetSource.Dataset(@"C:\Charts\a.000")], new ChartRenderOptions(TextScale: 0.75));

            var arguments = ServerCommandLine.Build(set, new ServerEndpoint(12345, "t"), null, 1024);

            Assert.Contains("0.75", arguments);
            Assert.Contains("12345", arguments);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }
}
