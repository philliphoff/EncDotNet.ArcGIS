using System.Text.Json;

namespace EncDotNet.SoundCharts.MapTime;

/// <summary>
/// How a chart server's time-varying datasets pick what they draw, from its
/// <c>/datasets</c> document (issue #64): each dataset's
/// <c>timeSelection</c>, and the union of their <c>timeRange</c>s.
/// </summary>
/// <param name="Selections">Each time-varying dataset's selection.</param>
/// <param name="Span">
/// The union (UTC) of the datasets' <c>timeRange</c>s, the span in which
/// they draw something tied to a time; <see langword="null"/> when none
/// gives one.
/// </param>
public sealed record DatasetTimes(IReadOnlyList<TimeSelection> Selections, (DateTime Start, DateTime End)? Span)
{
    /// <summary>
    /// Reads a chart server's <c>/datasets</c> document; <see langword="null"/>
    /// when it isn't valid JSON or describes no time-varying dataset (a server
    /// before issue #64 gives no <c>timeSelection</c>).
    /// </summary>
    /// <param name="json">The document.</param>
    public static DatasetTimes? TryRead(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("datasets", out var datasets)
                || datasets.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var selections = new List<TimeSelection>();
            DateTime? start = null;
            DateTime? end = null;
            foreach (var dataset in datasets.EnumerateArray())
            {
                if (ReadSelection(dataset) is not { } selection)
                {
                    continue;
                }

                selections.Add(selection);
                if (dataset.TryGetProperty("timeRange", out var range)
                    && range is { ValueKind: JsonValueKind.Array }
                    && range.GetArrayLength() == 2
                    && range[0].ValueKind == JsonValueKind.String
                    && range[1].ValueKind == JsonValueKind.String
                    && TimeSteps.TryParse(range[0].GetString(), out var from)
                    && TimeSteps.TryParse(range[1].GetString(), out var to)
                    && from <= to)
                {
                    start = start is { } s && s <= from ? s : from;
                    end = end is { } e && e >= to ? e : to;
                }
            }

            return selections.Count == 0
                ? null
                : new DatasetTimes(selections, start is { } first && end is { } last ? (first, last) : null);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Reads a dataset entry's <c>timeSelection</c>, or <see langword="null"/>.</summary>
    /// <param name="dataset">An entry of <c>/datasets</c> or of TileJSON's <c>s100.datasets</c>.</param>
    internal static TimeSelection? ReadSelection(JsonElement dataset) =>
        dataset.ValueKind == JsonValueKind.Object
        && dataset.TryGetProperty("timeSelection", out var selection)
        && selection.ValueKind == JsonValueKind.String
            ? TimeSelections.TryParse(selection.GetString())
            : null;
}
