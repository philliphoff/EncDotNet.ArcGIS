using System.Runtime.InteropServices;

namespace EncDotNet.SoundCharts.Tests;

public class AboutInfoTests
{
    private static AboutInfo Create(Architecture process, Architecture os) =>
        new("0.1.0", "0.27.0 (001df0e)", "3.7.3.62345", "10.0.15", process, os);

    [Fact]
    public void ArchitectureText_NamesOneArchitecture_WhenNative()
    {
        Assert.Equal("x64", Create(Architecture.X64, Architecture.X64).ArchitectureText);
    }

    [Fact]
    public void ArchitectureText_NamesTheHost_WhenEmulated()
    {
        Assert.Equal("x64 (emulated on Arm64)", Create(Architecture.X64, Architecture.Arm64).ArchitectureText);
    }

    [Fact]
    public void ToText_ListsEveryVersion()
    {
        var text = Create(Architecture.X64, Architecture.Arm64).ToText();

        Assert.Equal(
            [
                "SoundCharts: 0.1.0",
                "EncDotNet.S100: 0.27.0 (001df0e)",
                "ArcGIS Pro: 3.7.3.62345",
                ".NET: 10.0.15",
                "Architecture: x64 (emulated on Arm64)",
            ],
            text.Split(Environment.NewLine));
    }

    [Fact]
    public void ToSingleLine_IsOneLine()
    {
        var line = Create(Architecture.X64, Architecture.Arm64).ToSingleLine();

        Assert.Equal(
            "add-in 0.1.0; EncDotNet.S100 0.27.0 (001df0e); ArcGIS Pro 3.7.3.62345; .NET 10.0.15; process x64; OS Arm64",
            line);
    }

    [Fact]
    public void Collect_ReportsTheRunningProcess()
    {
        var info = AboutInfo.Collect();

        Assert.Equal(Environment.Version.ToString(), info.RuntimeVersion);
        Assert.Equal(RuntimeInformation.ProcessArchitecture, info.ProcessArchitecture);
        Assert.NotEqual(VersionText.Unknown, info.S100Version);
    }
}
