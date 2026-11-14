using System.Globalization;
using System.Text.Json;

namespace EncDotNet.SoundCharts.Export;

/// <summary>
/// Turns the instant a time-varying layer draws into the time step
/// <c>s100 tiles export</c> takes.
/// </summary>
/// <remarks>
/// <c>tiles serve</c> draws each dataset at its step nearest the WMTS
/// <c>Time</c>; <c>tiles export</c> takes a step index instead
/// (<c>--time-step</c>). For a layer of one time series, or of several on the
/// same steps, the index of the nearest step is the same picture.
/// </remarks>
public static class ExportTime
{
    /// <summary>
    /// Reads a WMTS <c>Time</c> dimension value (ISO 8601, UTC), or
    /// <see langword="null"/> when it isn't one.
    /// </summary>
    /// <param name="value">The dimension value.</param>
    public static DateTime? TryParse(object? value) => value switch
    {
        DateTime time => ToUtc(time),
        DateTimeOffset offset => offset.UtcDateTime,
        string text when DateTime.TryParse(
            text,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out var parsed) => DateTime.SpecifyKind(parsed, DateTimeKind.Utc),
        _ => null,
    };

    /// <summary>
    /// Reads a chart server's time steps from its TileJSON (<c>s100.times</c>),
    /// in order; empty when it has none or isn't TileJSON. Never throws on
    /// bad input.
    /// </summary>
    /// <param name="json">The TileJSON document.</param>
    public static IReadOnlyList<DateTime> ReadTileJsonSteps(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("s100", out var s100)
                || s100.ValueKind != JsonValueKind.Object
                || !s100.TryGetProperty("times", out var times)
                || times.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return times.EnumerateArray()
                .Select(t => t.ValueKind == JsonValueKind.String ? TryParse(t.GetString()) : null)
                .OfType<DateTime>()
                .Distinct()
                .Order()
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <summary>
    /// Gets the index of the step nearest <paramref name="instant"/>, or
    /// <see langword="null"/> when there are no steps or no instant.
    /// </summary>
    /// <param name="steps">The layer's time steps, in order.</param>
    /// <param name="instant">The instant the layer draws.</param>
    public static int? NearestStep(IReadOnlyList<DateTime> steps, DateTime? instant)
    {
        ArgumentNullException.ThrowIfNull(steps);
        if (steps.Count == 0 || instant is not { } time)
        {
            return null;
        }

        var target = ToUtc(time);
        var best = 0;
        var bestDistance = TimeSpan.MaxValue;
        for (var i = 0; i < steps.Count; i++)
        {
            var distance = (ToUtc(steps[i]) - target).Duration();
            if (distance < bestDistance)
            {
                best = i;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static DateTime ToUtc(DateTime time) => time.Kind switch
    {
        DateTimeKind.Utc => time,
        DateTimeKind.Local => time.ToUniversalTime(),
        _ => DateTime.SpecifyKind(time, DateTimeKind.Utc),
    };
}
