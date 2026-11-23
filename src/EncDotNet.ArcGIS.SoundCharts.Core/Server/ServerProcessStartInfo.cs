namespace EncDotNet.SoundCharts.Server;

/// <summary>What to run for a chart server.</summary>
/// <param name="FileName">The program, <c>s100.exe</c>.</param>
/// <param name="Arguments">The arguments, unquoted.</param>
/// <param name="WorkingDirectory">The working folder, on the long path.</param>
/// <param name="Environment">Environment variables to set for the process.</param>
public sealed record ServerProcessStartInfo(
    string FileName,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    IReadOnlyDictionary<string, string> Environment);
