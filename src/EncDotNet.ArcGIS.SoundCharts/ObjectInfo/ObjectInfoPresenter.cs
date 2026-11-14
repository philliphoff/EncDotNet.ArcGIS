using System.Globalization;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts.ObjectInfo;

/// <summary>
/// Turns an <see cref="ObjectInfoReport"/> into what the
/// <b>SoundCharts Object information</b> pane shows, in the viewer's words.
/// It doesn't use the Pro API or WPF, so it is tested without Pro.
/// </summary>
internal static class ObjectInfoPresenter
{
    /// <summary>The pane's content before the first click.</summary>
    public static ObjectInfoView Empty { get; } = new(
        Strings.ObjectInfo_EmptyTitle,
        Strings.ObjectInfo_EmptyDescription,
        Location: null,
        TimeNote: null,
        Items: [],
        Problems: []);

    /// <summary>The pane's content when the map has no SoundCharts layers to look in.</summary>
    /// <param name="latitude">The clicked latitude.</param>
    /// <param name="longitude">The clicked longitude.</param>
    public static ObjectInfoView NoChartLayers(double latitude, double longitude) => new(
        Strings.ObjectInfo_NoChartLayersTitle,
        Strings.ObjectInfo_NoChartLayersDescription,
        Location(latitude, longitude),
        TimeNote: null,
        Items: [],
        Problems: []);

    /// <summary>The pane's content while a pick runs.</summary>
    /// <param name="latitude">The clicked latitude.</param>
    /// <param name="longitude">The clicked longitude.</param>
    public static ObjectInfoView Working(double latitude, double longitude) => new(
        Strings.ObjectInfo_Working,
        Description: null,
        Location(latitude, longitude),
        TimeNote: null,
        Items: [],
        Problems: []);

    /// <summary>The pane's content when a pick failed unexpectedly.</summary>
    public static ObjectInfoView Failed { get; } = new(
        Strings.ObjectInfo_FailedTitle,
        Strings.ObjectInfo_FailedDescription,
        Location: null,
        TimeNote: null,
        Items: [],
        Problems: []);

    /// <summary>Presents <paramref name="report"/>.</summary>
    /// <param name="report">The pick's result.</param>
    /// <returns>The pane's content.</returns>
    public static ObjectInfoView Present(ObjectInfoReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var items = new List<ObjectInfoItem>();
        foreach (var dataset in report.Datasets)
        {
            var group = Format(Strings.ObjectInfo_DatasetFormat, dataset.Dataset.Name, dataset.Spec);
            foreach (var feature in dataset.Features)
            {
                items.Add(new ObjectInfoItem(
                    feature,
                    group,
                    dataset.Dataset.NotForNavigation,
                    Format(Strings.ObjectInfo_IdFormat, feature.FeatureRef),
                    Format(Strings.ObjectInfo_SourceFormat, dataset.Dataset.Path),
                    [.. feature.Attributes.Select(a => new ObjectInfoAttributeRow(
                        a.Name,
                        a.Value,
                        Format(Strings.ObjectInfo_AttributeTooltipFormat, a.Code, a.RawValue),
                        a.Level,
                        a.ExternalText))],
                    [.. feature.References.Select(r => Format(Strings.ObjectInfo_ReferenceFormat, r.Role, r.Target))]));
            }
        }

        var count = report.FeatureCount;
        var heading = count switch
        {
            0 => Strings.ObjectInfo_NothingHereTitle,
            1 => Strings.ObjectInfo_OneFeature,
            _ => Format(Strings.ObjectInfo_FeatureCountFormat, count),
        };
        var description = count == 0 ? Strings.ObjectInfo_NothingHereDescription : null;

        // Only time-varying coverages depend on the time; say which was used
        // whenever the report has a coverage value.
        var hasCoverage = report.Datasets.Any(d => d.Features.Any(f => f.Kind == ObjectInfoFeatureKind.Coverage));
        var timeNote = !hasCoverage
            ? null
            : report.Time is { } time
                ? Format(Strings.ObjectInfo_TimeFormat, FormatUtc(time))
                : Strings.ObjectInfo_TimeDefault;

        return new ObjectInfoView(
            heading,
            description,
            Location(report.Latitude, report.Longitude),
            timeNote,
            items,
            [.. report.Problems.Select(p => Format(Strings.ObjectInfo_ProblemFormat, p.Dataset.Name, p.Message))]);
    }

    /// <summary>
    /// Formats a UTC time the way the design brief's <b>Time format</b> says:
    /// <c>12 Nov 2026 14:00 UTC</c>.
    /// </summary>
    internal static string FormatUtc(DateTime time) =>
        Format(Strings.ObjectInfo_UtcFormat, DateTime.SpecifyKind(time, DateTimeKind.Utc));

    private static string Location(double latitude, double longitude) =>
        Format(Strings.ObjectInfo_LocationFormat, latitude, longitude);

    private static string Format(string format, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, format, args);
}

/// <summary>What the Object information pane shows.</summary>
/// <param name="Heading">The heading: the feature count, or a state such as "No feature picked".</param>
/// <param name="Description">A line under the heading, or <c>null</c>.</param>
/// <param name="Location">The clicked position, or <c>null</c> before a click.</param>
/// <param name="TimeNote">Which time water levels and currents were sampled at, or <c>null</c> when there are none.</param>
/// <param name="Items">The features and coverage values, grouped by <see cref="ObjectInfoItem.Group"/>.</param>
/// <param name="Problems">One line per dataset that couldn't be read.</param>
internal sealed record ObjectInfoView(
    string Heading,
    string? Description,
    string? Location,
    string? TimeNote,
    IReadOnlyList<ObjectInfoItem> Items,
    IReadOnlyList<string> Problems);

/// <summary>One feature or coverage value in the pane's list.</summary>
/// <param name="Feature">The feature.</param>
/// <param name="Group">The dataset heading the item is listed under.</param>
/// <param name="NotForNavigation">Whether the dataset's provider marks it not for navigation.</param>
/// <param name="Caption">The line under the title, its id.</param>
/// <param name="Source">The dataset's path, as the viewer's "Source:" line.</param>
/// <param name="Attributes">The attribute rows.</param>
/// <param name="References">One line per association.</param>
internal sealed record ObjectInfoItem(
    ObjectInfoFeature Feature,
    string Group,
    bool NotForNavigation,
    string Caption,
    string Source,
    IReadOnlyList<ObjectInfoAttributeRow> Attributes,
    IReadOnlyList<string> References)
{
    /// <summary>Gets the item's title, the feature type name.</summary>
    public string Title => Feature.Title;
}

/// <summary>One attribute row in the pane.</summary>
/// <param name="Name">The attribute's name.</param>
/// <param name="Value">The value to show.</param>
/// <param name="ToolTip">The code and value as encoded.</param>
/// <param name="Level">How far the row is indented.</param>
/// <param name="Text">The text of a file the attribute refers to, or <c>null</c>.</param>
internal sealed record ObjectInfoAttributeRow(string Name, string Value, string ToolTip, int Level, string? Text)
{
    /// <summary>
    /// Gets the row's indent as a WPF thickness string (<c>"12,0,0,0"</c>):
    /// 12 device-independent pixels a level. WPF converts it when binding.
    /// </summary>
    public string Indent => string.Create(CultureInfo.InvariantCulture, $"{Level * 12},0,0,0");
}
