namespace EncDotNet.SoundCharts.OpenWith;

/// <summary>What <see cref="ContentProbe"/> found at one path.</summary>
/// <param name="Path">The full path probed.</param>
/// <param name="Kind">What the path holds.</param>
/// <param name="Product">
/// The product of the first dataset found (such as <c>S-101</c>), or
/// <see langword="null"/> when none was read: always for
/// <see cref="ContentProbeKind.None"/>, and for an exchange set, whose
/// catalogue alone is enough.
/// </param>
public sealed record ContentProbeResult(string Path, ContentProbeKind Kind, string? Product)
{
    /// <summary>Gets whether the path holds something SoundCharts can add to a map.</summary>
    public bool IsChartData => Kind != ContentProbeKind.None;
}
