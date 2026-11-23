using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The view of the <b>SoundCharts Route check</b> pane; its data context is
/// <see cref="RouteCheckPaneViewModel"/>, which Pro sets.
/// </summary>
internal sealed partial class RouteCheckPaneView : UserControl
{
    /// <summary>
    /// The pane width below which the toolbar's three labelled buttons don't
    /// fit, so Open Route... moves into the ≡ menu (look and feel design §6).
    /// </summary>
    private const double NarrowWidth = 360;

    private const string OpenRouteAutomationId = "RouteCheck.OpenRoute";

    public RouteCheckPaneView()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.WidthChanged)
        {
            ShowOpenRouteInMenu(e.NewSize.Width < NarrowWidth);
        }
    }

    private void ShowOpenRouteInMenu(bool inMenu)
    {
        OpenRouteButton.Visibility = inMenu ? Visibility.Collapsed : Visibility.Visible;
        MoreButton.Visibility = inMenu ? Visibility.Visible : Visibility.Collapsed;

        // One of the two carries Open Route's AutomationId at a time, so a UI
        // test finds the one that's shown.
        AutomationProperties.SetAutomationId(OpenRouteButton, inMenu ? string.Empty : OpenRouteAutomationId);
        AutomationProperties.SetAutomationId(OpenRouteMenuItem, inMenu ? OpenRouteAutomationId : string.Empty);
    }
}
