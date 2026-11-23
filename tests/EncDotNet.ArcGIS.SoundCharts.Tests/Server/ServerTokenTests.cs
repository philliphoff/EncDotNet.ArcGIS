using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts.Tests.Server;

public class ServerTokenTests
{
    [Fact]
    public void Create_Gives128RandomBitsInBase64Url()
    {
        var token = ServerToken.Create();

        Assert.Equal(22, token.Length);
        Assert.True(ServerToken.IsValid(token));
    }

    [Fact]
    public void Create_GivesANewTokenEachTime()
    {
        var tokens = Enumerable.Range(0, 100).Select(_ => ServerToken.Create()).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(100, tokens.Count);
    }

    [Fact]
    public void Create_AppendsTheSuffix()
    {
        var token = ServerToken.Create("3f9a1c02e47b5d18");

        Assert.Matches("^[A-Za-z0-9_-]{22}-3f9a1c02e47b5d18$", token);
    }

    [Fact]
    public void Create_RejectsASuffixTheServerWouldRefuse()
    {
        Assert.Throws<ArgumentException>(() => ServerToken.Create("a/b"));
    }

    [Theory]
    [InlineData("abc-DEF_123", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("a b", false)]
    [InlineData("é", false)]
    public void IsValid_FollowsTheServersRule(string? token, bool expected)
    {
        Assert.Equal(expected, ServerToken.IsValid(token));
    }
}
