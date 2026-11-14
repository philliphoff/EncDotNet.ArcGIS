using System.Text.Json;

namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>
/// Reads the zoom range a chart server publishes in its TileJSON. Below
/// <c>minzoom</c> the server answers empty tiles, so the layer's minimum
/// scale is that zoom's scale (design brief D13).
/// </summary>
public static class TileJsonZoom
{
    /// <summary>
    /// Gets the <c>minzoom</c> of a TileJSON document, or <see langword="null"/>
    /// when it has none or isn't TileJSON. Never throws on bad input.
    /// </summary>
    /// <param name="json">The document.</param>
    public static int? TryReadMinZoom(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("minzoom", out var minZoom)
                && minZoom.ValueKind == JsonValueKind.Number
                && minZoom.TryGetDouble(out var value)
                && double.IsFinite(value)
                && value is >= 0 and <= 30
                    ? (int)Math.Floor(value)
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the minimum scale of a layer whose server's lowest zoom is
    /// <paramref name="minZoom"/>: that zoom's scale, or 0 (no limit) when
    /// it's unknown or 0.
    /// </summary>
    /// <param name="minZoom">The server's <c>minzoom</c>.</param>
    public static double MinScale(int? minZoom) =>
        minZoom is > 0 and <= 30 ? ChartLayerDocument.ScaleOfZoom(minZoom.Value) : 0;
}
