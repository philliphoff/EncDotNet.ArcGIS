namespace EncDotNet.SoundCharts.RouteCheck;

/// <summary>What the <b>SoundCharts Route check</b> pane is showing, for the view to pick its layout.</summary>
public enum RouteCheckPaneState
{
    /// <summary>No check yet: the pane says how to start one.</summary>
    Empty,

    /// <summary>A route is being checked: the verdict card shows a spinner.</summary>
    Working,

    /// <summary>A check finished: the verdict card, summary and findings.</summary>
    Verdict,

    /// <summary>A check couldn't run: an error message bar says why.</summary>
    Failed,
}
