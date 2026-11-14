namespace EncDotNet.SoundCharts.Export;

/// <summary>A format in the export window's list.</summary>
/// <param name="Format">The format.</param>
/// <param name="Label">Its localized name.</param>
internal sealed record ExportFormatOption(ChartExportFormat Format, string Label)
{
    /// <inheritdoc/>
    public override string ToString() => Label;
}
