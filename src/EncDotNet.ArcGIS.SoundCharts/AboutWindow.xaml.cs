using System.Runtime.InteropServices;
using System.Windows;

namespace EncDotNet.SoundCharts;

/// <summary>The <b>About SoundCharts</b> window.</summary>
internal partial class AboutWindow
{
    private readonly AboutInfo _info;

    /// <summary>Creates the window showing <paramref name="info"/>.</summary>
    public AboutWindow(AboutInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        _info = info;
        DataContext = info;
        InitializeComponent();
    }

    private void OnCopyClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(_info.ToText());
        }
        catch (ExternalException)
        {
            // Another application holds the clipboard; the user can try again.
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
