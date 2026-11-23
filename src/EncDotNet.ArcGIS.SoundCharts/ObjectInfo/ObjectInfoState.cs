namespace EncDotNet.SoundCharts.ObjectInfo;

/// <summary>
/// What the <b>SoundCharts Object information</b> pane is showing. Each
/// state has its own look (look and feel design §6, finding A6).
/// </summary>
internal enum ObjectInfoState
{
    /// <summary>Nothing picked yet: an empty state that says how to use the tool.</summary>
    Empty,

    /// <summary>The map has no visible SoundCharts layers at the click: an empty state.</summary>
    NoChartLayers,

    /// <summary>A pick is running: a busy state.</summary>
    Working,

    /// <summary>The pick found nothing at the point: an empty state.</summary>
    NothingHere,

    /// <summary>The pick failed unexpectedly: an error message bar.</summary>
    Failed,

    /// <summary>The pick found features or coverage values: the list and the selected feature's details.</summary>
    Results,
}
