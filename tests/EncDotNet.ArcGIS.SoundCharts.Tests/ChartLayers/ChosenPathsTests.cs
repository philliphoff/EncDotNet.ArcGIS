using EncDotNet.SoundCharts.ChartLayers;

namespace EncDotNet.SoundCharts.Tests.ChartLayers;

/// <summary>
/// Tests for <see cref="ChosenPaths"/>: reading the paths chosen for Add
/// SoundCharts Data or Add as Feature Layers behind a cancelable progress
/// dialog.
/// </summary>
public class ChosenPathsTests
{
    private static Task<DatasetInspection> Read(string path, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(path + " dataset");
        return Task.FromResult(new DatasetInspection(Path.GetFileName(path), [], []));
    }

    [Fact]
    public async Task ReadsEveryPath_InOrder()
    {
        var inspections = await ChosenPaths.InspectAsync(
            "Test", [@"C:\charts\a", @"C:\charts\b"], Read, progress: null, CancellationToken.None);

        Assert.Equal(["a", "b"], inspections.Select(i => i.Input));
    }

    [Fact]
    public async Task ReportsEachPathsName_ThenWhatTheInspectorReports()
    {
        var reported = new List<string>();
        var progress = new ListProgress(reported);

        await ChosenPaths.InspectAsync("Test", [@"C:\charts\a\", @"C:\charts\b.000"], Read, progress, CancellationToken.None);

        Assert.Equal(["a", @"C:\charts\a\ dataset", "b.000", @"C:\charts\b.000 dataset"], reported);
    }

    [Fact]
    public async Task AnUnreadablePath_BecomesAWarning_AndTheOthersAreStillRead()
    {
        static Task<DatasetInspection> FailFirst(string path, IProgress<string>? progress, CancellationToken cancellationToken) =>
            path.EndsWith('a')
                ? throw new IOException("locked")
                : Read(path, progress, cancellationToken);

        var inspections = await ChosenPaths.InspectAsync(
            "Test", [@"C:\charts\a", @"C:\charts\b"], FailFirst, progress: null, CancellationToken.None);

        Assert.Equal(2, inspections.Count);
        Assert.Contains(inspections[0].Warnings, w => w.Contains("locked", StringComparison.Ordinal));
        Assert.Empty(inspections[1].Warnings);
    }

    [Fact]
    public async Task ACancel_StopsBeforeTheNextPath()
    {
        using var cancel = new CancellationTokenSource();
        var read = new List<string>();
        Task<DatasetInspection> CancelAfterFirst(string path, IProgress<string>? progress, CancellationToken cancellationToken)
        {
            read.Add(path);
            cancel.Cancel();
            return Read(path, progress, CancellationToken.None);
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ChosenPaths.InspectAsync(
            "Test", [@"C:\charts\a", @"C:\charts\b"], CancelAfterFirst, progress: null, cancel.Token));

        Assert.Equal([@"C:\charts\a"], read);
    }

    [Fact]
    public async Task ACancelInsideTheInspector_IsNotReportedAsAnUnreadablePath()
    {
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();

        // Read throws OperationCanceledException; it must propagate, not become a warning.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ChosenPaths.InspectAsync(
            "Test", [@"C:\charts\a"], (path, progress, _) => Read(path, progress, cancel.Token), progress: null, CancellationToken.None));
    }

    /// <summary>Records reports synchronously, unlike <see cref="Progress{T}"/>.</summary>
    private sealed class ListProgress(List<string> reported) : IProgress<string>
    {
        public void Report(string value) => reported.Add(value);
    }
}
