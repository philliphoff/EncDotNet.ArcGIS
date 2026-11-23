using System.IO;
using EncDotNet.S100;
using EncDotNet.S100.Datasets.Pipelines;
using EncDotNet.S100.Features;

namespace EncDotNet.SoundCharts.ObjectInfo;

/// <summary>
/// Runs EncDotNet.S100's pick in-process on a set of datasets at a point:
/// every feature there, with its attributes, information associations and
/// source dataset, and the S-102, S-104 and S-111 coverage values. It is the
/// add-in's counterpart of the viewer's Object information and
/// <c>s100 identify</c>.
/// </summary>
/// <remarks>
/// <para>
/// The pick uses the library's dataset processors, as the viewer's does:
/// <see cref="IDatasetProcessor.HitTestFeatures"/> finds the features,
/// <see cref="IDatasetProcessor.GetFeatureInfoAt"/> decodes their attributes
/// against the feature catalogue,
/// <see cref="IDatasetProcessor.GetFeatureGeometryAt"/> gives the geometry to
/// highlight, and <see cref="IDatasetProcessor.GetCoverageInfo"/> samples the
/// coverages. It doesn't use the Pro API, so it is tested without Pro.
/// </para>
/// <para>
/// Opened datasets are kept, up to <see cref="MaxOpenDatasets"/>, so the next
/// click doesn't parse them again; a file that changed on disk is opened
/// afresh. Calls are serialized, because a processor isn't safe to use from
/// two threads at once. Call it off Pro's UI thread and off the Main CIM
/// Thread: <see cref="IdentifyAsync"/> runs on the thread pool.
/// </para>
/// </remarks>
public sealed class ObjectInfoService : IDisposable
{
    /// <summary>The number of opened datasets kept between calls.</summary>
    public const int MaxOpenDatasets = 32;

    private readonly IDatasetProcessorFactory _factory;
    private readonly bool _ownsFactory;
    private readonly Lock _gate = new();
    private readonly Dictionary<string, OpenDataset> _open = new(StringComparer.OrdinalIgnoreCase);
    private long _clock;
    private bool _disposed;

    /// <summary>
    /// Creates a service that opens datasets with the official feature and
    /// portrayal catalogues bundled in EncDotNet.S100.
    /// </summary>
    public ObjectInfoService()
        : this(BundledDatasetProcessorFactory.Create(), ownsFactory: true)
    {
    }

    /// <summary>Creates a service that opens datasets with <paramref name="factory"/>.</summary>
    /// <param name="factory">Creates the dataset processors.</param>
    /// <param name="ownsFactory"><c>true</c> to dispose <paramref name="factory"/> with the service.</param>
    public ObjectInfoService(IDatasetProcessorFactory factory, bool ownsFactory = false)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
        _ownsFactory = ownsFactory;
    }

    /// <summary>
    /// Identifies everything at a point on the thread pool. See
    /// <see cref="Identify"/>.
    /// </summary>
    /// <param name="datasets">The datasets to look in, in the order to report them.</param>
    /// <param name="latitude">The point's latitude, WGS-84 degrees.</param>
    /// <param name="longitude">The point's longitude, WGS-84 degrees.</param>
    /// <param name="radiusMeters">How near a point or curve feature must be to count, in metres.</param>
    /// <param name="time">The instant (UTC) to sample time-varying coverages at, or <c>null</c> for each dataset's default step.</param>
    /// <param name="cancellationToken">Cancels the pick, for example when the user clicks again.</param>
    /// <returns>The report.</returns>
    public Task<ObjectInfoReport> IdentifyAsync(
        IReadOnlyList<ChartDataset> datasets,
        double latitude,
        double longitude,
        double radiusMeters,
        DateTime? time,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => Identify(datasets, latitude, longitude, radiusMeters, time, cancellationToken), cancellationToken);

    /// <summary>
    /// Identifies every feature and coverage value at a point in
    /// <paramref name="datasets"/>.
    /// </summary>
    /// <param name="datasets">The datasets to look in, in the order to report them.</param>
    /// <param name="latitude">The point's latitude, WGS-84 degrees, in [-90, 90].</param>
    /// <param name="longitude">The point's longitude, WGS-84 degrees.</param>
    /// <param name="radiusMeters">
    /// How near a point or curve feature must be to count, in metres. Area
    /// features count when they contain the point.
    /// </param>
    /// <param name="time">
    /// The instant (UTC) to sample S-104 and S-111 at, or <c>null</c> for each
    /// dataset's default step (its first time step).
    /// </param>
    /// <param name="cancellationToken">Cancels the pick between datasets and features.</param>
    /// <returns>
    /// The report. A dataset that can't be read is listed in
    /// <see cref="ObjectInfoReport.Problems"/>, not thrown.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">The latitude, longitude or radius isn't a valid value.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    public ObjectInfoReport Identify(
        IReadOnlyList<ChartDataset> datasets,
        double latitude,
        double longitude,
        double radiusMeters,
        DateTime? time,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(datasets);
        if (!double.IsFinite(latitude) || latitude is < -90.0 or > 90.0)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "The latitude must be in [-90, 90].");
        }

        if (!double.IsFinite(longitude))
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "The longitude must be a finite number.");
        }

        if (!double.IsFinite(radiusMeters) || radiusMeters < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusMeters), radiusMeters, "The radius must be zero or more.");
        }

        var utcTime = time is { } t ? DateTime.SpecifyKind(t.Kind == DateTimeKind.Local ? t.ToUniversalTime() : t, DateTimeKind.Utc) : (DateTime?)null;
        var results = new List<ObjectInfoDatasetResult>();
        var problems = new List<ObjectInfoProblem>();

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            foreach (var dataset in datasets.DistinctBy(d => d.Path, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();

                IDatasetProcessor processor;
                try
                {
                    processor = Open(dataset.Path);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // The library reads files the user chose; a malformed one can
                    // throw almost anything, and must not stop the other datasets.
                    problems.Add(new ObjectInfoProblem(dataset, ex.Message));
                    continue;
                }

                try
                {
                    var features = Pick(processor, latitude, longitude, radiusMeters, utcTime, cancellationToken);
                    if (features.Count > 0)
                    {
                        results.Add(new ObjectInfoDatasetResult(
                            dataset, processor.Spec.Name, features, InForce(dataset, processor.Spec.Name, utcTime)));
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    problems.Add(new ObjectInfoProblem(dataset, ex.Message));
                }
            }
        }

        return new ObjectInfoReport(latitude, longitude, utcTime, results, problems);
    }

    /// <summary>Releases the opened datasets and, when the service owns it, the factory.</summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (var open in _open.Values)
            {
                (open.Processor as IDisposable)?.Dispose();
            }

            _open.Clear();
            if (_ownsFactory)
            {
                (_factory as IDisposable)?.Dispose();
            }
        }
    }

    private static List<ObjectInfoFeature> Pick(
        IDatasetProcessor processor,
        double latitude,
        double longitude,
        double radiusMeters,
        DateTime? time,
        CancellationToken cancellationToken)
    {
        var features = new List<ObjectInfoFeature>();

        // Most specific first, as the library's identify ranks them: points,
        // then curves, then areas; nearer, then smaller, first within each.
        var hits = processor.HitTestFeatures(latitude, longitude, radiusMeters)
            .OrderBy(h => Rank(h.Primitive))
            .ThenBy(h => h.DistanceMeters)
            .ThenBy(h => h.Ordinal);
        foreach (var hit in hits)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var info = processor.GetFeatureInfoAt(hit.Ordinal) ?? processor.GetFeatureInfo(hit.FeatureRef);
            features.Add(new ObjectInfoFeature(
                hit.FeatureRef,
                info?.FeatureType ?? hit.FeatureType,
                info?.FeatureTypeName,
                hit.Primitive switch
                {
                    S100GeometryType.Point => ObjectInfoFeatureKind.Point,
                    S100GeometryType.Curve => ObjectInfoFeatureKind.Curve,
                    _ => ObjectInfoFeatureKind.Surface,
                },
                hit.Inside ? 0.0 : hit.DistanceMeters,
                info is null ? [] : Flatten(info.Attributes),
                info is null ? [] : References(processor, info),
                processor.GetFeatureGeometryAt(hit.Ordinal)));
        }

        if (processor.GetCoverageInfo(latitude, longitude, time) is { } coverage)
        {
            features.Add(new ObjectInfoFeature(
                coverage.FeatureRef,
                coverage.FeatureType,
                coverage.FeatureTypeName,
                ObjectInfoFeatureKind.Coverage,
                0.0,
                Flatten(coverage.Attributes),
                References(processor, coverage),
                Geometry: null));
        }

        return features;
    }

    /// <summary>
    /// Whether an S-124 dataset's warning is in force at <paramref name="time"/>
    /// (D33); <see langword="null"/> for other products, without a map time,
    /// or when the warning can't be read as a typed model.
    /// </summary>
    private static bool? InForce(ChartDataset dataset, string spec, DateTime? time)
    {
        if (time is not { } instant || !string.Equals(spec, "S-124", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            return NavigationalWarningValidity.IsInForce(dataset.Path, instant);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The features were picked; only the validity is unknown.
            return null;
        }
    }

    private static int Rank(S100GeometryType primitive) => primitive switch
    {
        S100GeometryType.Point => 0,
        S100GeometryType.Curve => 1,
        _ => 2,
    };

    /// <summary>
    /// Flattens the library's attribute tree into rows, a complex attribute
    /// before its sub-attributes.
    /// </summary>
    internal static IReadOnlyList<ObjectInfoAttribute> Flatten(IReadOnlyList<PickAttribute> attributes)
    {
        var rows = new List<ObjectInfoAttribute>();
        Add(attributes, 0);
        return rows;

        void Add(IReadOnlyList<PickAttribute> level, int depth)
        {
            foreach (var attribute in level)
            {
                rows.Add(new ObjectInfoAttribute(
                    attribute.Code,
                    attribute.DisplayName,
                    attribute.DisplayText,
                    attribute.RawValue,
                    depth,
                    attribute.HasExternalText ? attribute.ExternalText : null));
                Add(attribute.Children, depth + 1);
            }
        }
    }

    private static List<ObjectInfoReference> References(IDatasetProcessor processor, FeatureInfo info)
    {
        var references = new List<ObjectInfoReference>(info.References.Count);
        foreach (var reference in info.References)
        {
            var target = processor.GetFeatureInfo(reference.TargetRef);
            references.Add(new ObjectInfoReference(
                reference.Role,
                reference.TargetRef,
                target?.FeatureTypeName ?? target?.FeatureType ?? reference.TargetRef));
        }

        return references;
    }

    private IDatasetProcessor Open(string path)
    {
        var file = new FileInfo(path);
        if (!file.Exists)
        {
            // A dataset can be a folder (a GRIB2 run); otherwise it's missing.
            if (!Directory.Exists(path))
            {
                throw new FileNotFoundException($"The dataset file '{path}' doesn't exist.", path);
            }
        }

        var stamp = file.Exists ? (file.LastWriteTimeUtc, file.Length) : (Directory.GetLastWriteTimeUtc(path), 0L);
        if (_open.TryGetValue(file.FullName, out var open) && open.Stamp == stamp)
        {
            open.LastUsed = ++_clock;
            return open.Processor;
        }

        if (open is not null)
        {
            _open.Remove(file.FullName);
            (open.Processor as IDisposable)?.Dispose();
        }

        var processor = _factory.CreateProcessor(file.FullName);
        if (_open.Count >= MaxOpenDatasets)
        {
            var oldest = _open.MinBy(kv => kv.Value.LastUsed);
            _open.Remove(oldest.Key);
            (oldest.Value.Processor as IDisposable)?.Dispose();
        }

        _open[file.FullName] = new OpenDataset(processor, stamp) { LastUsed = ++_clock };
        return processor;
    }

    private sealed class OpenDataset(IDatasetProcessor processor, (DateTime, long) stamp)
    {
        public IDatasetProcessor Processor { get; } = processor;

        public (DateTime, long) Stamp { get; } = stamp;

        public long LastUsed { get; set; }
    }
}
