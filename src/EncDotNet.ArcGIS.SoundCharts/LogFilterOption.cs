using EncDotNet.SoundCharts.Logging;

namespace EncDotNet.SoundCharts;

/// <summary>One choice of the SoundCharts Log pane's level filter.</summary>
/// <param name="Filter">The filter.</param>
/// <param name="Label">Its localized label.</param>
internal sealed record LogFilterOption(LogFilter Filter, string Label)
{
    /// <inheritdoc/>
    public override string ToString() => Label;
}
