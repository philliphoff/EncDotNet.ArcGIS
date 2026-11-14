using System.IO;
using EncDotNet.S100;
using EncDotNet.S100.Collections;
using EncDotNet.S100.Core;
using EncDotNet.S100.Datasets.Pipelines;

namespace EncDotNet.SoundCharts.Library;

/// <summary>
/// Reads the little the Library needs from one dataset file in-process,
/// with the same dataset processors as Object information: the metadata of
/// a loose dataset while it's indexed (<see cref="Probe"/>), and the time
/// steps (<see cref="ReadTimeSteps"/>) and extent (<see cref="ReadFootprint"/>)
/// of the dataset the user selects.
/// </summary>
/// <remarks>
/// Nothing is drawn or kept open: each call opens the file, reads, and
/// closes it. Never throws for a bad file; a file that can't be read gives
/// <see langword="null"/>. Call it off Pro's UI thread and the MCT.
/// </remarks>
public sealed class LibraryDatasetReader : IDisposable
{
    private readonly IDatasetProcessorFactory _factory;
    private readonly bool _ownsFactory;

    /// <summary>Creates a reader with the catalogues bundled in EncDotNet.S100.</summary>
    public LibraryDatasetReader()
        : this(BundledDatasetProcessorFactory.Create(), ownsFactory: true)
    {
    }

    /// <summary>Creates a reader that opens datasets with <paramref name="factory"/>.</summary>
    /// <param name="factory">Creates the dataset processors.</param>
    /// <param name="ownsFactory"><see langword="true"/> to dispose <paramref name="factory"/> with the reader.</param>
    public LibraryDatasetReader(IDatasetProcessorFactory factory, bool ownsFactory = false)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
        _ownsFactory = ownsFactory;
    }

    /// <summary>
    /// Reads a loose dataset's metadata for the index (product, edition,
    /// extent, display scales, time coverage), the
    /// <see cref="S100.Collections.Indexing.DatasetProbe"/> the library calls.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <param name="cancellationToken">Not observed: one file is read at a time.</param>
    /// <returns>The metadata, or <see langword="null"/> when the file isn't a dataset or can't be read.</returns>
    public DatasetMetadata? Probe(string path, CancellationToken cancellationToken)
    {
        if (!IsCandidate(path))
        {
            return null;
        }

        return Read(path, processor => processor.Metadata);
    }

    /// <summary>
    /// Reads the times a time-varying dataset (S-104 water level, S-111
    /// surface currents) has data for, in UTC.
    /// </summary>
    /// <param name="path">The dataset file.</param>
    /// <returns>The times in order; empty for a dataset without time steps, or one that can't be read.</returns>
    public IReadOnlyList<DateTime> ReadTimeSteps(string path)
    {
        if (!IsCandidate(path))
        {
            return [];
        }

        return Read(path, processor => processor is ITimeAwareDatasetProcessor timed
            ? timed.AvailableTimes.Order().ToArray()
            : null) ?? [];
    }

    /// <summary>
    /// Reads a dataset's extent from its own metadata, for <b>Show
    /// Footprint</b> when the Library's index has none (an exchange set
    /// whose catalogue gives no bounds).
    /// </summary>
    /// <param name="path">The dataset file.</param>
    /// <returns>The bounding box, or <see langword="null"/> when the dataset has no geographic extent or can't be read.</returns>
    public LibraryFootprint? ReadFootprint(string path)
    {
        if (!IsCandidate(path))
        {
            return null;
        }

        return Read(path, processor => processor.Metadata) is { Extent: { } extent, HorizontalCrsEpsg: null or 4326 }
            ? LibraryFootprint.FromBounds(new GeoBounds(extent.SouthLatitude, extent.WestLongitude, extent.NorthLatitude, extent.EastLongitude))
            : null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsFactory)
        {
            (_factory as IDisposable)?.Dispose();
        }
    }

    private static bool IsCandidate(string path)
    {
        try
        {
            return File.Exists(path) && DatasetPipelineFactory.DetectProductSpec(path) is not null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private T? Read<T>(string path, Func<IDatasetProcessor, T?> read)
        where T : class
    {
        IDatasetProcessor processor;
        try
        {
            processor = _factory.CreateProcessor(path);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The library reads files the user chose; a malformed one can throw almost anything.
            return null;
        }

        try
        {
            return read(processor);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
        finally
        {
            (processor as IDisposable)?.Dispose();
        }
    }
}
