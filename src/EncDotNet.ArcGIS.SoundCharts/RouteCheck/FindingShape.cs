namespace EncDotNet.SoundCharts.RouteCheck;

/// <summary>Whether a finding is at a place on the route or along a stretch of it.</summary>
public enum FindingShape
{
    /// <summary>A place: a danger, a crossing, a minimum. Goes in <b>Findings (points)</b>.</summary>
    Point,

    /// <summary>A stretch of the route. Goes in <b>Findings (segments)</b>.</summary>
    Segment,
}
