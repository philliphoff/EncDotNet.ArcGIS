using System.IO;
using EncDotNet.S100;
using EncDotNet.S100.Datasets.Pipelines;

namespace EncDotNet.SoundCharts.MapTime;

/// <summary>
/// The S-124 navigational warnings of a chart, judged together as the chart
/// server judges them (U7, issue #75): which are drawn at a time, and when
/// that can change.
/// </summary>
/// <remarks>
/// <para>
/// The server draws a warning part at a time when the library's
/// <see cref="NavigationalWarningSet.IsDrawnAt"/> says so, for the set of
/// every S-124 dataset it serves, not showing warnings not yet in force
/// (EncDotNet.S100 #1525). A warning can be ended by its own validity and
/// cancellation date, by a cancellation, or by an in-force bulletin that
/// leaves it out. This type builds the same set from the same datasets, so
/// the <b>Navigational warnings</b> layer asks for a time only when what
/// it draws changes (<see cref="Times"/>), and Object information says "In
/// force at map time" exactly when the map draws the warning
/// (<see cref="IsInForce"/>, D33).
/// </para>
/// </remarks>
public sealed class NavigationalWarnings
{
    private readonly NavigationalWarningSet _set;
    private readonly Dictionary<string, IReadOnlyList<NavigationalWarningFeature>> _parts;

    private NavigationalWarnings(NavigationalWarningSet set, Dictionary<string, IReadOnlyList<NavigationalWarningFeature>> parts, IReadOnlyList<NavigationalWarningInfo> messages)
    {
        _set = set;
        _parts = parts;
        var candidates = new WarningTimes(Changes(parts.Values.SelectMany(p => p).Select(p => p.Warning).Concat(messages)));
        Times = new WarningTimes(Distinct(candidates.Changes));
    }

    /// <summary>Gets a chart without warnings.</summary>
    public static NavigationalWarnings Empty { get; } = new(NavigationalWarningSet.Empty, new(StringComparer.OrdinalIgnoreCase), []);

    /// <summary>Gets when the warnings drawn can change, and the time segment to ask for.</summary>
    public WarningTimes Times { get; }

    /// <summary>Gets the paths of the datasets with warning parts.</summary>
    public IReadOnlyCollection<string> Paths => _parts.Keys;

    /// <summary>
    /// Builds the set from opened datasets, as the chart server does: each
    /// S-124 dataset's parts, and its warning as a whole, which may cancel
    /// or, as an in-force bulletin, end others without parts of its own.
    /// Datasets of other products are skipped.
    /// </summary>
    /// <param name="datasets">The datasets' paths and processors.</param>
    public static NavigationalWarnings From(IEnumerable<(string Path, IDatasetProcessor Processor)> datasets)
    {
        ArgumentNullException.ThrowIfNull(datasets);
        var parts = new Dictionary<string, IReadOnlyList<NavigationalWarningFeature>>(StringComparer.OrdinalIgnoreCase);
        var messages = new List<NavigationalWarningInfo>();
        foreach (var (path, processor) in datasets)
        {
            if (!IsS124(processor.Spec.Name) || parts.ContainsKey(path))
            {
                continue;
            }

            var own = processor.GetNavigationalWarnings();
            parts[path] = own;
            if (processor.GetNavigationalWarningMessage() is { } message)
            {
                messages.Add(message);
            }
            else
            {
                messages.AddRange(own.Select(p => p.Warning));
            }
        }

        return new NavigationalWarnings(new NavigationalWarningSet(messages), parts, messages);
    }

    /// <summary>
    /// Opens and reads the S-124 datasets at <paramref name="paths"/>. A
    /// dataset that can't be read is left out, as the server draws it
    /// with every warning.
    /// </summary>
    /// <param name="paths">The datasets' paths.</param>
    /// <param name="factory">Opens the datasets; the bundled catalogues when <see langword="null"/>.</param>
    /// <param name="failed">Each path that couldn't be read, and why.</param>
    public static NavigationalWarnings Read(IEnumerable<string> paths, IDatasetProcessorFactory? factory, out IReadOnlyList<(string Path, string Reason)> failed)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var owned = factory is null ? BundledDatasetProcessorFactory.Create() : null;
        var opened = new List<(string, IDatasetProcessor)>();
        var problems = new List<(string, string)>();
        try
        {
            foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    opened.Add((path, (factory ?? owned!).CreateProcessor(Path.GetFullPath(path))));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Files the user chose: a malformed one can throw almost anything.
                    problems.Add((path, ex.Message));
                }
            }

            failed = problems;
            return From(opened);
        }
        finally
        {
            foreach (var (_, processor) in opened)
            {
                (processor as IDisposable)?.Dispose();
            }

            (owned as IDisposable)?.Dispose();
        }
    }

    /// <summary>Gets whether <paramref name="spec"/> names S-124.</summary>
    /// <param name="spec">A product specification, such as <c>S-124</c>.</param>
    public static bool IsS124(string? spec) => string.Equals(spec, "S-124", StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets whether the map draws a warning part at <paramref name="instant"/>.</summary>
    /// <param name="warning">The part's warning.</param>
    /// <param name="instant">The map instant (UTC).</param>
    public bool IsDrawnAt(NavigationalWarningInfo warning, DateTime instant)
    {
        ArgumentNullException.ThrowIfNull(warning);
        return _set.IsDrawnAt(warning, new DateTimeOffset(TimeSteps.Utc(instant)), showNotYetInForce: false);
    }

    /// <summary>
    /// Gets whether the warning of the dataset at <paramref name="path"/> is
    /// in force at <paramref name="instant"/>, as the map draws it: a part
    /// among <paramref name="featureRefs"/> (what was picked) is drawn, or,
    /// when none of them is a part, any part of the dataset is.
    /// </summary>
    /// <param name="path">The S-124 dataset's path.</param>
    /// <param name="featureRefs">The features picked in it.</param>
    /// <param name="instant">The map instant (UTC).</param>
    /// <returns>Whether it's in force, or <see langword="null"/> when the dataset has no warning parts.</returns>
    public bool? IsInForce(string path, IEnumerable<string> featureRefs, DateTime instant)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(featureRefs);
        if (!_parts.TryGetValue(path, out var parts) || parts.Count == 0)
        {
            return null;
        }

        var picked = featureRefs.ToHashSet(StringComparer.Ordinal);
        var judged = parts.Where(p => picked.Contains(p.FeatureRef)).ToList();
        return (judged.Count > 0 ? judged : parts).Any(p => IsDrawnAt(p.Warning, instant));
    }

    /// <summary>
    /// The candidate instants at which the parts drawn actually change: a
    /// candidate after which the same parts draw as before it is dropped, so
    /// the layer's tile URLs change only when the warnings in force do.
    /// </summary>
    private IEnumerable<DateTime> Distinct(IReadOnlyList<DateTime> candidates)
    {
        var drawn = Drawn(WarningTimes.BeforeAll);
        foreach (var candidate in candidates)
        {
            var now = Drawn(candidate);
            if (!now.SetEquals(drawn))
            {
                yield return candidate;
                drawn = now;
            }
        }
    }

    /// <summary>The parts drawn at <paramref name="instant"/>, as path and feature reference.</summary>
    private HashSet<(string, string)> Drawn(DateTime instant) =>
        [.. _parts.SelectMany(pair => pair.Value
            .Where(p => IsDrawnAt(p.Warning, instant))
            .Select(p => (pair.Key.ToUpperInvariant(), p.FeatureRef)))];

    /// <summary>
    /// The instants at which <see cref="NavigationalWarningSet.IsDrawnAt"/>
    /// can change for any of <paramref name="warnings"/>: each one's validity
    /// starts and ends and cancellation date (<see cref="WarningValidity.Changes"/>),
    /// and the publication of every warning that cancels others or is an
    /// in-force bulletin. Nothing else the rule reads depends on the time.
    /// </summary>
    private static IEnumerable<DateTime> Changes(IEnumerable<NavigationalWarningInfo> warnings)
    {
        foreach (var warning in warnings)
        {
            foreach (var change in WarningValidity.Changes(warning.Validity, warning.CancellationDate))
            {
                yield return change.UtcDateTime;
            }

            if ((warning.Cancels.Count > 0 || warning.InForceBulletin is not null) && warning.PublicationTime is { } published)
            {
                yield return published.UtcDateTime;
            }
        }
    }
}
