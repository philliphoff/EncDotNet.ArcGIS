using System.Globalization;
using System.Windows.Data;

namespace EncDotNet.SoundCharts.Themes;

/// <summary>
/// Converts a view model's tone (a <see cref="SoundChartsTone"/>, a
/// <c>RouteCheckTone</c> or a tone name) to a <see cref="SoundChartsTone"/>
/// for <c>sc:Tone.Value</c>. The theme has an instance as <c>SC_ToneConverter</c>.
/// </summary>
public sealed class ToneConverter : IValueConverter
{
    /// <summary>Converts a tone value; see <see cref="Tones.From(object?)"/>.</summary>
    /// <param name="value">The view model's tone.</param>
    /// <param name="targetType">Unused.</param>
    /// <param name="parameter">Unused.</param>
    /// <param name="culture">Unused.</param>
    /// <returns>The <see cref="SoundChartsTone"/>.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => Tones.From(value);

    /// <summary>Not supported: tones bind one way.</summary>
    /// <param name="value">Unused.</param>
    /// <param name="targetType">Unused.</param>
    /// <param name="parameter">Unused.</param>
    /// <param name="culture">Unused.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always.</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("Tones bind one way.");
}
