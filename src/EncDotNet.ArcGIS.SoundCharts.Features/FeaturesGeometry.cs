using ArcGIS.Core.Geometry;
using EncDotNet.SoundCharts.Features;

namespace EncDotNet.SoundCharts.PluginDatasource;

/// <summary>Builds Pro geometry, in WGS 84, from a <see cref="FeatureShape"/>.</summary>
internal static class FeaturesGeometry
{
    /// <summary>Builds the Pro geometry of <paramref name="shape"/>.</summary>
    /// <param name="shape">The shape.</param>
    public static Geometry ToGeometry(FeatureShape shape)
    {
        var sr = SpatialReferences.WGS84;
        switch (shape.Kind)
        {
            case FeatureGeometryKind.Point:
                var p = shape.Parts[0][0];
                return MapPointBuilderEx.CreateMapPoint(p.X, p.Y, sr);
            case FeatureGeometryKind.Multipoint:
                return MultipointBuilderEx.CreateMultipoint(shape.Parts[0].Select(v => MapPointBuilderEx.CreateMapPoint(v.X, v.Y, sr)), sr);
            case FeatureGeometryKind.Polyline:
                var line = new PolylineBuilderEx(sr);
                foreach (var part in shape.Parts)
                {
                    line.AddPart(part.Select(v => new Coordinate2D(v.X, v.Y)));
                }

                return line.ToGeometry();
            default:
                var polygon = new PolygonBuilderEx(sr);
                foreach (var part in shape.Parts)
                {
                    polygon.AddPart(part.Select(v => new Coordinate2D(v.X, v.Y)));
                }

                return polygon.ToGeometry();
        }
    }
}
