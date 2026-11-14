using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.Library;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Resources;
using Microsoft.Win32;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The <b>SoundCharts Library</b> pane (<c>SoundCharts_LibraryPane</c> in
/// Config.daml, design brief D3, D9): the viewer's Library model over
/// EncDotNet.S100 dataset collections. Collections, their sources and their
/// indexed datasets in a tree, the selected dataset's details, and the
/// actions <b>Add to Map</b>, <b>Show Footprint</b>, <b>Validate</b> and
/// <b>Open Containing Folder</b>. Nothing is loaded until asked.
/// </summary>
internal sealed class LibraryPaneViewModel : DockPane
{
    private const string DockPaneId = "SoundCharts_LibraryPane";

    private readonly LibraryFootprintOverlay _footprint = new();
    private string _filterText = string.Empty;
    private LibraryNode? _selectedNode;
    private string? _selectedId;
    private LibraryDatasetDetails? _details;
    private bool _subscribed;
    private int _rebuildQueued;

    /// <summary>Creates the view model; Pro calls it when the pane first opens.</summary>
    public LibraryPaneViewModel()
    {
        AddFolderCommand = new RelayCommand(AddFolder);
        AddExchangeSetCommand = new RelayCommand(AddExchangeSet);
        AddManifestCommand = new RelayCommand(AddManifest);
        RefreshCommand = new RelayCommand(Refresh);
        RemoveCommand = new RelayCommand(Remove, () => _selectedNode?.Kind is LibraryNodeKind.Collection or LibraryNodeKind.Source);
        AddToMapCommand = new RelayCommand(AddToMapAsync, () => _selectedNode?.Item is not null && MapView.Active is not null);
        ShowFootprintCommand = new RelayCommand(ShowFootprintAsync, () => _selectedNode?.Item is not null);
        ValidateCommand = new RelayCommand(ValidateAsync, () => _selectedNode?.Item is not null && !IsValidating);
        OpenContainingFolderCommand = new RelayCommand(OpenContainingFolder, () => _selectedNode?.Item is not null);
    }

    /// <summary>Gets the tree's rows: one per collection.</summary>
    public ObservableCollection<LibraryNode> Nodes { get; } = [];

    /// <summary>Gets or sets the filter text, matched against dataset names, titles, products and properties.</summary>
    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value ?? string.Empty))
            {
                Rebuild();
            }
        }
    }

    /// <summary>Gets or sets the selected row; the view sets it from the tree.</summary>
    public LibraryNode? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (SetProperty(ref _selectedNode, value))
            {
                _selectedId = value?.Id;
                UpdateDetails(readTimeSteps: true);
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>Gets the selected dataset's details, or <see langword="null"/> when no dataset is selected.</summary>
    public LibraryDatasetDetails? Details
    {
        get => _details;
        private set
        {
            if (SetProperty(ref _details, value))
            {
                NotifyPropertyChanged(nameof(HasDetails));
                NotifyPropertyChanged(nameof(ShowPlaceholder));
            }
        }
    }

    /// <summary>Gets whether a dataset's details are shown.</summary>
    public bool HasDetails => _details is not null;

    /// <summary>Gets whether the details panel shows <b>Select a dataset to see its details</b>.</summary>
    public bool ShowPlaceholder => _details is null && !IsEmpty;

    /// <summary>Gets whether the library has no collections.</summary>
    public bool IsEmpty => LibraryHost.Library.Collections.Count == 0;

    /// <summary>Gets the notice shown with every dataset.</summary>
    public string NotForNavigation => Strings.Library_NotForNavigation;

    /// <summary>Gets the <b>Add a folder…</b> command.</summary>
    public ICommand AddFolderCommand { get; }

    /// <summary>Gets the <b>Add an exchange set…</b> command.</summary>
    public ICommand AddExchangeSetCommand { get; }

    /// <summary>Gets the <b>Add a collection manifest…</b> command.</summary>
    public ICommand AddManifestCommand { get; }

    /// <summary>Gets the <b>Refresh</b> command: the selected collection or source, or everything.</summary>
    public ICommand RefreshCommand { get; }

    /// <summary>Gets the <b>Remove from library</b> command.</summary>
    public ICommand RemoveCommand { get; }

    /// <summary>Gets the <b>Add to Map</b> command.</summary>
    public ICommand AddToMapCommand { get; }

    /// <summary>Gets the <b>Show Footprint</b> command.</summary>
    public ICommand ShowFootprintCommand { get; }

    /// <summary>Gets the <b>Validate</b> command.</summary>
    public ICommand ValidateCommand { get; }

    /// <summary>Gets the <b>Open Containing Folder</b> command.</summary>
    public ICommand OpenContainingFolderCommand { get; }

    private bool IsValidating =>
        SelectedFile is { } file && LibraryHost.Validations.Get(file).State == DatasetValidationState.Running;

    private string? SelectedFile => _selectedNode?.Item is { } state ? LibraryItemPaths.DatasetFile(state.Item) : null;

    /// <summary>Opens the pane, selecting a collection when given.</summary>
    /// <param name="collectionId">The collection to select and expand, or <see langword="null"/>.</param>
    public static void Show(Guid? collectionId = null)
    {
        if (FrameworkApplication.DockPaneManager.Find(DockPaneId) is LibraryPaneViewModel pane)
        {
            if (collectionId is { } id)
            {
                pane._selectedId = LibraryTreeBuilder.CollectionNodeId(id);
                pane.Rebuild();
            }

            pane.Activate();
        }
    }

    /// <summary>
    /// Adds dropped paths to the library: to the collection of the row they
    /// were dropped on, or to a new collection.
    /// </summary>
    /// <param name="paths">The dropped files and folders.</param>
    /// <param name="target">The row under the drop, or <see langword="null"/>.</param>
    public async Task DropAsync(IReadOnlyList<string> paths, LibraryNode? target)
    {
        try
        {
            await LibraryHost.AddAsync(paths, target?.Collection?.Id);
        }
        catch (Exception ex)
        {
            Fail("Adding dropped items to the Library", ex);
        }
    }

    protected override Task InitializeAsync()
    {
        if (!_subscribed)
        {
            LibraryHost.Library.Changed += OnLibraryChanged;
            LibraryHost.Validations.Changed += OnValidationChanged;
            _subscribed = true;
        }

        Rebuild();
        return base.InitializeAsync();
    }

    protected override Task UninitializeAsync()
    {
        if (_subscribed)
        {
            LibraryHost.Library.Changed -= OnLibraryChanged;
            LibraryHost.Validations.Changed -= OnValidationChanged;
            _subscribed = false;
        }

        _ = _footprint.ClearAsync();
        return base.UninitializeAsync();
    }

    protected override void OnShow(bool isVisible)
    {
        if (!isVisible)
        {
            _ = _footprint.ClearAsync();
        }
    }

    private void OnLibraryChanged(object? sender, EventArgs e)
    {
        // Raised on the indexing thread, often many times in a row: rebuild once per burst.
        if (Interlocked.Exchange(ref _rebuildQueued, 1) == 0)
        {
            FrameworkApplication.Current?.Dispatcher.BeginInvoke(() =>
            {
                Interlocked.Exchange(ref _rebuildQueued, 0);
                Rebuild();
            });
        }
    }

    private void OnValidationChanged(object? sender, string path)
    {
        FrameworkApplication.Current?.Dispatcher.BeginInvoke(() =>
        {
            if (string.Equals(path, SelectedFile, StringComparison.OrdinalIgnoreCase))
            {
                UpdateDetails(readTimeSteps: false);
                CommandManager.InvalidateRequerySuggested();
            }
        });
    }

    private void Rebuild()
    {
        var expanded = Nodes.SelectMany(n => n.SelfAndDescendants()).Where(n => n.IsExpanded).Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
        if (_selectedId is { } selected && selected.StartsWith("c:", StringComparison.Ordinal))
        {
            expanded.Add(selected);
        }

        var nodes = LibraryTreeBuilder.Build(LibraryHost.Library.Collections, _filterText, expanded, _selectedId);
        Nodes.Clear();
        foreach (var node in nodes)
        {
            Nodes.Add(node);
        }

        _selectedNode = nodes.SelectMany(n => n.SelfAndDescendants()).FirstOrDefault(n => n.Id == _selectedId);
        NotifyPropertyChanged(nameof(SelectedNode));
        NotifyPropertyChanged(nameof(IsEmpty));
        NotifyPropertyChanged(nameof(ShowPlaceholder));
        UpdateDetails(readTimeSteps: true);
        CommandManager.InvalidateRequerySuggested();
    }

    private void UpdateDetails(bool readTimeSteps)
    {
        if (_selectedNode is not { Item: { } state } node)
        {
            Details = null;
            return;
        }

        var file = LibraryItemPaths.DatasetFile(state.Item);
        var validation = file is null ? DatasetValidation.NotValidated : LibraryHost.Validations.Get(file);
        IReadOnlyList<DateTime>? steps = file is null ? [] : LibraryHost.CachedTimeSteps(file);
        Details = LibraryDatasetDetailsBuilder.Build(state, node.Collection?.Definition.Name, validation, steps);

        if (readTimeSteps && steps is null && file is not null && LibraryDatasetDetailsBuilder.HasTimeSteps(state.Item.ProductSpec))
        {
            _ = ReadTimeStepsAsync(file);
        }
    }

    private async Task ReadTimeStepsAsync(string file)
    {
        try
        {
            await LibraryHost.ReadTimeStepsAsync(file);
            FrameworkApplication.Current?.Dispatcher.BeginInvoke(() =>
            {
                if (string.Equals(file, SelectedFile, StringComparison.OrdinalIgnoreCase))
                {
                    UpdateDetails(readTimeSteps: false);
                }
            });
        }
        catch (Exception ex)
        {
            AddInLog.Write(LogLevel.Warning, $"SoundCharts Library: reading the time steps of {file} failed: {ex.Message}");
        }
    }

    private void AddFolder()
    {
        var dialog = new OpenFolderDialog { Title = Strings.Library_AddFolderTitle, Multiselect = true };
        if (dialog.ShowDialog(FrameworkApplication.Current.MainWindow) == true)
        {
            _ = AddAsync(dialog.FolderNames, "Add a folder");
        }
    }

    private void AddExchangeSet()
    {
        var dialog = new OpenFileDialog
        {
            Title = Strings.Library_AddExchangeSetTitle,
            Filter = Strings.Library_ExchangeSetFilter,
            Multiselect = true,
            CheckFileExists = true,
        };
        if (dialog.ShowDialog(FrameworkApplication.Current.MainWindow) == true)
        {
            _ = AddAsync(dialog.FileNames, "Add an exchange set");
        }
    }

    private void AddManifest()
    {
        var dialog = new OpenFileDialog
        {
            Title = Strings.Library_AddManifestTitle,
            Filter = Strings.Library_ManifestFilter,
            Multiselect = false,
            CheckFileExists = true,
        };
        if (dialog.ShowDialog(FrameworkApplication.Current.MainWindow) == true)
        {
            _ = AddAsync(dialog.FileNames, "Add a collection manifest");
        }
    }

    private async Task AddAsync(IReadOnlyList<string> paths, string action)
    {
        try
        {
            // Adding to the selected collection, as the viewer's Add to Library offers it first.
            await LibraryHost.AddAsync(paths, _selectedNode?.Collection?.Id);
        }
        catch (Exception ex)
        {
            Fail(action, ex);
        }
    }

    private void Refresh()
    {
        LibraryHost.Library.Refresh(_selectedNode?.Collection?.Id, _selectedNode?.Kind == LibraryNodeKind.Collection ? null : _selectedNode?.Source?.Id);
    }

    private void Remove()
    {
        if (_selectedNode is not { Collection: { } collection } node)
        {
            return;
        }

        var sourceId = node.Kind == LibraryNodeKind.Source ? node.Source?.Id : null;
        var name = node.Title;
        var answer = MessageBox.Show(
            string.Format(CultureInfo.CurrentCulture, Strings.Library_RemoveConfirmFormat, name),
            Strings.Library_Caption,
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);
        if (answer == MessageBoxResult.OK)
        {
            // The last source of a collection takes the collection with it, as an empty collection lists nothing.
            var removed = sourceId is { } source && collection.Sources.Count > 1
                ? LibraryHost.Library.Remove(collection.Id, source)
                : LibraryHost.Library.Remove(collection.Id);
            AddInLog.Write($"SoundCharts Library: removed {name} ({(removed ? "done" : "not found")}).");
            _selectedId = null;
        }
    }

    private async Task AddToMapAsync()
    {
        try
        {
            if (_selectedNode?.Item is not { } state || MapView.Active is not { } view)
            {
                return;
            }

            var paths = LibraryItemPaths.AddToMapPaths([state.Item]);
            if (paths.FirstOrDefault(p => !LibraryItemPaths.Exists(p)) is { } missing)
            {
                Say(string.Format(CultureInfo.CurrentCulture, Strings.Library_FileMissingFormat, missing), MessageBoxImage.Warning);
                return;
            }

            AddInLog.Write($"SoundCharts Library: adding {string.Join(", ", paths)} to the map.");
            await AddSoundChartsData.RunAsync(view, paths);
        }
        catch (Exception ex)
        {
            Fail("Add to Map", ex);
        }
    }

    private async Task ShowFootprintAsync()
    {
        try
        {
            if (_selectedNode?.Item is not { } state)
            {
                return;
            }

            if (MapView.Active is not { } view)
            {
                Say(Strings.Library_NoMap, MessageBoxImage.Information);
                return;
            }

            // The index's coverage or bounds; else the dataset's own extent, read now.
            var footprint = LibraryFootprint.Of(state.Item);
            if (footprint is null && LibraryItemPaths.DatasetFile(state.Item) is { } file && LibraryItemPaths.Exists(file))
            {
                footprint = await Task.Run(() => LibraryHost.Reader.ReadFootprint(file));
            }

            if (footprint is null)
            {
                Say(Strings.Library_NoFootprint, MessageBoxImage.Information);
                return;
            }

            await _footprint.ShowAsync(view, footprint);
        }
        catch (Exception ex)
        {
            Fail("Show Footprint", ex);
        }
    }

    private async Task ValidateAsync()
    {
        try
        {
            if (_selectedNode?.Item is not { } state)
            {
                return;
            }

            if (LibraryItemPaths.DatasetFile(state.Item) is not { } file)
            {
                Say(Strings.Library_ValidationZip, MessageBoxImage.Information);
                return;
            }

            if (!LibraryItemPaths.Exists(file))
            {
                Say(string.Format(CultureInfo.CurrentCulture, Strings.Library_FileMissingFormat, file), MessageBoxImage.Warning);
                return;
            }

            if (LibraryHost.Validator is not { } validator)
            {
                Say(Strings.Library_ValidationNoServer, MessageBoxImage.Warning);
                return;
            }

            var result = await LibraryHost.Validations.ValidateAsync(file, validator);
            AddInLog.Write(
                result.State == DatasetValidationState.Failed ? LogLevel.Warning : LogLevel.Info,
                $"SoundCharts Library: validated {file}: {result.State}, {result.Errors} error(s), {result.Warnings} warning(s). {result.Message}".TrimEnd());
        }
        catch (Exception ex)
        {
            Fail("Validate", ex);
        }
    }

    private void OpenContainingFolder()
    {
        try
        {
            if (_selectedNode?.Item is not { } state || LibraryItemPaths.FileToReveal(state.Item) is not { } file)
            {
                return;
            }

            if (!LibraryItemPaths.Exists(file))
            {
                Say(string.Format(CultureInfo.CurrentCulture, Strings.Library_FileMissingFormat, file), MessageBoxImage.Warning);
                return;
            }

            // explorer.exe reads /select itself, so the path is quoted after the comma.
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{file}\"") { UseShellExecute = false })?.Dispose();
        }
        catch (Exception ex)
        {
            Fail("Open Containing Folder", ex);
        }
    }

    private static void Say(string message, MessageBoxImage image) =>
        MessageBox.Show(message, Strings.Library_Caption, MessageBoxButton.OK, image);

    private static void Fail(string action, Exception ex)
    {
        // The boundary with Pro: log and tell, rather than let the exception reach Pro.
        AddInLog.Write(LogLevel.Error, $"SoundCharts Library: {action} failed: {ex}");
        Say(string.Format(CultureInfo.CurrentCulture, Strings.Library_ActionFailedFormat, ex.Message), MessageBoxImage.Error);
    }
}
