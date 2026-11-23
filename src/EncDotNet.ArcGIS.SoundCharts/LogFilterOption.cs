using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts;

/// <summary>One choice of the SoundCharts Log pane's level filter.</summary>
/// <param name="Filter">The filter.</param>
/// <param name="Label">Its localized label.</param>
internal sealed record LogFilterOption(LogFilter Filter, string Label)
{
    /// <summary>Gets the level filter's choices, in the viewer's order; the first is the default.</summary>
    /// <returns>The choices.</returns>
    public static IReadOnlyList<LogFilterOption> All() =>
    [
        new(LogFilter.InfoAndAbove, Strings.Log_Filter_InfoAndAbove),
        new(LogFilter.WarningsAndErrors, Strings.Log_Filter_WarningsAndErrors),
        new(LogFilter.Errors, Strings.Log_Filter_Errors),
        new(LogFilter.Everything, Strings.Log_Filter_Everything),
    ];

    /// <summary>
    /// Gets the choice a count chip picks, as the viewer's: the errors chip
    /// shows <b>Errors</b>, the warnings chip <b>Warnings and errors</b>.
    /// </summary>
    /// <param name="options">The choices, from <see cref="All"/>.</param>
    /// <param name="level">The chip's level: <see cref="LogLevel.Error"/> or <see cref="LogLevel.Warning"/>.</param>
    /// <returns>The choice, or <see langword="null"/> for another level.</returns>
    public static LogFilterOption? ForCount(IReadOnlyList<LogFilterOption> options, LogLevel level)
    {
        ArgumentNullException.ThrowIfNull(options);
        LogFilter? filter = level switch
        {
            LogLevel.Error => LogFilter.Errors,
            LogLevel.Warning => LogFilter.WarningsAndErrors,
            _ => null,
        };
        return filter is null ? null : options.FirstOrDefault(o => o.Filter == filter);
    }

    /// <inheritdoc/>
    public override string ToString() => Label;
}
