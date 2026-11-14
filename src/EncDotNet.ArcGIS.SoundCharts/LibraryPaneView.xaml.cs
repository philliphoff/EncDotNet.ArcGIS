using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using EncDotNet.SoundCharts.Library;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The view of the <b>SoundCharts Library</b> pane; its data context is
/// <see cref="LibraryPaneViewModel"/>, which Pro sets. Files and folders
/// dropped on the pane are added to the Library: the pane is the add-in's
/// own surface, so it takes drops; the map doesn't (D9).
/// </summary>
internal sealed partial class LibraryPaneView : UserControl
{
    public LibraryPaneView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private LibraryPaneViewModel? ViewModel => DataContext as LibraryPaneViewModel;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Tree.ItemContainerStyle is not null)
        {
            return;
        }

        // Rows keep their expansion and selection across rebuilds through the
        // nodes. Built here, on Pro's own TreeViewItem style when the theme
        // has one: BasedOn in XAML needs a static resource the pane can't
        // count on finding at load time.
        var style = new Style(typeof(TreeViewItem), TryFindResource(typeof(TreeViewItem)) as Style);
        style.Setters.Add(new Setter(TreeViewItem.IsExpandedProperty, new Binding(nameof(LibraryNode.IsExpanded)) { Mode = BindingMode.TwoWay }));
        style.Setters.Add(new Setter(TreeViewItem.IsSelectedProperty, new Binding(nameof(LibraryNode.IsSelected)) { Mode = BindingMode.TwoWay }));
        style.Setters.Add(new Setter(System.Windows.Automation.AutomationProperties.NameProperty, new Binding(nameof(LibraryNode.Title))));
        Tree.ItemContainerStyle = style;
    }

    private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (ViewModel is { } viewModel)
        {
            viewModel.SelectedNode = e.NewValue as LibraryNode;
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (ViewModel is not { } viewModel || e.Data.GetData(DataFormats.FileDrop) is not string[] { Length: > 0 } paths)
        {
            return;
        }

        e.Handled = true;
        await viewModel.DropAsync(paths, NodeUnder(e.OriginalSource as DependencyObject));
    }

    // The row a drop landed on: its collection takes the dropped items.
    private static LibraryNode? NodeUnder(DependencyObject? element)
    {
        while (element is not null)
        {
            if (element is TreeViewItem { DataContext: LibraryNode node })
            {
                return node;
            }

            element = element is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(element)
                : LogicalTreeHelper.GetParent(element);
        }

        return null;
    }
}
