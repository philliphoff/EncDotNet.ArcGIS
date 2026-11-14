namespace EncDotNet.SoundCharts.Library;

/// <summary>What a finished <c>s100 validate</c> process left.</summary>
/// <param name="ExitCode">Its exit code.</param>
/// <param name="StandardOutput">Everything it wrote to standard output.</param>
/// <param name="StandardError">Everything it wrote to standard error.</param>
public sealed record ValidationProcessResult(int ExitCode, string StandardOutput, string StandardError);
