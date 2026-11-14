namespace EncDotNet.SoundCharts.Export;

/// <summary>What <b>Export SoundCharts Chart...</b> writes: the file, its format and resolution.</summary>
/// <param name="OutputPath">The file to write.</param>
/// <param name="Format">The file format.</param>
/// <param name="Dpi">The resolution, in dots per inch.</param>
public sealed record ChartExportSettings(string OutputPath, ChartExportFormat Format, int Dpi)
{
    /// <summary>Throws when the settings can't be exported.</summary>
    /// <exception cref="ArgumentException">The path is empty, or doesn't match the format.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The resolution is outside the accepted range.</exception>
    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(OutputPath);
        if (ChartExportFormats.FromPath(OutputPath) != Format)
        {
            throw new ArgumentException($"'{OutputPath}' isn't a {ChartExportFormats.Extension(Format)} file.", nameof(OutputPath));
        }

        if (!ChartExportFormats.IsValidResolution(Dpi))
        {
            throw new ArgumentOutOfRangeException(
                nameof(Dpi),
                Dpi,
                $"The resolution must be between {ChartExportFormats.MinResolution} and {ChartExportFormats.MaxResolution} dpi.");
        }
    }
}
