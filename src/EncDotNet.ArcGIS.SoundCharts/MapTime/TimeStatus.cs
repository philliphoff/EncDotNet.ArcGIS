namespace EncDotNet.SoundCharts.MapTime;

/// <summary>
/// What a time-varying SoundCharts layer draws at an instant (design brief
/// §6.3, D30, D31): the step it snaps to, or no data and the nearest data on
/// either side.
/// </summary>
/// <param name="Instant">The map instant (UTC) the status is for.</param>
/// <param name="Step">The step (UTC) drawn, or <see langword="null"/> when there's no data at the instant.</param>
/// <param name="Previous">With no data, the last step (UTC) before the instant, or <see langword="null"/>.</param>
/// <param name="Next">With no data, the first step (UTC) after the instant, or <see langword="null"/>.</param>
public sealed record TimeStatus(DateTime Instant, DateTime? Step, DateTime? Previous = null, DateTime? Next = null)
{
    /// <summary>Gets whether the layer has data at the instant.</summary>
    public bool HasData => Step is not null;

    /// <summary>
    /// Gets the step nearest the instant on either side when there's no data,
    /// for <b>Go to Nearest Data</b>; the drawn step otherwise; or
    /// <see langword="null"/> when the layer has no steps.
    /// </summary>
    public DateTime? Nearest => Step ?? (Previous, Next) switch
    {
        ({ } previous, { } next) => Instant - previous <= next - Instant ? previous : next,
        ({ } previous, null) => previous,
        (null, { } next) => next,
        _ => null,
    };
}
