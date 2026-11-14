using EncDotNet.SoundCharts.Charts;

namespace EncDotNet.SoundCharts.Settings;

/// <summary>
/// What SoundCharts layers draw with: the palette after Follow Pro theme,
/// and the chart server's start-up options.
/// </summary>
/// <param name="Palette">The palette the layers show.</param>
/// <param name="RenderOptions">The start-up options every chart server gets.</param>
public sealed record EffectiveChartSettings(ChartPalette Palette, ChartRenderOptions RenderOptions)
{
    /// <summary>Gets the layers' WMTS style.</summary>
    public string Style => ChartStyle.For(Palette);

    /// <summary>
    /// Works out what the layers draw with (D26): with Follow Pro theme on,
    /// Pro's Light theme gives <b>Day</b> and Dark gives <b>Dusk</b>;
    /// otherwise the project's palette. <b>Night</b> is only ever the
    /// project's choice.
    /// </summary>
    /// <param name="project">The project's settings.</param>
    /// <param name="application">The application settings.</param>
    /// <param name="darkTheme">Whether Pro shows its dark theme.</param>
    public static EffectiveChartSettings From(MarinerSettings project, ApplicationSettings application, bool darkTheme)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(application);

        var palette = application.FollowProTheme
            ? darkTheme ? ChartPalette.Dusk : ChartPalette.Day
            : project.Palette;
        return new EffectiveChartSettings(palette, project.ToRenderOptions());
    }

    /// <summary>Gets what going from <paramref name="before"/> to this means for the layers.</summary>
    /// <param name="before">The settings before.</param>
    public ChartSettingsChange ChangeFrom(EffectiveChartSettings before)
    {
        ArgumentNullException.ThrowIfNull(before);
        if (before.RenderOptions != RenderOptions)
        {
            return ChartSettingsChange.Restart;
        }

        return before.Palette != Palette ? ChartSettingsChange.Restyle : ChartSettingsChange.None;
    }
}
