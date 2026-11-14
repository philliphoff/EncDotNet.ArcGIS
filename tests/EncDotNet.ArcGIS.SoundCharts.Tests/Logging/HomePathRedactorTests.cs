using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts.Tests.Logging;

public class HomePathRedactorTests
{
    private const string Home = @"C:\Users\alice";

    [Theory]
    [InlineData(@"Serving C:\Users\alice\Charts\a.000", @"Serving ~\Charts\a.000")]
    [InlineData(@"Serving c:\users\ALICE\Charts\a.000", @"Serving ~\Charts\a.000")]
    [InlineData("Serving C:/Users/alice/Charts/a.000", "Serving ~/Charts/a.000")]
    [InlineData(@"{""path"":""C:\\Users\\alice\\a.000""}", @"{""path"":""~\\a.000""}")]
    [InlineData(@"C:\Users\alice", "~")]
    [InlineData(@"C:\Users\alicea\a.000", @"C:\Users\alicea\a.000")]
    [InlineData("no paths here", "no paths here")]
    public void Redact_ReplacesTheHomeFolder(string text, string expected)
    {
        Assert.Equal(expected, HomePathRedactor.Redact(text, Home));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(@"C:\")]
    public void Redact_WithoutAUsableHome_LeavesTheText(string? home)
    {
        Assert.Equal(@"C:\Users\alice\a.000", HomePathRedactor.Redact(@"C:\Users\alice\a.000", home));
    }
}
