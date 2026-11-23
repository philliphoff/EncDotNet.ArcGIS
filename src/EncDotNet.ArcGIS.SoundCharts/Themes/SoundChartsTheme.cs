using System.Windows;
using ArcGIS.Desktop.Framework;

namespace EncDotNet.SoundCharts.Themes;

/// <summary>
/// The add-in's theme: the shared styles in <c>Themes/SoundCharts.xaml</c>
/// and the colour dictionary for Pro's theme. Each view merges one of these.
/// </summary>
/// <remarks>
/// <para>
/// The colour dictionary is chosen when the view loads, from
/// <see cref="FrameworkApplication.ApplicationTheme"/> (Default, Dark or
/// HighContrast), or High Contrast when Windows has a contrast theme on. Pro
/// 3.7 has no public theme-changed event (its reference assemblies have none),
/// so there is nothing to swap on: after a theme change, restart Pro. A view
/// created after the change also takes the new theme.
/// </para>
/// <para>
/// Each instance loads its own copies of the dictionaries, so a view's
/// brushes and styles resolve against that view alone. Merging the theme
/// into a view's resources is what makes
/// <c>BasedOn="{StaticResource SC_…}"</c> legal in the view.
/// </para>
/// </remarks>
public sealed class SoundChartsTheme : ResourceDictionary
{
    /// <summary>Merges the colour dictionary for Pro's theme and the shared styles.</summary>
    public SoundChartsTheme()
    {
        var variant = ThemeDictionaries.Choose(ReadProTheme(), SystemParameters.HighContrast);
        Variant = variant;
        MergedDictionaries.Add(new ResourceDictionary { Source = ThemeDictionaries.ColorsUri(variant) });
        MergedDictionaries.Add(new ResourceDictionary { Source = ThemeDictionaries.SharedUri });
    }

    /// <summary>The colour dictionary this theme merged.</summary>
    public ThemeVariant Variant { get; }

    private static ThemeVariant ReadProTheme()
    {
        try
        {
            return FrameworkApplication.ApplicationTheme switch
            {
                ApplicationTheme.Dark => ThemeVariant.Dark,
                ApplicationTheme.HighContrast => ThemeVariant.HighContrast,
                _ => ThemeVariant.Light,
            };
        }
        catch (Exception)
        {
            // The Visual Studio designer and tests have no running Pro; Light
            // matches the design-only Default.xaml the views merge.
            return ThemeVariant.Light;
        }
    }
}
