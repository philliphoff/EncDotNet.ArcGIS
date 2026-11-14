namespace EncDotNet.SoundCharts.Export;

/// <summary>
/// What one layout map frame shows: its extent in Web Mercator (EPSG:3857)
/// metres and its size on the page.
/// </summary>
/// <remarks>
/// The extent is the Web Mercator envelope of the frame's view, whatever the
/// map's own coordinate system and rotation: chart servers draw in Web
/// Mercator, and Pro projects the rendered chart into the frame.
/// </remarks>
/// <param name="MinX">The western edge, in Web Mercator metres.</param>
/// <param name="MinY">The southern edge, in Web Mercator metres.</param>
/// <param name="MaxX">The eastern edge, in Web Mercator metres.</param>
/// <param name="MaxY">The northern edge, in Web Mercator metres.</param>
/// <param name="WidthInches">The frame's width on the page, in inches.</param>
/// <param name="HeightInches">The frame's height on the page, in inches.</param>
public sealed record ExportFrame(
    double MinX,
    double MinY,
    double MaxX,
    double MaxY,
    double WidthInches,
    double HeightInches)
{
    /// <summary>Gets the extent's width, in Web Mercator metres.</summary>
    public double Width => MaxX - MinX;

    /// <summary>Gets the extent's height, in Web Mercator metres.</summary>
    public double Height => MaxY - MinY;

    /// <summary>
    /// Gets the Web Mercator metres one pixel covers when the frame is drawn
    /// at <paramref name="dpi"/>, the larger of the two axes' values.
    /// </summary>
    /// <param name="dpi">The resolution, in dots per inch.</param>
    public double MetresPerPixel(double dpi) =>
        Math.Max(Width / (WidthInches * dpi), Height / (HeightInches * dpi));

    /// <summary>Throws when the extent or the page size is empty or not finite.</summary>
    /// <exception cref="ArgumentException">The frame has no area.</exception>
    public void Validate()
    {
        double[] values = [MinX, MinY, MaxX, MaxY, WidthInches, HeightInches];
        if (values.Any(v => !double.IsFinite(v)) || Width <= 0 || Height <= 0 || WidthInches <= 0 || HeightInches <= 0)
        {
            throw new ArgumentException($"The map frame has no area to export: {this}.");
        }
    }
}
