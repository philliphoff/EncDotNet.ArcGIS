using System.Text.Json;
using System.Text.Json.Nodes;

namespace EncDotNet.SoundCharts.Settings;

/// <summary>
/// Reads and writes <see cref="MarinerSettings"/> and
/// <see cref="ApplicationSettings"/> as JSON: in the project (Pro module
/// settings, saved with the <c>.aprx</c>) and in the user's settings file.
/// </summary>
/// <remarks>
/// The JSON carries a <c>version</c>. Reading is forgiving, because a project
/// can come from another add-in version: a missing, unknown or invalid
/// value takes its default, and unknown keys are ignored. Numbers are
/// written in invariant form and enums by name.
/// </remarks>
public static class SettingsJson
{
    /// <summary>The settings schema this add-in writes.</summary>
    public const int Version = 1;

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    /// <summary>Gets <paramref name="settings"/> as JSON.</summary>
    /// <param name="settings">The settings.</param>
    public static string ToJson(MarinerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var node = MarinerNode(settings);
        node.Insert(0, "version", Version);
        return node.ToJsonString();
    }

    /// <summary>Gets <paramref name="settings"/> as indented JSON, for the user's settings file.</summary>
    /// <param name="settings">The settings.</param>
    public static string ToJson(ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var node = new JsonObject
        {
            ["version"] = Version,
            ["projectDefaults"] = MarinerNode(settings.ProjectDefaults),
            ["followProTheme"] = settings.FollowProTheme,
            ["timeFormat"] = settings.TimeFormat.ToString(),
            ["depthUnit"] = settings.DepthUnit.ToString(),
            ["serverPath"] = settings.ServerPath,
            ["tileCacheMegabytes"] = settings.TileCacheMegabytes,
        };
        return node.ToJsonString(Indented);
    }

    /// <summary>
    /// Reads mariner settings, or <see langword="null"/> when
    /// <paramref name="json"/> isn't a JSON object.
    /// </summary>
    /// <param name="json">The JSON, or <see langword="null"/>.</param>
    /// <param name="defaults">The values for anything missing or invalid; <see cref="MarinerSettings.Default"/> when <see langword="null"/>.</param>
    public static MarinerSettings? ReadMariner(string? json, MarinerSettings? defaults = null) =>
        Parse(json) is { } node ? ReadMariner(node, defaults ?? MarinerSettings.Default) : null;

    /// <summary>
    /// Reads application settings, or <see langword="null"/> when
    /// <paramref name="json"/> isn't a JSON object.
    /// </summary>
    /// <param name="json">The JSON, or <see langword="null"/>.</param>
    public static ApplicationSettings? ReadApplication(string? json)
    {
        if (Parse(json) is not { } node)
        {
            return null;
        }

        var defaults = ApplicationSettings.Default;
        var cache = Integer(node, "tileCacheMegabytes") ?? defaults.TileCacheMegabytes;
        return new ApplicationSettings
        {
            ProjectDefaults = node["projectDefaults"] is JsonObject project
                ? ReadMariner(project, MarinerSettings.Default)
                : defaults.ProjectDefaults,
            FollowProTheme = Boolean(node, "followProTheme") ?? defaults.FollowProTheme,
            TimeFormat = Enumeration(node, "timeFormat", defaults.TimeFormat),
            DepthUnit = Enumeration(node, "depthUnit", defaults.DepthUnit),
            ServerPath = Text(node, "serverPath") is { Length: > 0 } path ? path : null,
            TileCacheMegabytes = cache is >= ApplicationSettings.MinTileCacheMegabytes and <= ApplicationSettings.MaxTileCacheMegabytes
                ? cache
                : defaults.TileCacheMegabytes,
        };
    }

    private static JsonObject MarinerNode(MarinerSettings settings) => new()
    {
        ["palette"] = settings.Palette.ToString(),
        ["displayCategory"] = settings.DisplayCategory.ToString(),
        ["safetyContour"] = settings.SafetyContour,
        ["safetyDepth"] = settings.SafetyDepth,
        ["shallowContour"] = settings.ShallowContour,
        ["deepContour"] = settings.DeepContour,
        ["fourShades"] = settings.FourShades,
        ["shallowWaterDangers"] = settings.ShallowWaterDangers,
        ["plainBoundaries"] = settings.PlainBoundaries,
        ["simplifiedSymbols"] = settings.SimplifiedSymbols,
        ["fullLightLines"] = settings.FullLightLines,
        ["ignoreScaleMinimum"] = settings.IgnoreScaleMinimum,
        ["soundings"] = settings.ShowSoundings,
        ["text"] = settings.ShowText,
        ["symbolScale"] = settings.SymbolScale,
        ["textScale"] = settings.TextScale,
        ["nationalLanguage"] = settings.NationalLanguage,
    };

    private static MarinerSettings ReadMariner(JsonObject node, MarinerSettings defaults)
    {
        var read = new MarinerSettings
        {
            Palette = Enumeration(node, "palette", defaults.Palette),
            DisplayCategory = Enumeration(node, "displayCategory", defaults.DisplayCategory),
            SafetyContour = Number(node, "safetyContour") ?? defaults.SafetyContour,
            SafetyDepth = Number(node, "safetyDepth") ?? defaults.SafetyDepth,
            ShallowContour = Number(node, "shallowContour") ?? defaults.ShallowContour,
            DeepContour = Number(node, "deepContour") ?? defaults.DeepContour,
            FourShades = Boolean(node, "fourShades") ?? defaults.FourShades,
            ShallowWaterDangers = Boolean(node, "shallowWaterDangers") ?? defaults.ShallowWaterDangers,
            PlainBoundaries = Boolean(node, "plainBoundaries") ?? defaults.PlainBoundaries,
            SimplifiedSymbols = Boolean(node, "simplifiedSymbols") ?? defaults.SimplifiedSymbols,
            FullLightLines = Boolean(node, "fullLightLines") ?? defaults.FullLightLines,
            IgnoreScaleMinimum = Boolean(node, "ignoreScaleMinimum") ?? defaults.IgnoreScaleMinimum,
            ShowSoundings = Boolean(node, "soundings") ?? defaults.ShowSoundings,
            ShowText = Boolean(node, "text") ?? defaults.ShowText,
            SymbolScale = Number(node, "symbolScale") ?? defaults.SymbolScale,
            TextScale = Number(node, "textScale") ?? defaults.TextScale,
            NationalLanguage = Text(node, "nationalLanguage") ?? defaults.NationalLanguage,
        };

        // Values that break a rule (contours out of order, a scale too big)
        // can't be fixed one at a time, so the whole group falls back.
        var problems = read.Validate();
        if (problems.Contains(MarinerSettingsProblem.DepthOutOfRange) || problems.Contains(MarinerSettingsProblem.ContoursOutOfOrder))
        {
            read = read with
            {
                SafetyContour = defaults.SafetyContour,
                SafetyDepth = defaults.SafetyDepth,
                ShallowContour = defaults.ShallowContour,
                DeepContour = defaults.DeepContour,
            };
        }

        if (problems.Contains(MarinerSettingsProblem.ScaleOutOfRange))
        {
            read = read with { SymbolScale = defaults.SymbolScale, TextScale = defaults.TextScale };
        }

        if (problems.Contains(MarinerSettingsProblem.NationalLanguageInvalid))
        {
            read = read with { NationalLanguage = defaults.NationalLanguage };
        }

        return read;
    }

    private static JsonObject? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static JsonValue? Value(JsonObject node, string key) => node.TryGetPropertyValue(key, out var value) ? value as JsonValue : null;

    private static double? Number(JsonObject node, string key) =>
        Value(node, key) is { } value && value.TryGetValue<double>(out var number) && double.IsFinite(number) ? number : null;

    private static int? Integer(JsonObject node, string key) =>
        Value(node, key) is { } value && value.TryGetValue<int>(out var number) ? number : null;

    private static bool? Boolean(JsonObject node, string key) =>
        Value(node, key) is { } value && value.TryGetValue<bool>(out var flag) ? flag : null;

    private static string? Text(JsonObject node, string key) =>
        Value(node, key) is { } value && value.TryGetValue<string>(out var text) ? text : null;

    private static T Enumeration<T>(JsonObject node, string key, T fallback)
        where T : struct, Enum =>
        Text(node, key) is { } text && !text.Any(char.IsDigit) && Enum.TryParse<T>(text, ignoreCase: true, out var value) && Enum.IsDefined(value)
            ? value
            : fallback;
}
