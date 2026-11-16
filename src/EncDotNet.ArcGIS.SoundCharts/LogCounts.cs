using System.Globalization;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The errors and warnings in the SoundCharts log, for the Log pane's count
/// chips (look and feel design §6, §10 decision 10; the viewer's logs panel
/// D8). Counts every level, whatever the level filter shows, so choosing
/// <b>Errors</b> doesn't hide the warnings' count.
/// </summary>
/// <param name="Errors">The number of Error entries.</param>
/// <param name="Warnings">The number of Warning entries.</param>
internal readonly record struct LogCounts(int Errors, int Warnings)
{
    /// <summary>Gets whether there are errors: the errors chip shows only then.</summary>
    public bool HasErrors => Errors > 0;

    /// <summary>Gets whether there are warnings: the warnings chip shows only then.</summary>
    public bool HasWarnings => Warnings > 0;

    /// <summary>Gets the errors chip's text, such as <c>2 errors</c>.</summary>
    public string ErrorsText => Errors == 1
        ? Strings.Log_ErrorCount_One
        : string.Format(CultureInfo.CurrentCulture, Strings.Log_ErrorCount_Format, Errors);

    /// <summary>Gets the warnings chip's text, such as <c>5 warnings</c>.</summary>
    public string WarningsText => Warnings == 1
        ? Strings.Log_WarningCount_One
        : string.Format(CultureInfo.CurrentCulture, Strings.Log_WarningCount_Format, Warnings);

    /// <summary>Counts the errors and warnings in <paramref name="entries"/>.</summary>
    /// <param name="entries">The log's entries.</param>
    /// <param name="source">The one source to count, as the pane's source filter, or <see langword="null"/> for every source.</param>
    public static LogCounts Of(IEnumerable<LogEntry> entries, string? source = null)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var errors = 0;
        var warnings = 0;
        foreach (var entry in entries)
        {
            if (source is not null && !string.Equals(entry.Source, source, StringComparison.Ordinal))
            {
                continue;
            }

            switch (entry.Level)
            {
                case LogLevel.Error:
                    errors++;
                    break;
                case LogLevel.Warning:
                    warnings++;
                    break;
            }
        }

        return new LogCounts(errors, warnings);
    }
}
