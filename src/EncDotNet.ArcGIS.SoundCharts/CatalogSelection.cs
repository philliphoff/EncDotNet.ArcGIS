using System.Collections;
using System.IO;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;

namespace EncDotNet.SoundCharts;

/// <summary>
/// Reads which Catalog items a context menu was opened on, in the Catalog
/// pane or a Catalog view, as file and folder paths.
/// </summary>
internal static class CatalogSelection
{
    /// <summary>
    /// Gets the paths of the right-clicked items that are files or folders
    /// on disk. Project items (maps, layouts) and portal items have none.
    /// </summary>
    /// <param name="contextItem">The dynamic menu's own context item, if Pro set one.</param>
    public static IReadOnlyList<string> RightClickedPaths(object? contextItem)
    {
        // Pro passes the clicked items as the menu's data context; the
        // Catalog window's selection is the fallback, since a right-click
        // selects the item first.
        var items = ItemsOf(FrameworkApplication.ContextMenuDataContext)
            ?? ItemsOf(contextItem)
            ?? ItemsOf((FrameworkApplication.ActiveWindow as IProjectWindow)?.SelectedItems)
            ?? ItemsOf(Project.GetCatalogPane(false)?.SelectedItems)
            ?? [];

        return [.. items
            .Select(i => i.Path)
            .Where(p => !string.IsNullOrWhiteSpace(p) && (File.Exists(p) || Directory.Exists(p)))
            .Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    private static IReadOnlyList<Item>? ItemsOf(object? context)
    {
        List<Item> items = context switch
        {
            Item item => [item],
            IEnumerable<Item> many => [.. many],
            IEnumerable other and not string => [.. other.OfType<Item>()],
            _ => [],
        };
        return items.Count > 0 ? items : null;
    }
}
