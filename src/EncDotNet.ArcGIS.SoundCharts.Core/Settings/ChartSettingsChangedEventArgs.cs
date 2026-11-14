namespace EncDotNet.SoundCharts.Settings;

/// <summary>A change to <see cref="ChartSettings"/>.</summary>
/// <param name="before">What the layers drew with before.</param>
/// <param name="after">What they draw with now.</param>
/// <param name="projectChanged">Whether the project's own settings changed, so the project needs saving.</param>
public sealed class ChartSettingsChangedEventArgs(EffectiveChartSettings before, EffectiveChartSettings after, bool projectChanged) : EventArgs
{
    /// <summary>Gets what the layers drew with before.</summary>
    public EffectiveChartSettings Before { get; } = before;

    /// <summary>Gets what they draw with now.</summary>
    public EffectiveChartSettings After { get; } = after;

    /// <summary>Gets whether the project's own settings changed, so the project needs saving.</summary>
    public bool ProjectChanged { get; } = projectChanged;

    /// <summary>Gets what the change means for the layers.</summary>
    public ChartSettingsChange Change => After.ChangeFrom(Before);
}
