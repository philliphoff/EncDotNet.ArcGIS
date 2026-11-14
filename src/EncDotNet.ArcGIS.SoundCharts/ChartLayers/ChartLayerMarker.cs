using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using EncDotNet.SoundCharts.Charts;
using EncDotNet.SoundCharts.ObjectInfo;

namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>
/// The add-in's provenance marker on a SoundCharts layer (design brief §3.4,
/// D16): a CIM custom property, <see cref="PropertyKey"/>, holding what the
/// layer is and which datasets it draws.
/// </summary>
/// <remarks>
/// <para>
/// The marker, not the layer's name or URL, is what makes a layer a
/// SoundCharts layer. It lives in the layer's own CIM definition, so it's
/// saved with the project, survives renaming, dragging out of the group, and
/// copying to another map or project (open question P3: yes, CIM layers carry
/// custom properties). The add-in rebuilds its servers and registry from the
/// markers when a project opens.
/// </para>
/// <para>
/// A group marker (<see cref="IsGroup"/>) names the inputs the user added,
/// for the group's name; a layer marker has a <see cref="Kind"/> and the
/// <see cref="Datasets"/> its server draws.
/// </para>
/// </remarks>
public sealed record ChartLayerMarker
{
    /// <summary>The CIM custom property key that holds the marker.</summary>
    public const string PropertyKey = "SoundCharts";

    /// <summary>The marker format's version; markers of a later version are ignored.</summary>
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private ChartLayerMarker(bool isGroup, ChartLayerKind? kind, IReadOnlyList<ChartLayerDataset> datasets, IReadOnlyList<string> inputs)
    {
        IsGroup = isGroup;
        Kind = kind;
        Datasets = datasets;
        Inputs = inputs;
    }

    /// <summary>Gets whether this marks the <b>SoundCharts · {source}</b> group rather than a chart layer.</summary>
    public bool IsGroup { get; }

    /// <summary>Gets the chart layer's kind, or <see langword="null"/> for the group.</summary>
    public ChartLayerKind? Kind { get; }

    /// <summary>Gets the datasets the layer draws, in canonical order; empty for the group.</summary>
    public IReadOnlyList<ChartLayerDataset> Datasets { get; }

    /// <summary>Gets the names of the inputs added to the group (cells, exchange sets, folders); empty for a layer.</summary>
    public IReadOnlyList<string> Inputs { get; }

    /// <summary>Creates the marker of a chart layer.</summary>
    /// <param name="kind">The layer's kind.</param>
    /// <param name="datasets">The datasets it draws; at least one.</param>
    public static ChartLayerMarker ForLayer(ChartLayerKind kind, IEnumerable<ChartLayerDataset> datasets)
    {
        ArgumentNullException.ThrowIfNull(datasets);
        var list = datasets
            .DistinctBy(d => d.Path, StringComparer.OrdinalIgnoreCase)
            .OrderBy(d => d.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (list.Count == 0)
        {
            throw new ArgumentException("A chart layer needs at least one dataset.", nameof(datasets));
        }

        return new ChartLayerMarker(false, kind, list, []);
    }

    /// <summary>Creates the marker of the group.</summary>
    /// <param name="inputs">The names of the inputs added to it.</param>
    public static ChartLayerMarker ForGroup(IEnumerable<string> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        return new ChartLayerMarker(true, null, [], [.. inputs.Distinct(StringComparer.OrdinalIgnoreCase)]);
    }

    /// <summary>
    /// Reads a marker from its JSON, or returns <see langword="null"/> when
    /// the text isn't a marker this version understands. Never throws.
    /// </summary>
    /// <param name="json">The custom property's value.</param>
    public static ChartLayerMarker? TryParse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var dto = JsonSerializer.Deserialize<MarkerDto>(json, Options);
            if (dto is null || dto.Version is < 1 or > CurrentVersion)
            {
                return null;
            }

            if (dto.Role == "group")
            {
                return ForGroup(dto.Inputs ?? []);
            }

            if (dto.Role != "layer" || ChartLayerKinds.FromId(dto.Kind) is not { } kind)
            {
                return null;
            }

            var datasets = (dto.Datasets ?? [])
                .Where(d => !string.IsNullOrWhiteSpace(d.Path) && !string.IsNullOrWhiteSpace(d.Product))
                .Select(d => new ChartLayerDataset(
                    d.Path!,
                    d.Product!,
                    d.Edition,
                    d.Extent is { Length: 4 } e ? new GeoExtent(e[0], e[1], e[2], e[3]) : null,
                    d.Producer,
                    d.NotForNavigation))
                .ToList();
            return datasets.Count == 0 ? null : ForLayer(kind, datasets);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Gets the marker as compact JSON, the custom property's value.</summary>
    public string ToJson()
    {
        var dto = new MarkerDto
        {
            Version = CurrentVersion,
            Role = IsGroup ? "group" : "layer",
            Kind = Kind is { } kind ? ChartLayerKinds.Id(kind) : null,
            Inputs = IsGroup ? [.. Inputs] : null,
            Datasets = IsGroup
                ? null
                : [.. Datasets.Select(d => new DatasetDto
                {
                    Path = d.Path,
                    Product = d.Product,
                    Edition = d.Edition,
                    Producer = d.Producer,
                    NotForNavigation = d.NotForNavigation,
                    Extent = d.Extent is { } e ? [e.West, e.South, e.East, e.North] : null,
                })],
        };
        return JsonSerializer.Serialize(dto, Options);
    }

    /// <summary>
    /// Gets the chart server's dataset set for this layer (A2): its datasets
    /// as dataset files.
    /// </summary>
    /// <param name="options">The start-up options, or <see langword="null"/> for the defaults.</param>
    /// <exception cref="InvalidOperationException">This is the group's marker.</exception>
    public DatasetSet ToDatasetSet(ChartRenderOptions? options = null)
    {
        if (IsGroup)
        {
            throw new InvalidOperationException("The group has no datasets of its own.");
        }

        return new DatasetSet(Datasets.Select(d => DatasetSource.Dataset(d.Path)), options);
    }

    /// <summary>Gets the datasets as Object information needs them.</summary>
    public IReadOnlyList<ChartDataset> ToChartDatasets() =>
        [.. Datasets.Select(d => new ChartDataset(d.Path, d.Name, d.NotForNavigation))];

    /// <summary>Gets whether <paramref name="other"/> marks the same thing with the same datasets.</summary>
    /// <param name="other">The other marker.</param>
    public bool Equals(ChartLayerMarker? other) =>
        other is not null && string.Equals(ToJson(), other.ToJson(), StringComparison.Ordinal);

    /// <inheritdoc/>
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToJson());

    private sealed class MarkerDto
    {
        public int Version { get; set; }

        public string? Role { get; set; }

        public string? Kind { get; set; }

        public List<string>? Inputs { get; set; }

        public List<DatasetDto>? Datasets { get; set; }
    }

    private sealed class DatasetDto
    {
        public string? Path { get; set; }

        public string? Product { get; set; }

        public string? Edition { get; set; }

        public string? Producer { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool NotForNavigation { get; set; }

        public double[]? Extent { get; set; }
    }
}
