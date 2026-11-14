using System.Globalization;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>
/// The provenance text of a SoundCharts layer (design brief §3.3, §7.1,
/// D34): its description, "SoundCharts portrayal of {products} from
/// {producers}. Not for navigation.", and its credits, "Data: {producers}.
/// Portrayal: SoundCharts ({s100 version}). Not for navigation."
/// </summary>
/// <remarks>
/// Pro's service layers have no metadata the SDK can set (open question P7),
/// so the use limitation goes in the layer's description and attribution,
/// which Layer Properties and the attribution bar show.
/// </remarks>
public static class ChartCredits
{
    /// <summary>Gets the products of <paramref name="datasets"/>, in product order, such as <c>S-57, S-101</c>.</summary>
    /// <param name="datasets">The datasets.</param>
    public static string Products(IEnumerable<ChartLayerDataset> datasets) =>
        string.Join(", ", datasets.Select(d => d.Product).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));

    /// <summary>
    /// Gets the producers of <paramref name="datasets"/>, in order of dataset
    /// count, then by name; <see cref="Strings.Credits_UnknownProducer"/> when none is known.
    /// </summary>
    /// <param name="datasets">The datasets.</param>
    public static string Producers(IEnumerable<ChartLayerDataset> datasets)
    {
        var producers = datasets
            .Where(d => !string.IsNullOrWhiteSpace(d.Producer))
            .GroupBy(d => d.Producer!.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Key)
            .ToList();
        return producers.Count == 0 ? Strings.Credits_UnknownProducer : string.Join(", ", producers);
    }

    /// <summary>Gets a layer's description.</summary>
    /// <param name="datasets">The datasets it draws.</param>
    public static string Description(IReadOnlyCollection<ChartLayerDataset> datasets)
    {
        ArgumentNullException.ThrowIfNull(datasets);
        return string.Format(CultureInfo.InvariantCulture, Strings.Credits_Description, Products(datasets), Producers(datasets));
    }

    /// <summary>Gets a layer's credits.</summary>
    /// <param name="datasets">The datasets it draws.</param>
    /// <param name="serverVersion">The <c>s100</c> version that draws it, or <see langword="null"/> when unknown.</param>
    public static string Attribution(IReadOnlyCollection<ChartLayerDataset> datasets, string? serverVersion)
    {
        ArgumentNullException.ThrowIfNull(datasets);
        return string.Format(
            CultureInfo.InvariantCulture,
            Strings.Credits_Attribution,
            Producers(datasets),
            string.IsNullOrWhiteSpace(serverVersion) ? VersionText.Unknown : serverVersion);
    }
}
