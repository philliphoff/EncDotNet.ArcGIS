using System.Windows.Controls;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The view of the <b>SoundCharts Route check</b> pane; its data context is
/// <see cref="RouteCheckPaneViewModel"/>, which Pro sets.
/// </summary>
internal sealed partial class RouteCheckPaneView : UserControl
{
    public RouteCheckPaneView()
    {
        InitializeComponent();
    }
}
