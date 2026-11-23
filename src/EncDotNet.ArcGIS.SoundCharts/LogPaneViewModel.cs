using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The SoundCharts Log pane (<c>SoundCharts_LogPane</c> in Config.daml):
/// add-in events and chart server output in one list, with the viewer's
/// level filter, <b>Copy all</b> and <b>Open Log Folder</b> (design brief
/// §4.4, D8), and the error and warning counts (look and feel design §6).
/// </summary>
internal sealed class LogPaneViewModel : DockPane
{
    private const string DockPaneId = "SoundCharts_LogPane";

    private LogFilterOption _selectedFilter;
    private string? _sourceFilter;
    private LogCounts _counts;
    private bool _recountQueued;
    private bool _subscribed;

    /// <summary>Creates the view model; Pro calls it when the pane first opens.</summary>
    public LogPaneViewModel()
    {
        Filters = LogFilterOption.All();
        _selectedFilter = Filters[0];
        CopyAllCommand = new RelayCommand(CopyAll);
        OpenLogFolderCommand = new RelayCommand(OpenLogFolder);
        ShowAllCommand = new RelayCommand(() => SourceFilter = null);
        ShowLevelCommand = new RelayCommand(ShowLevel, parameter => parameter is LogLevel, true, false);
    }

    /// <summary>Gets the lines shown, oldest first.</summary>
    public ObservableCollection<LogRow> Entries { get; } = [];

    /// <summary>Gets the level filter's choices.</summary>
    public IReadOnlyList<LogFilterOption> Filters { get; }

    /// <summary>Gets or sets the chosen level filter.</summary>
    public LogFilterOption SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetProperty(ref _selectedFilter, value ?? Filters[0]))
            {
                Reload();
            }
        }
    }

    /// <summary>Gets or sets the one source to show, such as <c>chart 3f9a1c02</c>, or <see langword="null"/> for all.</summary>
    public string? SourceFilter
    {
        get => _sourceFilter;
        set
        {
            if (SetProperty(ref _sourceFilter, value))
            {
                NotifyPropertyChanged(nameof(SourceFilterText));
                NotifyPropertyChanged(nameof(HasSourceFilter));
                Reload();
            }
        }
    }

    /// <summary>
    /// Gets the errors and warnings in the log from the shown source, at
    /// every level: the toolbar's count chips.
    /// </summary>
    public LogCounts Counts
    {
        get => _counts;
        private set => SetProperty(ref _counts, value);
    }

    /// <summary>Gets whether one source is shown.</summary>
    public bool HasSourceFilter => _sourceFilter is not null;

    /// <summary>Gets the note that says which source is shown.</summary>
    public string SourceFilterText => _sourceFilter is null
        ? string.Empty
        : string.Format(CultureInfo.CurrentCulture, Strings.Log_ShowingSource, _sourceFilter);

    /// <summary>Gets the <b>Copy all</b> command.</summary>
    public ICommand CopyAllCommand { get; }

    /// <summary>Gets the <b>Open Log Folder</b> command.</summary>
    public ICommand OpenLogFolderCommand { get; }

    /// <summary>Gets the <b>Show all</b> command, which clears <see cref="SourceFilter"/>.</summary>
    public ICommand ShowAllCommand { get; }

    /// <summary>
    /// Gets the count chips' command: its parameter, a <see cref="LogLevel"/>,
    /// picks the level filter (<see cref="LogFilterOption.ForCount"/>).
    /// </summary>
    public ICommand ShowLevelCommand { get; }

    /// <summary>Opens the pane, showing only <paramref name="source"/> when given.</summary>
    /// <param name="source">The log source to show, or <see langword="null"/> for every source.</param>
    public static void Show(string? source = null)
    {
        if (FrameworkApplication.DockPaneManager.Find(DockPaneId) is LogPaneViewModel pane)
        {
            pane.SourceFilter = source;
            pane.Activate();
        }
    }

    protected override Task InitializeAsync()
    {
        if (!_subscribed)
        {
            SoundChartsSession.Log.EntryAdded += OnEntryAdded;
            _subscribed = true;
        }

        Reload();
        return base.InitializeAsync();
    }

    protected override Task UninitializeAsync()
    {
        if (_subscribed)
        {
            SoundChartsSession.Log.EntryAdded -= OnEntryAdded;
            _subscribed = false;
        }

        return base.UninitializeAsync();
    }

    private void OnEntryAdded(object? sender, LogEntry entry)
    {
        // Raised on the writing thread, often a server's output reader.
        FrameworkApplication.Current?.Dispatcher.BeginInvoke(() =>
        {
            QueueRecount();
            if (!_selectedFilter.Filter.Includes(entry, _sourceFilter))
            {
                return;
            }

            Entries.Add(new LogRow(entry, SoundChartsSession.Settings.Application.TimeFormat));
            while (Entries.Count > SoundChartsSession.Log.Capacity)
            {
                Entries.RemoveAt(0);
            }
        });
    }

    /// <summary>
    /// Recounts from the log once the dispatcher is idle: a burst of server
    /// output costs one count, and the count follows the log's own eviction.
    /// </summary>
    private void QueueRecount()
    {
        if (_recountQueued)
        {
            return;
        }

        _recountQueued = true;
        FrameworkApplication.Current?.Dispatcher.InvokeAsync(
            () =>
            {
                _recountQueued = false;
                Counts = LogCounts.Of(SoundChartsSession.Log.Snapshot(), _sourceFilter);
            },
            DispatcherPriority.Background);
    }

    private void Reload()
    {
        var snapshot = SoundChartsSession.Log.Snapshot();
        Counts = LogCounts.Of(snapshot, _sourceFilter);
        Entries.Clear();
        foreach (var entry in snapshot)
        {
            if (_selectedFilter.Filter.Includes(entry, _sourceFilter))
            {
                Entries.Add(new LogRow(entry, SoundChartsSession.Settings.Application.TimeFormat));
            }
        }
    }

    private void ShowLevel(object? parameter)
    {
        if (parameter is LogLevel level && LogFilterOption.ForCount(Filters, level) is { } option)
        {
            SelectedFilter = option;
        }
    }

    private void CopyAll()
    {
        try
        {
            Clipboard.SetText(LogEntry.ToText(Entries.Select(row => row.Entry), HomePathRedactor.Redact));
        }
        catch (ExternalException)
        {
            // Another application holds the clipboard; the user can try again.
        }
    }

    private void OpenLogFolder()
    {
        try
        {
            var folder = SoundChartsSession.Folders.Logs;
            System.IO.Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true })?.Dispose();
        }
        catch (Exception ex)
        {
            AddInLog.Write(LogLevel.Error, $"Opening the log folder failed: {ex.Message}");
        }
    }
}
