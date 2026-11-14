using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using EncDotNet.SoundCharts.ChartLayers;

namespace EncDotNet.SoundCharts.FeatureLayers;

/// <summary>
/// The provenance marker (D16) of a <b>SoundCharts features</b> group: the
/// CIM custom property <see cref="ChartLayerMarker.PropertyKey"/>, like the
/// chart group's, with the role <c>features</c>, so a renamed or moved
/// group is still known as SoundCharts and the dataset it shows.
/// </summary>
/// <remarks>
/// <see cref="ChartLayerMarker.TryParse"/> doesn't know the role and reads
/// it as no marker, so the chart layer code leaves these groups alone.
/// </remarks>
/// <param name="DatasetPath">The dataset's full path.</param>
/// <param name="Product">The dataset's product, such as <c>S-101</c>.</param>
public sealed record FeatureGroupMarker(string DatasetPath, string Product)
{
    /// <summary>The marker's role.</summary>
    public const string Role = "features";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Reads a marker, or returns <see langword="null"/> for anything else.</summary>
    /// <param name="json">The custom property's value.</param>
    public static FeatureGroupMarker? TryParse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var dto = JsonSerializer.Deserialize<MarkerDto>(json, Options);
            return dto is { Version: 1, Role: Role, Dataset: { Length: > 0 } path, Product: { Length: > 0 } product }
                ? new FeatureGroupMarker(path, product)
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Writes the marker as JSON.</summary>
    public string ToJson() => JsonSerializer.Serialize(
        new MarkerDto { Version = 1, Role = Role, Dataset = DatasetPath, Product = Product },
        Options);

    private sealed class MarkerDto
    {
        public int Version { get; set; }

        public string? Role { get; set; }

        public string? Dataset { get; set; }

        public string? Product { get; set; }
    }
}
