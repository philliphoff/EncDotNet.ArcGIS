namespace EncDotNet.SoundCharts.ObjectInfo;

/// <summary>
/// What Object information found at a point: every feature and coverage
/// value, grouped by dataset (design brief D6).
/// </summary>
/// <param name="Latitude">The point's latitude, WGS-84 degrees.</param>
/// <param name="Longitude">The point's longitude, WGS-84 degrees.</param>
/// <param name="Time">
/// The instant (UTC) time-varying coverages were sampled at; <c>null</c> when
/// each dataset's default step was used (its first time step).
/// </param>
/// <param name="Datasets">
/// The datasets with something at the point, in the order they were asked
/// about. A dataset with nothing at the point isn't listed.
/// </param>
/// <param name="Problems">Datasets that couldn't be read, with the reason.</param>
public sealed record ObjectInfoReport(
    double Latitude,
    double Longitude,
    DateTime? Time,
    IReadOnlyList<ObjectInfoDatasetResult> Datasets,
    IReadOnlyList<ObjectInfoProblem> Problems)
{
    /// <summary>Gets the number of features and coverage values in the report.</summary>
    public int FeatureCount => Datasets.Sum(d => d.Features.Count);
}

/// <summary>The features and coverage values one dataset has at the point.</summary>
/// <param name="Dataset">The dataset.</param>
/// <param name="Spec">The product specification the dataset declares, such as <c>S-101</c>.</param>
/// <param name="Features">
/// The dataset's features at the point, most specific first: points, then
/// curves, then areas, nearer before farther; coverage values last.
/// </param>
/// <param name="InForce">
/// For an S-124 dataset picked at a map time, whether its warning is in
/// force then (D33); <see langword="null"/> otherwise.
/// </param>
public sealed record ObjectInfoDatasetResult(
    ChartDataset Dataset,
    string Spec,
    IReadOnlyList<ObjectInfoFeature> Features,
    bool? InForce = null);

/// <summary>A dataset Object information couldn't read.</summary>
/// <param name="Dataset">The dataset.</param>
/// <param name="Message">Why, as the library reported it.</param>
public sealed record ObjectInfoProblem(ChartDataset Dataset, string Message);
