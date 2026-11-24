using EncDotNet.S100;
using EncDotNet.S100.Datasets.Pipelines;
using EncDotNet.SoundCharts.MapTime;

namespace EncDotNet.SoundCharts.Tests.MapTime;

/// <summary>
/// Issue #72: a layer draws the step EncDotNet.S100's
/// <see cref="TimeStepSelector"/> picks, the rule the chart server draws by,
/// on EncDotNet.S100's own time-varying test datasets. Skips without
/// <see cref="Fixtures.UpstreamDatasetsVariable"/>.
/// </summary>
public sealed class TimeStepSelectorAgreementTests
{
    /// <summary>Map times between steps, off any round number, so every rule is exercised between and on steps.</summary>
    private static readonly TimeSpan Sweep = TimeSpan.FromMinutes(7);

    [Theory]
    [InlineData("TC/al092022/al092022_5day_020.zip", TimeSelection.Continuous)]
    [InlineData("S104/104US004SC1BO_20251217T12Z.h5", TimeSelection.AtOrBefore)]
    [InlineData("S104/104US004SC1CP_20251217T12Z.h5", TimeSelection.AtOrBefore)]
    [InlineData("S111/111US00_DBOFS_20260320T18Z_US4DE1BB.h5", TimeSelection.Nearest)]
    public void Layer_DrawsTheStepTheServerDraws(string relativePath, TimeSelection expected)
    {
        var (sorted, semantics) = Read(relativePath);
        var upstreamRange = TimeStepSelector.RangeOf(semantics, sorted)!.Value;

        // As the add-in gets it: the steps from TileJSON, the selection and,
        // for a storm, the range from /datasets.
        var span = semantics.Selection == TimeStepSelection.Continuous ? upstreamRange : ((DateTime, DateTime)?)null;
        var steps = TimeSteps.Create(sorted, expected, span)!;
        Assert.Equal(semantics.Selection, TimeSelections.ToStepSelection(steps.Selection));
        if (semantics.Selection == TimeStepSelection.Continuous)
        {
            // The storm's range, past track included, is the server's.
            Assert.Equal(upstreamRange, steps.Range);
        }
        else
        {
            // The add-in's interval is the server's tolerance for these datasets.
            Assert.Equal(TimeStepSelector.ToleranceOf(semantics, sorted), steps.Interval);
        }

        var margin = TimeSpan.FromHours(2) + (2 * steps.Interval);
        for (var at = upstreamRange.Start - margin; at <= upstreamRange.End + margin; at += Sweep)
        {
            AssertAgrees(steps, sorted, semantics, at);
        }

        foreach (var step in sorted)
        {
            AssertAgrees(steps, sorted, semantics, step);
            AssertAgrees(steps, sorted, semantics, step.AddTicks(-1));
            AssertAgrees(steps, sorted, semantics, step.AddTicks(1));
        }
    }

    [Theory]
    [InlineData("TC/al092022/al092022_5day_020.zip")]
    [InlineData("S104/104US004SC1BO_20251217T12Z.h5")]
    [InlineData("S111/111US00_DBOFS_20260320T18Z_US4DE1BB.h5")]
    public void PreviousAndNextData_AreTheSelectorsSteps(string relativePath)
    {
        var (sorted, semantics) = Read(relativePath);
        var steps = TimeSteps.Create(sorted)!;
        var (start, end) = TimeStepSelector.RangeOf(semantics, sorted)!.Value;
        for (var at = start - TimeSpan.FromHours(3); at <= end + TimeSpan.FromHours(3); at += Sweep)
        {
            Assert.Equal(TimeStepSelector.Previous(sorted, at), steps.Before(at));
            Assert.Equal(TimeStepSelector.Next(sorted, at), steps.After(at));
        }
    }

    private static void AssertAgrees(TimeSteps steps, IReadOnlyList<DateTime> sorted, TimeSemantics semantics, DateTime at)
    {
        var server = TimeStepSelector.Select(sorted, semantics, at);
        var drawn = steps.Resolve(at).Step;
        var (start, end) = steps.Range;
        switch (semantics.Selection)
        {
            case TimeStepSelection.Continuous:
                // The server draws to the minute, everywhere; the add-in asks
                // for the quarter hour, in the storm's range.
                Assert.Equal(at >= start && at <= end ? TimeSteps.RoundDown(server!.Value) : null, drawn);
                break;
            case TimeStepSelection.Nearest when at < start || at > end:
                // Half an interval beyond the ends, the add-in hides a layer the server still draws (D31).
                Assert.Null(drawn);
                break;
            default:
                Assert.Equal(server, drawn);
                break;
        }
    }

    private static (DateTime[] Sorted, TimeSemantics Semantics) Read(string relativePath)
    {
        var path = Fixtures.Upstream(relativePath);
        Assert.SkipWhen(path is null, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        using var factory = (IDisposable)BundledDatasetProcessorFactory.Create();
        var processor = ((IDatasetProcessorFactory)factory).CreateProcessor(path!);
        try
        {
            var timed = Assert.IsAssignableFrom<ITimeAwareDatasetProcessor>(processor);
            var sorted = timed.AvailableTimes.Select(TimeSteps.Utc).Distinct().Order().ToArray();
            Assert.True(sorted.Length > 1);
            return (sorted, timed.TimeSemantics);
        }
        finally
        {
            (processor as IDisposable)?.Dispose();
        }
    }
}
