namespace EncDotNet.SoundCharts.RouteCheck;

/// <summary>The viewer's tones for a verdict, a summary chip and a finding row.</summary>
public enum RouteCheckTone
{
    /// <summary>Nothing to judge, or no data.</summary>
    Neutral,

    /// <summary>Meets the settings.</summary>
    Success,

    /// <summary>Something to check, or tight.</summary>
    Warning,

    /// <summary>Something to review: below the UKC setting, land, a danger on the track.</summary>
    Destructive,
}
