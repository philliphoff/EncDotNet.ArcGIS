using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts;

/// <summary>
/// Writes the add-in's own events to the SoundCharts log
/// (<see cref="SoundChartsSession.Log"/>): the SoundCharts Log pane and
/// <c>%LOCALAPPDATA%\SoundCharts\ArcGIS\logs\soundcharts-yyyyMMdd.log</c>.
/// </summary>
/// <remarks>
/// Writing never throws: a logging failure must not stop the add-in or Pro.
/// </remarks>
internal static class AddInLog
{
    /// <summary>Adds an Info entry.</summary>
    /// <param name="message">The text.</param>
    public static void Write(string message) => Write(LogLevel.Info, message);

    /// <summary>Adds an entry of <paramref name="level"/>.</summary>
    /// <param name="level">The severity.</param>
    /// <param name="message">The text.</param>
    public static void Write(LogLevel level, string message)
    {
        try
        {
            SoundChartsSession.Log.Write(level, SoundChartsLog.AddInSource, message);
        }
        catch (Exception)
        {
            // There is nowhere to report it; carry on without the log.
        }
    }
}
