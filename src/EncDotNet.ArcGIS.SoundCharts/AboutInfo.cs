using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using EncDotNet.S100;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The versions shown by <b>About SoundCharts</b> and written to the log when
/// the add-in loads.
/// </summary>
/// <param name="AddInVersion">The SoundCharts add-in version.</param>
/// <param name="S100Version">The EncDotNet.S100 library version.</param>
/// <param name="ProVersion">The ArcGIS Pro version hosting the add-in.</param>
/// <param name="RuntimeVersion">The .NET runtime version (<see cref="Environment.Version"/>).</param>
/// <param name="ProcessArchitecture">The architecture the process runs as.</param>
/// <param name="OSArchitecture">The architecture of the operating system.</param>
internal sealed record AboutInfo(
    string AddInVersion,
    string S100Version,
    string ProVersion,
    string RuntimeVersion,
    Architecture ProcessArchitecture,
    Architecture OSArchitecture)
{
    /// <summary>Collects the versions of the running process.</summary>
    public static AboutInfo Collect() => new(
        VersionText.FromAssembly(typeof(AboutInfo).Assembly),
        VersionText.FromAssembly(typeof(S100Dataset).Assembly),
        GetHostVersion(),
        Environment.Version.ToString(),
        RuntimeInformation.ProcessArchitecture,
        RuntimeInformation.OSArchitecture);

    /// <summary>
    /// Gets the process architecture, and the operating system's when they
    /// differ: Pro runs as x64 under emulation on Arm64 Windows.
    /// </summary>
    public string ArchitectureText => ProcessArchitecture == OSArchitecture
        ? FormatArchitecture(ProcessArchitecture)
        : string.Format(
            CultureInfo.CurrentCulture,
            Strings.About_ArchitectureEmulated,
            FormatArchitecture(ProcessArchitecture),
            FormatArchitecture(OSArchitecture));

    /// <summary>Gets the labelled rows shown in the About window.</summary>
    public IReadOnlyList<AboutRow> Rows =>
    [
        new(Strings.About_AddInVersion, AddInVersion),
        new(Strings.About_S100Version, S100Version),
        new(Strings.About_ProVersion, ProVersion),
        new(Strings.About_RuntimeVersion, RuntimeVersion),
        new(Strings.About_Architecture, ArchitectureText),
    ];

    /// <summary>Formats the rows as lines of <c>Label: value</c>, for the clipboard.</summary>
    public string ToText() =>
        string.Join(Environment.NewLine, Rows.Select(row => $"{row.Label}: {row.Value}"));

    /// <summary>Formats the versions on one line, in English, for the log.</summary>
    public string ToSingleLine() =>
        $"add-in {AddInVersion}; EncDotNet.S100 {S100Version}; ArcGIS Pro {ProVersion}; " +
        $".NET {RuntimeVersion}; process {FormatArchitecture(ProcessArchitecture)}; " +
        $"OS {FormatArchitecture(OSArchitecture)}";

    /// <summary>Gets the conventional name of <paramref name="architecture"/>, such as <c>x64</c>.</summary>
    public static string FormatArchitecture(Architecture architecture) => architecture switch
    {
        Architecture.X64 => "x64",
        Architecture.X86 => "x86",
        Architecture.Arm64 => "Arm64",
        Architecture.Arm => "Arm",
        _ => architecture.ToString(),
    };

    private static string GetHostVersion()
    {
        var path = Environment.ProcessPath;
        if (path is null)
        {
            return VersionText.Unknown;
        }

        var version = FileVersionInfo.GetVersionInfo(path).ProductVersion;
        return string.IsNullOrWhiteSpace(version) ? VersionText.Unknown : version;
    }
}
