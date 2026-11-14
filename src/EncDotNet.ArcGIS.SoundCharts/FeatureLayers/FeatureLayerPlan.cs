using System.Globalization;
using EncDotNet.SoundCharts.Features;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts.FeatureLayers;

/// <summary>
/// What <b>Add as Feature Layers</b> puts in the map for one dataset
/// (design brief §3.2, D14): a <b>SoundCharts features · {dataset}</b>
/// group, outside the chart group, with a layer named
/// <b>{feature type} · {dataset}</b> per feature class and a standalone
/// table per table.
/// </summary>
/// <param name="GroupName">The group layer's name.</param>
/// <param name="Members">The layers and tables, in the order to add them: each is added on top, so areas end up at the bottom and points on top.</param>
public sealed record FeatureLayerPlan(string GroupName, IReadOnlyList<FeatureLayerPlanItem> Members)
{
    /// <summary>The products whose datasets open as feature layers: those the library gives features for.</summary>
    public static readonly IReadOnlySet<string> Products = new HashSet<string>(StringComparer.Ordinal) { "S-101", "S-57", "S-124", "S-421" };

    // Drawn bottom to top.
    private static readonly FeatureGeometryKind[] DrawingOrder =
    [
        FeatureGeometryKind.Polygon,
        FeatureGeometryKind.Polyline,
        FeatureGeometryKind.Multipoint,
        FeatureGeometryKind.Point,
    ];

    /// <summary>Plans the layers of <paramref name="tables"/>.</summary>
    /// <param name="tables">The dataset's tables.</param>
    public static FeatureLayerPlan For(DatasetTables tables)
    {
        ArgumentNullException.ThrowIfNull(tables);
        var dataset = tables.Name;
        var split = tables.Tables
            .Select(t => t.FeatureType)
            .OfType<string>()
            .GroupBy(t => t, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.Ordinal);

        var members = new List<FeatureLayerPlanItem>();
        foreach (var kind in DrawingOrder)
        {
            // Within a kind, reverse alphabetical, so Contents lists them A to Z.
            foreach (var table in tables.Tables.Where(t => t.Kind == kind).OrderByDescending(t => t.DisplayName, StringComparer.CurrentCultureIgnoreCase))
            {
                members.Add(new FeatureLayerPlanItem(table.Name, LayerName(table, split, dataset), IsSpatial: true));
            }
        }

        foreach (var table in tables.Tables.Where(t => !t.IsSpatial).OrderBy(t => t.FeatureType is null).ThenBy(t => t.DisplayName, StringComparer.CurrentCultureIgnoreCase))
        {
            members.Add(new FeatureLayerPlanItem(table.Name, LayerName(table, split, dataset), IsSpatial: false));
        }

        return new FeatureLayerPlan(GroupNameOf(dataset), members);
    }

    /// <summary>Gets the group name for <paramref name="dataset"/>.</summary>
    /// <param name="dataset">The dataset's name.</param>
    public static string GroupNameOf(string dataset) =>
        string.Format(CultureInfo.CurrentCulture, Strings.Features_GroupName, dataset);

    private static string LayerName(FeatureTable table, HashSet<string> split, string dataset)
    {
        if (table.FeatureType is null)
        {
            return string.Format(CultureInfo.CurrentCulture, Strings.Features_LayerName, Strings.Features_Associations, dataset);
        }

        return split.Contains(table.FeatureType)
            ? string.Format(CultureInfo.CurrentCulture, Strings.Features_SplitLayerName, table.DisplayName, GeometryWord(table.Kind), dataset)
            : string.Format(CultureInfo.CurrentCulture, Strings.Features_LayerName, table.DisplayName, dataset);
    }

    private static string GeometryWord(FeatureGeometryKind kind) => kind switch
    {
        FeatureGeometryKind.Point => Strings.Features_Point,
        FeatureGeometryKind.Multipoint => Strings.Features_Multipoint,
        FeatureGeometryKind.Polyline => Strings.Features_Line,
        FeatureGeometryKind.Polygon => Strings.Features_Area,
        _ => Strings.Features_Table,
    };
}
