namespace EncDotNet.SoundCharts.Logging;

/// <summary>
/// The SoundCharts Log pane's level filter, with the choices of the
/// SoundCharts viewer's Logs tab (design brief §4.4).
/// </summary>
public enum LogFilter
{
    /// <summary>"Info and above": everything but Debug. The default.</summary>
    InfoAndAbove,

    /// <summary>"Warnings and errors".</summary>
    WarningsAndErrors,

    /// <summary>"Errors".</summary>
    Errors,

    /// <summary>"Everything, Debug included".</summary>
    Everything,
}

/// <summary>Applies a <see cref="LogFilter"/>.</summary>
public static class LogFilterExtensions
{
    /// <summary>Gets whether <paramref name="filter"/> shows entries of <paramref name="level"/>.</summary>
    /// <param name="filter">The filter.</param>
    /// <param name="level">The entry's level.</param>
    public static bool Includes(this LogFilter filter, LogLevel level) => filter switch
    {
        LogFilter.Everything => true,
        LogFilter.WarningsAndErrors => level >= LogLevel.Warning,
        LogFilter.Errors => level >= LogLevel.Error,
        _ => level >= LogLevel.Info,
    };

    /// <summary>
    /// Gets whether <paramref name="filter"/> shows <paramref name="entry"/>,
    /// optionally only from <paramref name="source"/> (a chart's Log button
    /// shows that chart's lines only).
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="entry">The entry.</param>
    /// <param name="source">The source to keep, or <see langword="null"/> for every source.</param>
    public static bool Includes(this LogFilter filter, LogEntry entry, string? source = null)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return filter.Includes(entry.Level)
            && (source is null || string.Equals(entry.Source, source, StringComparison.Ordinal));
    }
}
