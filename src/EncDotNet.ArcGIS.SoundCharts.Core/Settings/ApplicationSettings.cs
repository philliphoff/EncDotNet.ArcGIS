namespace EncDotNet.SoundCharts.Settings;

/// <summary>
/// The SoundCharts settings of this Windows user, on the Options page
/// under <b>Application</b> (design brief §5.1, D24): the defaults new
/// projects start from, and the settings that aren't a project's.
/// </summary>
public sealed record ApplicationSettings
{
    /// <summary>The smallest tile cache, in megabytes.</summary>
    public const int MinTileCacheMegabytes = 64;

    /// <summary>The largest tile cache, in megabytes.</summary>
    public const int MaxTileCacheMegabytes = 1024 * 1024;

    /// <summary>Gets the defaults.</summary>
    public static ApplicationSettings Default { get; } = new();

    /// <summary>Gets the palette and mariner settings a project without its own starts with.</summary>
    public MarinerSettings ProjectDefaults { get; init; } = MarinerSettings.Default;

    /// <summary>
    /// Gets whether the palette follows Pro's theme (D26): Light gives
    /// <b>Day</b>, Dark gives <b>Dusk</b>. Off by default, because a printed
    /// chart wants Day whatever the screen looks like.
    /// </summary>
    public bool FollowProTheme { get; init; }

    /// <summary>Gets how times are shown (D28); <b>UTC</b> by default.</summary>
    public TimeFormat TimeFormat { get; init; } = TimeFormat.Utc;

    /// <summary>Gets the unit depths are shown in; metres by default.</summary>
    public DepthUnit DepthUnit { get; init; } = DepthUnit.Metres;

    /// <summary>
    /// Gets the path of an <c>s100.exe</c> to run instead of the bundled
    /// one (D21), or <see langword="null"/> for the bundled one. The
    /// <c>SOUNDCHARTS_S100_PATH</c> environment variable wins over it.
    /// </summary>
    public string? ServerPath { get; init; }

    /// <summary>Gets the most disk space the shared tile cache takes, in megabytes; 2048 by default.</summary>
    public int TileCacheMegabytes { get; init; } = 2048;

    /// <summary>Gets whether every value is one the add-in can use.</summary>
    public bool IsValid =>
        ProjectDefaults.Validate().Count == 0
        && Enum.IsDefined(TimeFormat)
        && Enum.IsDefined(DepthUnit)
        && TileCacheMegabytes is >= MinTileCacheMegabytes and <= MaxTileCacheMegabytes;
}
