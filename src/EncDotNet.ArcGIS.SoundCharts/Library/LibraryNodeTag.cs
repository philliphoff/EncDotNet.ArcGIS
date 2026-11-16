namespace EncDotNet.SoundCharts.Library;

/// <summary>
/// A tag on a Library dataset row, such as <b>Loaded</b> or <b>Missing</b>,
/// in the viewer's words. The pane shows it as a chip: neutral, or a warning
/// chip for a problem.
/// </summary>
/// <param name="Text">The tag's text.</param>
/// <param name="IsWarning">Whether the tag is a problem, such as a dataset whose file is gone.</param>
public sealed record LibraryNodeTag(string Text, bool IsWarning = false);
