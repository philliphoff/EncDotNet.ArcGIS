namespace EncDotNet.SoundCharts.MapTime;

/// <summary>
/// What a time-varying SoundCharts layer draws at an instant (design brief
/// §6.3, D30, D31): the step it snaps to, or no data and the nearest data on
/// either side.
/// </summary>
/// <param name="Instant">The map instant (UTC) the status is for.</param>
/// <param name="Step">
/// The step (UTC) drawn, or for a <see cref="TimeSelection.Continuous"/>
/// layer the time drawn; <see langword="null"/> when there's no data at the
/// instant.
/// </param>
/// <param name="Previous">
/// With no data, the last step (UTC) before the instant, or for a continuous
/// layer after its range, where its track ends; for a continuous layer with
/// data, the step at or before the time drawn. <see langword="null"/> when there's none.
/// </param>
/// <param name="Next">
/// With no data, the first step (UTC) after the instant, or for a continuous
/// layer before its range, where its track starts; for a continuous layer
/// with data, the step after the time drawn. <see langword="null"/> when there's none.
/// </param>
/// <param name="Selection">How the layer picks what it draws.</param>
public sealed record TimeStatus(
    DateTime Instant,
    DateTime? Step,
    DateTime? Previous = null,
    DateTime? Next = null,
    TimeSelection Selection = TimeSelection.Nearest)
{
    /// <summary>Gets whether the layer has data at the instant.</summary>
    public bool HasData => Step is not null;

    /// <summary>
    /// Gets whether the layer draws anything at the instant: with data, or
    /// for a <see cref="TimeSelection.Continuous"/> layer always, since a
    /// tropical cyclone outside its range still draws its cone and track,
    /// without the storm.
    /// </summary>
    public bool Draws => HasData || Selection == TimeSelection.Continuous;

    /// <summary>
    /// Gets whether a <see cref="TimeSelection.Continuous"/> layer draws a
    /// time between two of its steps: a storm estimated between forecast
    /// points.
    /// </summary>
    public bool IsBetweenSteps =>
        Selection == TimeSelection.Continuous && Step is { } step && Previous is { } previous && step != previous && Next is not null;

    /// <summary>
    /// Gets whether a <see cref="TimeSelection.Continuous"/> layer draws a
    /// time before its first step: a storm on its past track.
    /// </summary>
    public bool IsBeforeSteps => Selection == TimeSelection.Continuous && Step is not null && Previous is null;

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
