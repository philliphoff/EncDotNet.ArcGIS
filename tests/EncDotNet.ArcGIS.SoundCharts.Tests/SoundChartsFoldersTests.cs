namespace EncDotNet.SoundCharts.Tests;

public class SoundChartsFoldersTests
{
    [Fact]
    public void TheFolders_FollowTheBriefsLayout()
    {
        using var temp = new TempFolder();
        var folders = new SoundChartsFolders(temp.Path, 4242);

        Assert.Equal(Path.Combine(temp.Path, "sessions"), folders.Sessions);
        Assert.Equal(Path.Combine(temp.Path, "sessions", "4242"), folders.Session);
        Assert.Equal(Path.Combine(temp.Path, "tile-cache"), folders.TileCache);
        Assert.Equal(Path.Combine(temp.Path, "logs"), folders.Logs);
    }

    [Fact]
    public void TheRoot_IsTheLongPath()
    {
        using var temp = new TempFolder();
        var longName = Directory.CreateDirectory(Path.Combine(temp.Path, "SoundCharts for ArcGIS long name")).FullName;
        var shortName = ShortPath(longName);
        if (shortName is null || !LongPath.LooksShort(shortName))
        {
            Assert.Skip("8.3 names are off on this volume.");
        }

        var folders = new SoundChartsFolders(shortName, 1);

        Assert.Equal(longName, folders.Root, ignoreCase: true);
        Assert.False(LongPath.LooksShort(folders.CreateServerWorkingDirectory("3f9a1c02")));
    }

    [Fact]
    public void CreateServerWorkingDirectory_IsInTheSession()
    {
        using var temp = new TempFolder();
        var folders = new SoundChartsFolders(temp.Path, 4242);

        var folder = folders.CreateServerWorkingDirectory("3f9a1c02");

        Assert.Equal(Path.Combine(folders.Session, "3f9a1c02"), folder);
        Assert.True(Directory.Exists(folder));
    }

    [Fact]
    public void DeleteStaleSessions_KeepsRunningProcesses()
    {
        using var temp = new TempFolder();
        var folders = new SoundChartsFolders(temp.Path, 1);
        folders.CreateServerWorkingDirectory("mine");
        temp.Folder(@"sessions\2\a");
        temp.Folder(@"sessions\3\b");
        temp.Folder(@"sessions\not-a-process");

        var deleted = folders.DeleteStaleSessions(id => id == 3);

        Assert.Equal(1, deleted);
        Assert.True(Directory.Exists(folders.Session));
        Assert.False(Directory.Exists(Path.Combine(folders.Sessions, "2")));
        Assert.True(Directory.Exists(Path.Combine(folders.Sessions, "3")));
        Assert.True(Directory.Exists(Path.Combine(folders.Sessions, "not-a-process")));
    }

    [Fact]
    public void DeleteSession_RemovesThisProcessesFolder()
    {
        using var temp = new TempFolder();
        var folders = new SoundChartsFolders(temp.Path, 7);
        folders.CreateServerWorkingDirectory("3f9a1c02");

        folders.DeleteSession();

        Assert.False(Directory.Exists(folders.Session));
    }

    [Theory]
    [InlineData(@"C:\Users\PHOFF~1.RED\AppData", true)]
    [InlineData(@"C:\PROGRA~2\x", true)]
    [InlineData(@"C:\Users\phoff.REDMOND\AppData", false)]
    [InlineData(@"C:\Users\a~b\x", false)]
    public void LooksShort_SpotsEightDotThreeNames(string path, bool expected)
    {
        Assert.Equal(expected, LongPath.LooksShort(path));
    }

    private static string? ShortPath(string path)
    {
        var buffer = new char[1024];
        var length = GetShortPathName(path, buffer, (uint)buffer.Length);
        return length == 0 || length > buffer.Length ? null : new string(buffer, 0, (int)length);
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, EntryPoint = "GetShortPathNameW")]
    private static extern uint GetShortPathName(string longPath, char[] shortPath, uint bufferLength);
}
