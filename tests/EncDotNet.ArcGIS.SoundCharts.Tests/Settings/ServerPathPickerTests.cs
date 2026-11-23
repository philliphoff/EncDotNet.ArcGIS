namespace EncDotNet.SoundCharts.Tests.Settings;

public class ServerPathPickerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void InitialDirectory_IsNone_WithoutAPath(string? current)
    {
        Assert.Null(ServerPathPicker.InitialDirectory(current));
    }

    [Fact]
    public void InitialDirectory_IsTheProgramsFolder()
    {
        using var temp = new TempFolder();
        var program = temp.File(Path.Combine("build", "s100.exe"));

        Assert.Equal(Path.GetDirectoryName(program), ServerPathPicker.InitialDirectory(program));
    }

    [Fact]
    public void InitialDirectory_IgnoresQuotes()
    {
        using var temp = new TempFolder();
        var program = temp.File("s100.exe");

        Assert.Equal(temp.Path, ServerPathPicker.InitialDirectory($"\"{program}\""));
    }

    [Fact]
    public void InitialDirectory_IsTheFolder_WhenTheProgramIsMissing()
    {
        using var temp = new TempFolder();

        Assert.Equal(temp.Path, ServerPathPicker.InitialDirectory(Path.Combine(temp.Path, "s100.exe")));
    }

    [Fact]
    public void InitialDirectory_IsNone_WhenTheFolderIsMissing()
    {
        using var temp = new TempFolder();

        Assert.Null(ServerPathPicker.InitialDirectory(Path.Combine(temp.Path, "missing", "s100.exe")));
    }
}
