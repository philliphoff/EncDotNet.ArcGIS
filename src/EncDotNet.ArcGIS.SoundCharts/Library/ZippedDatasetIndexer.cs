using System.Globalization;
using System.IO;
using EncDotNet.S100.Collections;
using EncDotNet.S100.Collections.Indexing;
using EncDotNet.SoundCharts.Charts;

namespace EncDotNet.SoundCharts.Library;

/// <summary>
/// Indexes local folders and exchange sets as EncDotNet.S100's
/// <see cref="LocalSourceIndexer"/> does, and adds the <c>.zip</c> files it
/// reads only as exchange sets but that are datasets of their own, such as
/// an NHC tropical cyclone advisory's forecast zip (issue #63).
/// </summary>
/// <remarks>
/// <para>
/// EncDotNet.S100's indexer looks in every <c>.zip</c> for an exchange-set
/// catalogue, and gives an advisory zip, which has none, no items. This
/// indexer keeps everything it finds and adds an item for each zip
/// <see cref="DatasetSource.IsZippedDataset"/> accepts, read through the
/// same <see cref="DatasetProbe"/> as a loose dataset. The item's file is
/// the zip itself, so <b>Add to Map</b> adds it as a dataset. An advisory's
/// wind radii and best track zips aren't datasets of their own and stay
/// unlisted.
/// </para>
/// <para>
/// The fingerprint is the inner indexer's, which already covers each
/// <c>.zip</c> it finds.
/// </para>
/// </remarks>
public sealed class ZippedDatasetIndexer : ICollectionSourceIndexer
{
    private readonly ICollectionSourceIndexer _inner;
    private readonly DatasetProbe? _probe;

    /// <summary>Creates an indexer over <paramref name="inner"/>.</summary>
    /// <param name="inner">EncDotNet.S100's indexer of local folders and exchange sets.</param>
    /// <param name="probe">Reads a zipped dataset's metadata; <see langword="null"/> lists no zipped datasets.</param>
    public ZippedDatasetIndexer(ICollectionSourceIndexer inner, DatasetProbe? probe)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
        _probe = probe;
    }

    /// <inheritdoc/>
    public bool CanIndex(CollectionSource source) => _inner.CanIndex(source);

    /// <inheritdoc/>
    public ValueTask<string?> GetFingerprintAsync(CollectionSource source, CancellationToken cancellationToken) =>
        _inner.GetFingerprintAsync(source, cancellationToken);

    /// <inheritdoc/>
    public async ValueTask<SourceIndex> IndexAsync(
        CollectionSource source,
        IProgress<IndexProgress>? progress,
        CancellationToken cancellationToken)
    {
        var index = await _inner.IndexAsync(source, progress, cancellationToken).ConfigureAwait(false);
        if (_probe is null || Find(source) is not { Root: var root, Zips.Count: > 0 } found)
        {
            return index;
        }

        var items = new List<CollectionItem>(index.Items);
        var diagnostics = index.Diagnostics
            .Where(d => d.Path is null || !found.Zips.Contains(d.Path, StringComparer.OrdinalIgnoreCase))
            .ToList();
        foreach (var zip in found.Zips)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new IndexProgress(items.Count, zip));
            if (Read(root, zip, diagnostics, cancellationToken) is { } item)
            {
                items.Add(item);
            }
        }

        return index with { Items = items, Diagnostics = diagnostics };
    }

    private static (string Root, IReadOnlyList<string> Zips)? Find(CollectionSource source)
    {
        try
        {
            switch (source)
            {
                case ExchangeSetSource set when File.Exists(set.Path):
                    var full = Path.GetFullPath(set.Path);
                    return (Path.GetDirectoryName(full)!, DatasetSource.IsZippedDataset(full) ? [full] : []);
                case LocalFolderSource folder when Directory.Exists(folder.Path):
                    var root = Path.GetFullPath(folder.Path);
                    return (root, ZipsIn(root, folder.Recursive));
                default:
                    return null;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static List<string> ZipsIn(string root, bool recursive)
    {
        var options = new EnumerationOptions { RecurseSubdirectories = recursive, IgnoreInaccessible = true };
        return Directory.EnumerateFiles(root, "*.zip", options)
            .Where(zip => !IsUnderDotFolder(root, zip) && DatasetSource.IsZippedDataset(zip))
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    // EncDotNet.S100's scanner doesn't look in folders whose names start with a dot.
    private static bool IsUnderDotFolder(string root, string file) =>
        Path.GetRelativePath(root, Path.GetDirectoryName(file)!)
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(part => part.StartsWith('.') && part != ".");

    private CollectionItem? Read(string root, string zip, List<IndexDiagnostic> diagnostics, CancellationToken cancellationToken)
    {
        S100.Core.DatasetMetadata? metadata;
        try
        {
            metadata = _probe!(zip, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            diagnostics.Add(new IndexDiagnostic(IndexDiagnosticSeverity.Warning, ex.Message, zip));
            return null;
        }

        if (metadata is null)
        {
            diagnostics.Add(new IndexDiagnostic(IndexDiagnosticSeverity.Info, "Not a recognised dataset.", zip));
            return null;
        }

        var properties = new Dictionary<string, string>(StringComparer.Ordinal);
        if (metadata.TimeCoverage is { } time)
        {
            properties["timeStart"] = time.Start.ToString("O", CultureInfo.InvariantCulture);
            properties["timeEnd"] = time.End.ToString("O", CultureInfo.InvariantCulture);
        }

        GeoBounds? bounds = metadata is { Extent: { } extent, HorizontalCrsEpsg: null or 4326 }
            ? new GeoBounds(extent.SouthLatitude, extent.WestLongitude, extent.NorthLatitude, extent.EastLongitude)
            : null;

        return new CollectionItem
        {
            Key = Path.GetRelativePath(root, zip).Replace('\\', '/'),
            ProductSpec = metadata.Spec.Name,
            ProductSpecVersion = metadata.Spec.Edition == default ? null : metadata.Spec.Edition.ToString(),
            Name = Path.GetFileNameWithoutExtension(zip),
            MinimumDisplayScale = metadata.DisplayScale?.Minimum,
            MaximumDisplayScale = metadata.DisplayScale?.Maximum,
            Bounds = bounds,
            Location = new LocalItemLocation(Path.GetDirectoryName(zip)!, Path.GetFileName(zip), []),
            Properties = properties,
        };
    }
}
