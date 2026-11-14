namespace EncDotNet.SoundCharts.Settings;

/// <summary>
/// Each palette and mariner setting of a project, so the add-in can say
/// which ones the chart server applies (<see cref="ChartServerSupport"/>).
/// </summary>
public enum MarinerSetting
{
    /// <summary>The palette.</summary>
    Palette,

    /// <summary>The display category.</summary>
    DisplayCategory,

    /// <summary>The safety contour.</summary>
    SafetyContour,

    /// <summary>The safety depth.</summary>
    SafetyDepth,

    /// <summary>The shallow contour.</summary>
    ShallowContour,

    /// <summary>The deep contour.</summary>
    DeepContour,

    /// <summary>The four-shade depth scheme.</summary>
    FourShades,

    /// <summary>Highlighting shallow-water dangers.</summary>
    ShallowWaterDangers,

    /// <summary>Plain boundaries.</summary>
    PlainBoundaries,

    /// <summary>Simplified symbols.</summary>
    SimplifiedSymbols,

    /// <summary>Full light lines.</summary>
    FullLightLines,

    /// <summary>Ignoring the scale minimum.</summary>
    IgnoreScaleMinimum,

    /// <summary>Showing soundings.</summary>
    Soundings,

    /// <summary>Showing text.</summary>
    Text,

    /// <summary>The symbol scale.</summary>
    SymbolScale,

    /// <summary>The text scale.</summary>
    TextScale,

    /// <summary>The national language.</summary>
    NationalLanguage,
}
