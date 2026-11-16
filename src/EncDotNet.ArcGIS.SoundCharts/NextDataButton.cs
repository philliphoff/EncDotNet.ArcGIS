namespace EncDotNet.SoundCharts;

/// <summary><b>Next data ›</b> (<c>SoundCharts_NextData</c>, design brief §6.6).</summary>
internal sealed class NextDataButton : DataStepButton
{
    /// <inheritdoc/>
    protected override bool Forward => true;

    /// <inheritdoc/>
    protected override string Name => "Next Data";
}
