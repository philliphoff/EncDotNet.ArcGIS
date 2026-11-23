using System.Windows;

namespace EncDotNet.SoundCharts.Themes;

/// <summary>
/// The <c>sc:Tone.Value</c> attached property: the semantic tone of a chip,
/// message bar, verdict card, section header glyph or tone glyph.
/// </summary>
/// <remarks>
/// It doesn't inherit: a neutral chip inside a danger card stays neutral. Set
/// it on each toned element, binding through <see cref="ToneConverter"/>
/// (<c>SC_ToneConverter</c>) when the view model's tone isn't a
/// <see cref="SoundChartsTone"/>.
/// </remarks>
public static class Tone
{
    /// <summary>Identifies the <c>sc:Tone.Value</c> attached property.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.RegisterAttached(
        "Value",
        typeof(SoundChartsTone),
        typeof(Tone),
        new FrameworkPropertyMetadata(SoundChartsTone.Neutral));

    /// <summary>Gets an element's tone.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The tone; <see cref="SoundChartsTone.Neutral"/> if none is set.</returns>
    public static SoundChartsTone GetValue(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (SoundChartsTone)element.GetValue(ValueProperty);
    }

    /// <summary>Sets an element's tone.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The tone.</param>
    public static void SetValue(DependencyObject element, SoundChartsTone value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(ValueProperty, value);
    }
}
