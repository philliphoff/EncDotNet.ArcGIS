using System.Text.RegularExpressions;
using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts.Server;

/// <summary>Reads chart server output lines for the log.</summary>
public static partial class ServerOutput
{
    /// <summary>
    /// Gets the log level of a line: <c>Error:</c> lines and ASP.NET Core
    /// <c>fail:</c> and <c>crit:</c> lines are errors, <c>Warning:</c>,
    /// <c>warn:</c> and <c>Skipped</c> lines are warnings, <c>dbug:</c> and
    /// <c>trce:</c> are debug; anything else on standard error is an error
    /// (an unhandled exception's stack trace), and on standard output is
    /// information.
    /// </summary>
    /// <param name="line">The line.</param>
    public static LogLevel Classify(ServerOutputLine line)
    {
        ArgumentNullException.ThrowIfNull(line);

        var text = line.Text.TrimStart();
        if (StartsWith(text, "Error:") || StartsWith(text, "fail:") || StartsWith(text, "crit:") || StartsWith(text, "Unhandled exception"))
        {
            return LogLevel.Error;
        }

        if (StartsWith(text, "Warning:") || StartsWith(text, "warn:") || StartsWith(text, "Skipped "))
        {
            return LogLevel.Warning;
        }

        if (StartsWith(text, "dbug:") || StartsWith(text, "trce:"))
        {
            return LogLevel.Debug;
        }

        return line.IsError ? LogLevel.Error : LogLevel.Info;
    }

    /// <summary>Removes ANSI escape sequences, in case the server colours its output.</summary>
    /// <param name="text">The text.</param>
    public static string StripAnsi(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Contains('\u001b', StringComparison.Ordinal) ? AnsiEscape().Replace(text, string.Empty) : text;
    }

    /// <summary>
    /// Gets whether a line says the server couldn't bind its port, so it
    /// should be started again on another one.
    /// </summary>
    /// <param name="text">The line.</param>
    public static bool IsAddressInUse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Contains("address already in use", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Failed to bind", StringComparison.OrdinalIgnoreCase)
            || text.Contains("AddressInUse", StringComparison.OrdinalIgnoreCase);
    }

    private static bool StartsWith(string text, string prefix) => text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"\u001b\[[0-9;?]*[ -/]*[@-~]")]
    private static partial Regex AnsiEscape();
}
