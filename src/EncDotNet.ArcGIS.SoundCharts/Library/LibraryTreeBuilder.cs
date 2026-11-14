using System.Globalization;
using EncDotNet.S100.Collections;
using EncDotNet.S100.Collections.Library;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts.Library;

/// <summary>
/// Builds the Library tree from the library's snapshot, as the viewer's
/// Library lists it: collections, their sources, and each source's indexed
/// datasets, with the viewer's status lines and tags (issue #14, D9).
/// </summary>
public static class LibraryTreeBuilder
{
    /// <summary>Gets the id of a collection's row.</summary>
    /// <param name="collectionId">The collection.</param>
    public static string CollectionNodeId(Guid collectionId) => $"c:{collectionId}";

    /// <summary>Gets the id of a source's row.</summary>
    /// <param name="sourceId">The source.</param>
    public static string SourceNodeId(Guid sourceId) => $"s:{sourceId}";

    /// <summary>Builds the rows.</summary>
    /// <param name="collections">The library's collections.</param>
    /// <param name="filter">
    /// Free text to match datasets by name, title, product or a property, as
    /// the viewer's filter; <see langword="null"/> or blank for all. A filter
    /// expands every row that has a match.
    /// </param>
    /// <param name="expanded">The ids of rows to show expanded.</param>
    /// <param name="selectedId">The id of the row to show selected, or <see langword="null"/>.</param>
    /// <returns>One row per collection that has something to show.</returns>
    public static IReadOnlyList<LibraryNode> Build(
        IReadOnlyList<LibraryCollection> collections,
        string? filter = null,
        IReadOnlySet<string>? expanded = null,
        string? selectedId = null)
    {
        ArgumentNullException.ThrowIfNull(collections);
        var text = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim();
        var rows = new List<LibraryNode>();
        foreach (var collection in collections)
        {
            var sources = new List<LibraryNode>();
            foreach (var source in collection.Sources)
            {
                var datasets = Datasets(collection, source, text, selectedId);
                if (text is not null && datasets.Count == 0)
                {
                    continue;
                }

                var (line, warning) = Status(collection, [source]);
                var sourceId = SourceNodeId(source.Id);
                sources.Add(new LibraryNode(sourceId, LibraryNodeKind.Source, LibraryNodeText.SourceName(source, collection), line ?? LibraryNodeText.SourceLocation(source.Definition), datasets)
                {
                    Badge = LibraryNodeText.KindOf(source.Definition),
                    IsWarning = warning,
                    Collection = collection,
                    Source = source,
                    IsExpanded = text is not null || (expanded?.Contains(sourceId) ?? false),
                    IsSelected = sourceId == selectedId,
                });
            }

            if (text is not null && sources.Count == 0)
            {
                continue;
            }

            var (status, isWarning) = Status(collection, collection.Sources);
            var id = CollectionNodeId(collection.Id);
            rows.Add(new LibraryNode(id, LibraryNodeKind.Collection, collection.Definition.Name, status, sources)
            {
                Badge = DatasetCount(collection.ItemCount),
                IsWarning = isWarning,
                Collection = collection,
                IsExpanded = text is not null || (expanded?.Contains(id) ?? false),
                IsSelected = id == selectedId,
            });
        }

        return rows;
    }

    /// <summary>"1 dataset", "12 datasets".</summary>
    /// <param name="count">The number of datasets.</param>
    public static string DatasetCount(int count) => count == 1
        ? Strings.Library_OneDataset
        : string.Format(CultureInfo.CurrentCulture, Strings.Library_DatasetCountFormat, count);

    /// <summary>"Ed 3", "Ed 3 Upd 2", or <see langword="null"/> without an edition.</summary>
    /// <param name="item">The dataset.</param>
    public static string? EditionText(CollectionItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return (item.Edition, item.Update) switch
        {
            ({ } edition, { } update and > 0) => string.Format(CultureInfo.CurrentCulture, Strings.Library_EditionUpdateFormat, edition, update),
            ({ } edition, _) => string.Format(CultureInfo.CurrentCulture, Strings.Library_EditionFormat, edition),
            _ => null,
        };
    }

    private static List<LibraryNode> Datasets(LibraryCollection collection, LibrarySource source, string? filter, string? selectedId)
    {
        var rows = new List<LibraryNode>();
        if (source.Index is not { } index)
        {
            return rows;
        }

        foreach (var item in index.Items
            .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.ProductSpec, StringComparer.Ordinal))
        {
            var state = new LibraryItemState(item, source);
            if (filter is not null && !state.Matches(filter))
            {
                continue;
            }

            var tags = LibraryItemText.Tags(new LibraryItemTextInput(item, source, state.Availability, state.EffectiveItem))
                .Select(t => t.Text)
                .ToList();
            if (state.Availability == LibraryAvailability.Missing)
            {
                tags.Insert(0, Strings.Library_Tag_Missing);
            }

            if (state.NotForNavigation)
            {
                tags.Add(Strings.Library_NotForNavigation);
            }

            var badge = EditionText(item) is { } edition ? $"{item.ProductSpec} · {edition}" : item.ProductSpec;
            var title = string.IsNullOrWhiteSpace(item.Title) || string.Equals(item.Title, item.Name, StringComparison.OrdinalIgnoreCase)
                ? null
                : item.Title;
            rows.Add(new LibraryNode(state.Id, LibraryNodeKind.Dataset, item.Name, title, [])
            {
                Badge = badge,
                Tags = tags,
                IsWarning = state.Availability == LibraryAvailability.Missing,
                Collection = collection,
                Source = source,
                Item = state,
                IsSelected = state.Id == selectedId,
            });
        }

        return rows;
    }

    private static (string? Line, bool IsWarning) Status(LibraryCollection collection, IReadOnlyList<LibrarySource> sources)
    {
        var (line, kind) = LibraryNodeText.Status(new LibraryNodeStatusInput(collection, sources));
        return (line, kind is LibraryNodeStatusKind.Warning or LibraryNodeStatusKind.Error);
    }
}
