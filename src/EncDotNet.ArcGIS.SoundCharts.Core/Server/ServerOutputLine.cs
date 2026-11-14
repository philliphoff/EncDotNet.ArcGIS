namespace EncDotNet.SoundCharts.Server;

/// <summary>One line a chart server process wrote.</summary>
/// <param name="Text">The line, without its line break.</param>
/// <param name="IsError">Whether it came from standard error.</param>
public sealed record ServerOutputLine(string Text, bool IsError);
