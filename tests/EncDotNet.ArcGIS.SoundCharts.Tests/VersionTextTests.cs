using EncDotNet.S100;

namespace EncDotNet.SoundCharts.Tests;

public class VersionTextTests
{
    [Theory]
    [InlineData("0.27.0", "0.27.0")]
    [InlineData("0.27.0+", "0.27.0")]
    [InlineData("0.27.0+001df0e5bb6e9e5e25e686ef1edd89c8129d63c8", "0.27.0 (001df0e)")]
    [InlineData("0.0.0-dev+5e21966", "0.0.0-dev (5e21966)")]
    [InlineData("1.2.3+local.build", "1.2.3 (local.build)")]
    public void FormatInformational_ShortensCommitHashes(string informational, string expected)
    {
        Assert.Equal(expected, VersionText.FormatInformational(informational));
    }

    [Fact]
    public void FromAssembly_ReadsTheEncDotNetS100Version()
    {
        var version = VersionText.FromAssembly(typeof(S100Dataset).Assembly);

        Assert.NotEqual(VersionText.Unknown, version);
        Assert.Matches(@"^\d+\.\d+\.\d+", version);
    }
}
