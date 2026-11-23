using System.Runtime.InteropServices;
using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts.Tests.Server;

public class ServerExecutableTests
{
    [Theory]
    [InlineData(Architecture.Arm64, "win-arm64")]
    [InlineData(Architecture.X64, "win-x64")]
    [InlineData(Architecture.X86, "win-x64")]
    public void PreferredRuntimeIdentifier_IsNativeOnArm64(Architecture os, string expected)
    {
        Assert.Equal(expected, ServerExecutable.PreferredRuntimeIdentifier(os));
    }

    [Fact]
    public void Locate_OnArm64_PrefersTheArm64Build()
    {
        using var temp = new TempFolder();
        temp.File(@"server\win-x64\s100.exe");
        var arm64 = temp.File(@"server\win-arm64\s100.exe");

        var executable = ServerExecutable.Locate(temp.Path, Architecture.Arm64, overridePath: null);

        Assert.NotNull(executable);
        Assert.Equal(arm64, executable.Path);
        Assert.Equal("win-arm64", executable.RuntimeIdentifier);
        Assert.False(executable.IsOverride);
    }

    [Fact]
    public void Locate_OnArm64_FallsBackToX64()
    {
        using var temp = new TempFolder();
        var x64 = temp.File(@"server\win-x64\s100.exe");

        var executable = ServerExecutable.Locate(temp.Path, Architecture.Arm64, overridePath: null);

        Assert.Equal(x64, executable?.Path);
        Assert.Equal("win-x64", executable?.RuntimeIdentifier);
    }

    [Fact]
    public void Locate_OnX64_TakesTheX64Build()
    {
        using var temp = new TempFolder();
        temp.File(@"server\win-arm64\s100.exe");
        var x64 = temp.File(@"server\win-x64\s100.exe");

        Assert.Equal(x64, ServerExecutable.Locate(temp.Path, Architecture.X64, overridePath: null)?.Path);
    }

    [Fact]
    public void Locate_OnX64_NeverTakesTheArm64Build()
    {
        using var temp = new TempFolder();
        temp.File(@"server\win-arm64\s100.exe");

        Assert.Null(ServerExecutable.Locate(temp.Path, Architecture.X64, overridePath: null));
    }

    [Fact]
    public void Locate_TakesTheOverride()
    {
        using var temp = new TempFolder();
        temp.File(@"server\win-x64\s100.exe");
        var custom = temp.File(@"custom\s100.exe");

        var executable = ServerExecutable.Locate(temp.Path, Architecture.X64, $"\"{custom}\"");

        Assert.Equal(custom, executable?.Path);
        Assert.True(executable?.IsOverride);
        Assert.Null(executable?.RuntimeIdentifier);
    }

    [Fact]
    public void Locate_WithAMissingOverride_FindsNothing()
    {
        using var temp = new TempFolder();
        temp.File(@"server\win-x64\s100.exe");

        Assert.Null(ServerExecutable.Locate(temp.Path, Architecture.X64, Path.Combine(temp.Path, "missing.exe")));
    }

    [Fact]
    public void Version_OfAFileWithoutVersionInfo_IsNull()
    {
        using var temp = new TempFolder();
        var executable = new ServerExecutable(temp.File("s100.exe", "not a program"), "win-x64", IsOverride: false);

        Assert.Null(executable.Version);
    }

    [Fact]
    public void TheBundledServer_HasTheRecordedVersion()
    {
        var bundled = ServerExecutable.Locate(AppContext.BaseDirectory, RuntimeInformation.OSArchitecture, overridePath: null);
        Assert.SkipWhen(bundled is null, "No bundled server; run tools/build-server.ps1 before building.");

        Assert.Matches(@"^\d+\.\d+\.\d+.*\+[0-9a-f]{40}$", bundled.Version);
    }
}
