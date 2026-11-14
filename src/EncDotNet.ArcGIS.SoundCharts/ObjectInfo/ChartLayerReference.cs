namespace EncDotNet.SoundCharts.ObjectInfo;

/// <summary>
/// A visible layer of the active map, as the Object information tool hands it
/// to an <see cref="IChartDatasetResolver"/>. It carries only plain values, so
/// resolvers don't need the Pro API and can be tested without Pro.
/// </summary>
/// <param name="LayerUri">The layer's URI in the project (Pro's <c>MapMember.URI</c>).</param>
/// <param name="Name">The layer's name in Contents.</param>
/// <param name="MapUri">The URI of the map that holds the layer.</param>
public sealed record ChartLayerReference(string LayerUri, string Name, string MapUri);
