using System.Windows.Controls;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The view of the <b>SoundCharts Object information</b> pane; its data
/// context is <see cref="ObjectInfoPaneViewModel"/>, which Pro sets.
/// </summary>
internal sealed partial class ObjectInfoPaneView : UserControl
{
    public ObjectInfoPaneView()
    {
        InitializeComponent();
    }
}
