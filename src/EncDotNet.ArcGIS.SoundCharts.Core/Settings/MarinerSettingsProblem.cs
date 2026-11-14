namespace EncDotNet.SoundCharts.Settings;

/// <summary>What <see cref="MarinerSettings.Validate"/> can find wrong.</summary>
public enum MarinerSettingsProblem
{
    /// <summary>A contour or depth is negative, too deep or not a number.</summary>
    DepthOutOfRange,

    /// <summary>The shallow contour is deeper than the safety contour, or the deep contour shallower.</summary>
    ContoursOutOfOrder,

    /// <summary>A symbol or text scale is outside 0.5 to 3.</summary>
    ScaleOutOfRange,

    /// <summary>The national language isn't three letters.</summary>
    NationalLanguageInvalid,

    /// <summary>A palette or display category isn't one the add-in knows.</summary>
    UnknownValue,
}
