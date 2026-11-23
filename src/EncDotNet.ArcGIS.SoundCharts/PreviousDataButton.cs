namespace EncDotNet.SoundCharts;

/// <summary><b>‹ Previous data</b> (<c>SoundCharts_PreviousData</c>, design brief §6.6).</summary>
internal sealed class PreviousDataButton : DataStepButton
{
    /// <inheritdoc/>
    protected override bool Forward => false;

    /// <inheritdoc/>
    protected override string Name => "Previous Data";
}
