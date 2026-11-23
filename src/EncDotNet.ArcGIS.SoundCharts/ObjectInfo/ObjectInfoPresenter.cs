using System.Globalization;
using EncDotNet.SoundCharts.Resources;
using EncDotNet.SoundCharts.Themes;

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
        ObjectInfoState.Empty,
        Strings.ObjectInfo_EmptyTitle,
        Strings.ObjectInfo_EmptyDescription,
        Position: null,
        TimeNote: null,
        Items: [],
        Problems: []);

    /// <summary>The pane's content when a pick failed unexpectedly.</summary>
    public static ObjectInfoView Failed { get; } = new(
        ObjectInfoState.Failed,
        Strings.ObjectInfo_FailedTitle,
        Strings.ObjectInfo_FailedDescription,
        Position: null,
        TimeNote: null,
        Items: [],
        Problems: []);

    /// <summary>The pane's content when the map has no SoundCharts layers to look in.</summary>
    /// <param name="latitude">The clicked latitude.</param>
    /// <param name="longitude">The clicked longitude.</param>
    public static ObjectInfoView NoChartLayers(double latitude, double longitude) => new(
        ObjectInfoState.NoChartLayers,
        Strings.ObjectInfo_NoChartLayersTitle,
        Strings.ObjectInfo_NoChartLayersDescription,
        (latitude, longitude),
        TimeNote: null,
        Items: [],
        Problems: []);

    /// <summary>The pane's content while a pick runs.</summary>
    /// <param name="latitude">The clicked latitude.</param>
    /// <param name="longitude">The clicked longitude.</param>
    public static ObjectInfoView Working(double latitude, double longitude) => new(
        ObjectInfoState.Working,
        Strings.ObjectInfo_Working,
        Description: null,
        (latitude, longitude),
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
            var validity = dataset.InForce switch
            {
                true => Strings.ObjectInfo_InForce,
                false => Strings.ObjectInfo_NotInForce,
                null => null,
            };

            // A warning in force is a CHECK (warning tone, with a glyph and a
            // word); every other row is neutral.
            var tone = dataset.InForce is true ? SoundChartsTone.Warning : SoundChartsTone.Neutral;
            var isWarning = dataset.InForce is not null
                || string.Equals(dataset.Spec, "S-124", StringComparison.OrdinalIgnoreCase);
            foreach (var feature in dataset.Features)
            {
                var kind = feature.Kind == ObjectInfoFeatureKind.Coverage
                    ? ObjectInfoItemKind.Coverage
                    : isWarning ? ObjectInfoItemKind.NavigationalWarning : ObjectInfoItemKind.Feature;
                var caption = Format(Strings.ObjectInfo_IdFormat, feature.FeatureRef);
                items.Add(new ObjectInfoItem(
                    feature,
                    group,
                    dataset.Dataset.Name,
                    dataset.Spec,
                    kind,
                    tone,
                    dataset.Dataset.NotForNavigation,
                    validity,
                    caption,
                    Format(Strings.ObjectInfo_IdentityFormat, group, caption),
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
        // Navigational warnings' validity depends on it too (D33).
        var hasCoverage = report.Datasets.Any(d => d.Features.Any(f => f.Kind == ObjectInfoFeatureKind.Coverage));
        var hasValidity = report.Datasets.Any(d => d.InForce is not null);
        var timeNote = report.Time is { } time
            ? hasCoverage
                ? Format(Strings.ObjectInfo_TimeFormat, FormatUtc(time))
                : hasValidity ? Format(Strings.ObjectInfo_MapTimeFormat, FormatUtc(time)) : null
            : hasCoverage ? Strings.ObjectInfo_TimeDefault : null;

        return new ObjectInfoView(
            count == 0 ? ObjectInfoState.NothingHere : ObjectInfoState.Results,
            heading,
            description,
            (report.Latitude, report.Longitude),
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

    /// <summary>Formats a position as the pane shows it: <c>36.9500°, -76.0500°</c>.</summary>
    internal static string FormatLocation(double latitude, double longitude) =>
        Format(Strings.ObjectInfo_LocationFormat, latitude, longitude);

    /// <summary>
    /// Formats a position for the clipboard, as the viewer's <b>Copy
    /// location</b> does: signed decimal degrees, latitude first, to six
    /// places, in the invariant culture so it pastes the same everywhere
    /// (<c>36.950000, -76.050000</c>).
    /// </summary>
    internal static string FormatPositionText(double latitude, double longitude) =>
        string.Create(CultureInfo.InvariantCulture, $"{latitude:0.000000}, {longitude:0.000000}");

    private static string Format(string format, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, format, args);
}

/// <summary>What the Object information pane shows.</summary>
/// <param name="State">Which of the pane's states this is; each has its own look.</param>
/// <param name="Heading">
/// The state's title, such as "No feature picked", the busy caption, or for
/// <see cref="ObjectInfoState.Results"/> the feature count.
/// </param>
/// <param name="Description">A line under the title, or <c>null</c>.</param>
/// <param name="Position">The clicked position, WGS-84 degrees, or <c>null</c> before a click.</param>
/// <param name="TimeNote">Which time water levels and currents were sampled at, or <c>null</c> when there are none.</param>
/// <param name="Items">The features and coverage values, grouped by <see cref="ObjectInfoItem.Group"/>.</param>
/// <param name="Problems">One line per dataset that couldn't be read.</param>
internal sealed record ObjectInfoView(
    ObjectInfoState State,
    string Heading,
    string? Description,
    (double Latitude, double Longitude)? Position,
    string? TimeNote,
    IReadOnlyList<ObjectInfoItem> Items,
    IReadOnlyList<string> Problems)
{
    /// <summary>Gets the clicked position as the pane shows it, or <c>null</c>.</summary>
    public string? Location => Position is { } p ? ObjectInfoPresenter.FormatLocation(p.Latitude, p.Longitude) : null;

    /// <summary>Gets the clicked position as <b>Copy position</b> puts it on the clipboard, or <c>null</c>.</summary>
    public string? PositionText => Position is { } p ? ObjectInfoPresenter.FormatPositionText(p.Latitude, p.Longitude) : null;

    /// <summary>
    /// Gets the overline above the position: the feature count with results,
    /// otherwise "OBJECT INFORMATION" once there is a position; <c>null</c>
    /// before a click.
    /// </summary>
    public string? Overline => State == ObjectInfoState.Results
        ? Heading
        : Position is null ? null : Strings.ObjectInfo_Overline;
}

/// <summary>One feature or coverage value in the pane's list.</summary>
/// <param name="Feature">The feature.</param>
/// <param name="Group">The dataset heading the item is listed under, which the list groups by.</param>
/// <param name="DatasetName">The dataset's name, shown in mono in the group header.</param>
/// <param name="Spec">The dataset's product specification, such as <c>S-101</c>: the group header's product tag.</param>
/// <param name="Kind">Which glyph the row shows.</param>
/// <param name="Tone">The row glyph's and validity chip's tone.</param>
/// <param name="NotForNavigation">Whether the dataset's provider marks it not for navigation.</param>
/// <param name="Validity">
/// For a navigational warning, "In force at map time" or "Not in force at
/// map time" (D33); <c>null</c> otherwise.
/// </param>
/// <param name="Caption">The line under the title, its id.</param>
/// <param name="Identity">The details' mono line under the title: the dataset heading and the id.</param>
/// <param name="Source">The dataset's path, as the viewer's "Source:" line.</param>
/// <param name="Attributes">The attribute rows.</param>
/// <param name="References">One line per association.</param>
internal sealed record ObjectInfoItem(
    ObjectInfoFeature Feature,
    string Group,
    string DatasetName,
    string Spec,
    ObjectInfoItemKind Kind,
    SoundChartsTone Tone,
    bool NotForNavigation,
    string? Validity,
    string Caption,
    string Identity,
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
