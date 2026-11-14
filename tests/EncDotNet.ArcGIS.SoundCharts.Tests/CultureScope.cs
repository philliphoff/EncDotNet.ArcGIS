using System.Globalization;

namespace EncDotNet.SoundCharts.Tests;

/// <summary>
/// Sets the current culture and UI culture for a test, invariant by default,
/// so number formats don't depend on the machine's culture; restores them on
/// dispose.
/// </summary>
internal sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

    public CultureScope(CultureInfo? culture = null)
    {
        CultureInfo.CurrentCulture = culture ?? CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = culture ?? CultureInfo.InvariantCulture;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
    }
}
