using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts.Tests.Server;

public class ServerOutputTests
{
    [Theory]
    [InlineData("Serving 2 dataset(s) (S-124, S-125) (rendered on demand, png, zoom 2-8):", false, LogLevel.Info)]
    [InlineData("Error: 'x.000' does not exist.", false, LogLevel.Error)]
    [InlineData("fail: Microsoft.AspNetCore.Server.Kestrel[13]", false, LogLevel.Error)]
    [InlineData("crit: Microsoft.AspNetCore.Server.Kestrel[0]", false, LogLevel.Error)]
    [InlineData("Unhandled exception. System.ArgumentOutOfRangeException: Index was out of range.", true, LogLevel.Error)]
    [InlineData("   at Microsoft.Extensions.FileProviders.Physical.PhysicalFilesWatcher.OnRenamed", true, LogLevel.Error)]
    [InlineData("Skipped unsupported dataset (no known product specification): placeholder.dat", true, LogLevel.Warning)]
    [InlineData("warn: Microsoft.AspNetCore.Hosting", false, LogLevel.Warning)]
    [InlineData("dbug: Microsoft.AspNetCore.Routing", false, LogLevel.Debug)]
    public void Classify_ReadsTheLevel(string text, bool isError, LogLevel expected)
    {
        Assert.Equal(expected, ServerOutput.Classify(new ServerOutputLine(text, isError)));
    }

    [Fact]
    public void StripAnsi_RemovesColourCodes()
    {
        Assert.Equal("Error: bad", ServerOutput.StripAnsi("\u001b[31mError:\u001b[0m bad"));
    }

    [Theory]
    [InlineData("crit: Failed to bind to address http://127.0.0.1:50000: address already in use.", true)]
    [InlineData("System.Net.Sockets.SocketException (10048): Only one usage of each socket address", false)]
    [InlineData("Error: 'x.000' does not exist.", false)]
    public void IsAddressInUse_SpotsABindFailure(string text, bool expected)
    {
        Assert.Equal(expected, ServerOutput.IsAddressInUse(text));
    }
}
