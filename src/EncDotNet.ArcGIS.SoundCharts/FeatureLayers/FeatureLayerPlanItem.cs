namespace EncDotNet.SoundCharts.FeatureLayers;

/// <summary>One layer or standalone table of a <see cref="FeatureLayerPlan"/>.</summary>
/// <param name="TableName">The plugin datasource's table name.</param>
/// <param name="Name">The layer or table name in Contents.</param>
/// <param name="IsSpatial">Whether it's a feature layer (otherwise a standalone table).</param>
public sealed record FeatureLayerPlanItem(string TableName, string Name, bool IsSpatial);
