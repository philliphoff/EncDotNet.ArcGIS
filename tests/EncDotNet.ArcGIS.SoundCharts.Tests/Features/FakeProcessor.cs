using EncDotNet.S100.Core;
using EncDotNet.S100.DataModel;
using EncDotNet.S100.Datasets.Pipelines;
using EncDotNet.S100.Features;

namespace EncDotNet.SoundCharts.Tests.Features;

/// <summary>
/// A dataset processor over features made in the test, the way the
/// library's GML processors give them: attributes from
/// <see cref="GetFeatureInfoAt"/>, geometry from <see cref="GetFeatureGeometryAt"/>.
/// </summary>
internal sealed class FakeProcessor(string spec, params FakeFeature[] features) : IDatasetProcessor
{
    public SpecRef Spec { get; } = SpecRef.Parse($"{spec}/1.0.0");

    public FeatureInfo? GetFeatureInfo(string featureRef) =>
        features.Select((f, i) => (f, i)).Where(x => x.f.Ref == featureRef).Select(x => GetFeatureInfoAt(x.i)).FirstOrDefault();

    public FeatureInfo? GetFeatureInfoAt(int ordinal) => ordinal < 0 || ordinal >= features.Length ? null : new FeatureInfo
    {
        FeatureRef = features[ordinal].Ref,
        FeatureType = features[ordinal].Type,
        FeatureTypeName = features[ordinal].TypeName,
        Attributes = features[ordinal].Attributes,
        References = features[ordinal].References,
    };

    public S100FeatureGeometry? GetFeatureGeometryAt(int ordinal) => ordinal < 0 || ordinal >= features.Length ? null : features[ordinal].Geometry;

    public IEnumerable<FeatureSummary> EnumerateFeatures() => features.Select((f, i) => new FeatureSummary
    {
        FeatureRef = f.Ref,
        Ordinal = i,
        FeatureType = f.Type,
        FeatureTypeName = f.TypeName,
    });

    public static S100FeatureGeometry Point(double lon, double lat) =>
        new() { Primitive = S100GeometryType.Point, Points = [new GeoPosition(lat, lon)] };

    public static S100FeatureGeometry Points(params (double Lon, double Lat)[] points) =>
        new() { Primitive = S100GeometryType.Point, Points = [.. points.Select(p => new GeoPosition(p.Lat, p.Lon))] };

    public static S100FeatureGeometry Line(params (double Lon, double Lat)[] points) =>
        new() { Primitive = S100GeometryType.Curve, Curves = [[.. points.Select(p => new GeoPosition(p.Lat, p.Lon))]] };

    public static S100FeatureGeometry Box(double west, double south, double east, double north) => new()
    {
        Primitive = S100GeometryType.Surface,
        // Counter-clockwise, as GML writes exteriors.
        ExteriorRing =
        [
            new GeoPosition(south, west),
            new GeoPosition(south, east),
            new GeoPosition(north, east),
            new GeoPosition(north, west),
            new GeoPosition(south, west),
        ],
    };

    public static PickAttribute Simple(string code, string raw, string? display = null, string? name = null, DateTime? date = null) =>
        new() { Code = code, Name = name, RawValue = raw, DisplayValue = display, DateTimeValue = date };

    public static PickAttribute Complex(string code, string? name, params PickAttribute[] children) =>
        new() { Code = code, Name = name, RawValue = string.Empty, Children = children };
}

/// <summary>A feature of a <see cref="FakeProcessor"/>.</summary>
internal sealed record FakeFeature(
    string Ref,
    string Type,
    S100FeatureGeometry? Geometry,
    IReadOnlyList<PickAttribute> Attributes,
    string? TypeName = null,
    IReadOnlyList<FeatureReference>? References = null)
{
    public IReadOnlyList<FeatureReference> References { get; } = References ?? [];
}
