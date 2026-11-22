using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EncDotNet.SoundCharts.Library;
using EncDotNet.SoundCharts.Themes;

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
    }

    private LibraryPaneViewModel? ViewModel => DataContext as LibraryPaneViewModel;

    private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (ViewModel is { } viewModel)
        {
            viewModel.SelectedNode = e.NewValue as LibraryNode;
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        var files = e.Data.GetDataPresent(DataFormats.FileDrop);
        e.Effects = files ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
        ShowDropTarget(files);
    }

    private void OnDragLeave(object sender, DragEventArgs e) => ShowDropTarget(false);

    // The empty Library's drop zone turns the accent colour while files are
    // dragged over the pane.
    private void ShowDropTarget(bool active) => Ui.SetIsDragOver(DropZone, active);

    private async void OnDrop(object sender, DragEventArgs e)
    {
        ShowDropTarget(false);
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
