using System.Globalization;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts.Export;

/// <summary>A resolution in the export window's list.</summary>
/// <param name="Dpi">The resolution, in dots per inch.</param>
internal sealed record ExportResolutionOption(int Dpi)
{
    /// <summary>Gets the localized label, such as "300 dpi".</summary>
    public string Label => string.Format(CultureInfo.CurrentCulture, Strings.Export_ResolutionFormat, Dpi);

    /// <inheritdoc/>
    public override string ToString() => Label;
}
