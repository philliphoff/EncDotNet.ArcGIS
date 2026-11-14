namespace EncDotNet.SoundCharts.Export;

/// <summary>File extensions and resolutions of <see cref="ChartExportFormat"/>.</summary>
public static class ChartExportFormats
{
    /// <summary>Gets the formats in the order the export window lists them.</summary>
    public static IReadOnlyList<ChartExportFormat> All { get; } =
        [ChartExportFormat.Pdf, ChartExportFormat.Png, ChartExportFormat.Jpeg, ChartExportFormat.Tiff];

    /// <summary>
    /// Gets the resolutions the export window offers, in dots per inch. 96 is
    /// the screen's; 300 and 600 are print resolutions (issue #12).
    /// </summary>
    public static IReadOnlyList<int> Resolutions { get; } = [96, 150, 300, 600];

    /// <summary>Gets the resolution the export window starts with.</summary>
    public const int DefaultResolution = 300;

    /// <summary>Gets the lowest resolution an export accepts.</summary>
    public const int MinResolution = 72;

    /// <summary>Gets the highest resolution an export accepts.</summary>
    public const int MaxResolution = 1200;

    /// <summary>Gets the file extension of <paramref name="format"/>, with its dot.</summary>
    /// <param name="format">The format.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> isn't a known format.</exception>
    public static string Extension(ChartExportFormat format) => format switch
    {
        ChartExportFormat.Pdf => ".pdf",
        ChartExportFormat.Png => ".png",
        ChartExportFormat.Jpeg => ".jpg",
        ChartExportFormat.Tiff => ".tif",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown export format."),
    };

    /// <summary>
    /// Gets the format a file name's extension names, or <see langword="null"/>
    /// when it names none of them.
    /// </summary>
    /// <param name="path">The file name or path.</param>
    public static ChartExportFormat? FromPath(string? path) =>
        Path.GetExtension(path ?? string.Empty).ToUpperInvariant() switch
        {
            ".PDF" => ChartExportFormat.Pdf,
            ".PNG" => ChartExportFormat.Png,
            ".JPG" or ".JPEG" => ChartExportFormat.Jpeg,
            ".TIF" or ".TIFF" => ChartExportFormat.Tiff,
            _ => null,
        };

    /// <summary>
    /// Gets <paramref name="path"/> with the extension of
    /// <paramref name="format"/>, unless it already has one of that format's
    /// extensions.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="format">The format.</param>
    public static string WithExtension(string path, ChartExportFormat format)
    {
        ArgumentNullException.ThrowIfNull(path);
        return FromPath(path) == format ? path : Path.ChangeExtension(path, Extension(format));
    }

    /// <summary>Gets whether <paramref name="dpi"/> is a resolution an export accepts.</summary>
    /// <param name="dpi">The resolution, in dots per inch.</param>
    public static bool IsValidResolution(int dpi) => dpi is >= MinResolution and <= MaxResolution;
}
