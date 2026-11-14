using System.IO;

namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>
/// One dataset of a SoundCharts layer, as Add SoundCharts Data found it and
/// the layer's marker keeps it: where the file is, what it is, and its
/// footprint.
/// </summary>
/// <param name="Path">The dataset file's full path.</param>
/// <param name="Product">The product specification read from the content, such as <c>S-101</c> or <c>S-57</c>.</param>
/// <param name="Edition">The edition the dataset declares, such as <c>2.0.0</c>, or <see langword="null"/> when it says none.</param>
/// <param name="Extent">The WGS-84 footprint, or <see langword="null"/> when it isn't known.</param>
/// <param name="Producer">The producing agency's name or code, or <see langword="null"/>.</param>
/// <param name="NotForNavigation"><see langword="true"/> when the producer marks the dataset not for navigation.</param>
public sealed record ChartLayerDataset(
    string Path,
    string Product,
    string? Edition = null,
    GeoExtent? Extent = null,
    string? Producer = null,
    bool NotForNavigation = false)
{
    /// <summary>Gets the dataset's name: its file name without the extension, such as <c>US5MA1BO</c>.</summary>
    public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);

    /// <summary>Gets the layer the dataset belongs to.</summary>
    public ChartLayerKind Kind => ChartLayerKinds.ForProduct(Product);

    /// <summary>Gets the product and edition for display, such as <c>S-101 2.0.0</c>.</summary>
    public string ProductAndEdition => Edition is null ? Product : $"{Product} {Edition}";

    /// <summary>Gets whether the file is still there.</summary>
    public bool Exists => File.Exists(Path);
}
