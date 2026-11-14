using System.ComponentModel;
using System.Runtime.CompilerServices;
using EncDotNet.S100.Collections.Library;

namespace EncDotNet.SoundCharts.Library;

/// <summary>
/// One row of the Library tree: a collection, a source or a dataset, in the
/// viewer's words (issue #14, D9). Built by <see cref="LibraryTreeBuilder"/>
/// from the library's snapshot; only expansion and selection change.
/// </summary>
public sealed class LibraryNode : INotifyPropertyChanged
{
    private bool _isExpanded;
    private bool _isSelected;

    /// <summary>Creates a row.</summary>
    /// <param name="id">The row's id, stable across rebuilds.</param>
    /// <param name="kind">What the row is.</param>
    /// <param name="title">The row's name.</param>
    /// <param name="detail">The second line, or <see langword="null"/> for none.</param>
    /// <param name="children">The rows under it.</param>
    public LibraryNode(string id, LibraryNodeKind kind, string title, string? detail, IReadOnlyList<LibraryNode> children)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(children);
        Id = id;
        Kind = kind;
        Title = title;
        Detail = detail;
        Children = children;
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the row's id, stable across rebuilds.</summary>
    public string Id { get; }

    /// <summary>Gets what the row is.</summary>
    public LibraryNodeKind Kind { get; }

    /// <summary>Gets the row's name.</summary>
    public string Title { get; }

    /// <summary>Gets the second line, or <see langword="null"/>.</summary>
    public string? Detail { get; }

    /// <summary>Gets whether the row has a second line.</summary>
    public bool HasDetail => !string.IsNullOrEmpty(Detail);

    /// <summary>Gets a short tag after the name: a source's kind (<c>DIR</c>, <c>ZIP</c>, <c>JSON</c>), a dataset's product and edition.</summary>
    public string? Badge { get; init; }

    /// <summary>Gets the dataset's tags, such as <b>Loaded</b>, in the viewer's words.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Gets whether the second line is a problem (indexing failed, a path not found).</summary>
    public bool IsWarning { get; init; }

    /// <summary>Gets the rows under this one.</summary>
    public IReadOnlyList<LibraryNode> Children { get; }

    /// <summary>Gets the collection of the row, or the one its source or dataset is in.</summary>
    public LibraryCollection? Collection { get; init; }

    /// <summary>Gets the source of a source or dataset row.</summary>
    public LibrarySource? Source { get; init; }

    /// <summary>Gets the dataset of a dataset row.</summary>
    public LibraryItemState? Item { get; init; }

    /// <summary>Gets or sets whether the row is expanded.</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => Set(ref _isExpanded, value);
    }

    /// <summary>Gets or sets whether the row is selected.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => Set(ref _isSelected, value);
    }

    /// <summary>Gets this row and every row under it, depth first.</summary>
    public IEnumerable<LibraryNode> SelfAndDescendants()
    {
        yield return this;
        foreach (var child in Children)
        {
            foreach (var node in child.SelfAndDescendants())
            {
                yield return node;
            }
        }
    }

    private void Set(ref bool field, bool value, [CallerMemberName] string? name = null)
    {
        if (field != value)
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
