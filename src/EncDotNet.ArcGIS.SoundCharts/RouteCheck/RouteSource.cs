namespace EncDotNet.SoundCharts.RouteCheck;

/// <summary>Where a checked route came from.</summary>
public enum RouteSource
{
    /// <summary>A line feature selected in the map.</summary>
    Feature,

    /// <summary>A line drawn on the map with <b>Draw Route</b>.</summary>
    Sketch,

    /// <summary>An S-421 route plan file.</summary>
    S421,

    /// <summary>An RTZ route exchange file (IEC 61174).</summary>
    Rtz,
}
