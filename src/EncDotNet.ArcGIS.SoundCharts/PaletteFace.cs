using EncDotNet.SoundCharts.Settings;

namespace EncDotNet.SoundCharts;

/// <summary>
/// Makes the <b>Palette</b> split button's face show the palette the
/// layers use (design proposal §4.5, §5.2), whichever way it was set: from
/// the split button, the Options page, or Follow Pro theme.
/// </summary>
/// <remarks>
/// Pro's split button shows the large image of the item last clicked, and
/// has no API to choose the item. So every item's large image is set to the
/// active palette's, which Pro loaded from Config.daml for the theme in use
/// (<c>Images</c> or <c>DarkImages</c>); each item keeps its own small
/// image, which is what the drop-down shows. Runs on the UI thread, from
/// <c>OnUpdate</c>.
/// </remarks>
internal static class PaletteFace
{
    private static readonly Dictionary<ChartPalette, object> Images = [];

    /// <summary>Remembers <paramref name="palette"/>'s own large image, from its button's Config.daml entry.</summary>
    /// <param name="palette">The palette.</param>
    /// <param name="image">The large image Pro loaded for the button, or <see langword="null"/>.</param>
    public static void Register(ChartPalette palette, object? image)
    {
        if (image is not null)
        {
            Images.TryAdd(palette, image);
        }
    }

    /// <summary>Gets the large image of the palette the layers use, or <paramref name="fallback"/> before it's known.</summary>
    /// <param name="fallback">The button's own large image.</param>
    public static object? Active(object? fallback) =>
        Images.TryGetValue(SoundChartsSession.Settings.Effective.Palette, out var image) ? image : fallback;
}
