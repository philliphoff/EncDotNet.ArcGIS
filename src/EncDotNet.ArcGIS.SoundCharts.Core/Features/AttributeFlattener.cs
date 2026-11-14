using System.Globalization;
using EncDotNet.S100.Datasets.Pipelines;

namespace EncDotNet.SoundCharts.Features;

/// <summary>
/// Flattens a feature's attribute tree, as EncDotNet.S100's
/// <see cref="FeatureInfo"/> gives it, into one value per column.
/// </summary>
/// <remarks>
/// <para>
/// A simple attribute is a column named by its code. A complex attribute's
/// sub-attributes, and an associated information type's attributes (which
/// the library lists as an attribute with children), are columns named by
/// the path of codes joined by <c>_</c>: <c>featureName_name</c>,
/// <c>NauticalInformation_information_text</c>. That keeps information
/// associations reachable from the feature's own row.
/// </para>
/// <para>
/// Repeated values of one column (a multi-valued attribute, two instances
/// of a complex attribute) are joined with <see cref="ValueSeparator"/> in
/// the dataset's order. The value is the library's display text (an
/// enumeration's label, say), except that a number shown with its unit
/// (<c>12.5 m</c>) keeps the bare number, so the column can be numeric.
/// </para>
/// </remarks>
public static class AttributeFlattener
{
    /// <summary>Joins repeated values of one column.</summary>
    public const string ValueSeparator = "; ";

    /// <summary>Joins the display names of a path into a field alias.</summary>
    public const string AliasSeparator = ": ";

    // Pick-time notices the library adds to a pick report; they aren't data.
    private static readonly HashSet<string> SkippedCodes = new(StringComparer.Ordinal) { "notice", "stale" };

    /// <summary>Flattens <paramref name="attributes"/> into columns, in first-seen order.</summary>
    /// <param name="attributes">A feature's attributes.</param>
    public static IReadOnlyList<FlatAttribute> Flatten(IReadOnlyList<PickAttribute> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        var columns = new Dictionary<string, Accumulator>(StringComparer.Ordinal);
        var order = new List<string>();
        Walk(attributes, null, null, columns, order);
        return [.. order.Select(path => columns[path].ToFlat(path))];
    }

    private static void Walk(
        IReadOnlyList<PickAttribute> attributes,
        string? path,
        string? alias,
        Dictionary<string, Accumulator> columns,
        List<string> order)
    {
        foreach (var attribute in attributes)
        {
            if (path is null && SkippedCodes.Contains(attribute.Code))
            {
                continue;
            }

            var childPath = path is null ? attribute.Code : $"{path}_{attribute.Code}";
            var childAlias = alias is null ? attribute.DisplayName : $"{alias}{AliasSeparator}{attribute.DisplayName}";
            if (attribute.Children.Count > 0)
            {
                Walk(attribute.Children, childPath, childAlias, columns, order);
                continue;
            }

            if (!columns.TryGetValue(childPath, out var column))
            {
                column = new Accumulator(childAlias);
                columns.Add(childPath, column);
                order.Add(childPath);
            }

            column.Add(ValueOf(attribute), attribute.DateTimeValue);
        }
    }

    /// <summary>Gets the text of one leaf attribute, as described in the remarks of <see cref="AttributeFlattener"/>.</summary>
    /// <param name="attribute">The attribute.</param>
    public static string ValueOf(PickAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        var display = attribute.DisplayText;
        var raw = attribute.RawValue;
        if (!string.Equals(display, raw, StringComparison.Ordinal)
            && raw.Length > 0
            && display.StartsWith(raw, StringComparison.Ordinal)
            && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            return raw;
        }

        return display;
    }

    private sealed class Accumulator(string alias)
    {
        private readonly List<string> _texts = [];
        private DateTime? _date;
        private bool _allDates = true;

        public void Add(string text, DateTime? date)
        {
            if (text.Length == 0)
            {
                return;
            }

            _texts.Add(text);
            _allDates &= date is not null;
            _date = date;
        }

        public FlatAttribute ToFlat(string path) => new(
            path,
            alias,
            _texts.Count == 0 ? null : string.Join(ValueSeparator, _texts),
            _texts.Count == 1 && _allDates ? _date : null,
            _texts.Count > 1);
    }
}
