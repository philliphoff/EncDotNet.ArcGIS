namespace EncDotNet.SoundCharts.Export;

/// <summary>
/// The file formats <b>Export SoundCharts Chart...</b> writes a layout to
/// (issue #12): PDF and the raster formats Pro can georeference to a map
/// frame.
/// </summary>
public enum ChartExportFormat
{
    /// <summary>PDF, with each map frame's coordinate system (GeoPDF).</summary>
    Pdf,

    /// <summary>PNG, with a world file for the georeferencing map frame.</summary>
    Png,

    /// <summary>JPEG, with a world file for the georeferencing map frame.</summary>
    Jpeg,

    /// <summary>TIFF, with GeoTIFF tags for the georeferencing map frame.</summary>
    Tiff,
}
