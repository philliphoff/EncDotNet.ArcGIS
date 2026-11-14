using System.IO;
using System.IO.Compression;
using System.Xml;
using EncDotNet.S100.Datasets.Pipelines;
using EncDotNet.SoundCharts.ChartLayers;

namespace EncDotNet.SoundCharts.OpenWith;

/// <summary>
/// Tells quickly whether a Catalog item, a file or a folder, holds data
/// SoundCharts can draw, from its content rather than its extension
/// (issue #6). <b>Open with SoundCharts</b> shows only for items it accepts.
/// </summary>
/// <remarks>
/// <para>
/// The probe runs while a context menu opens, on Pro's UI thread, so it
/// stops at the first dataset or exchange-set catalogue it finds and has a
/// file count, a folder depth and a time budget. A folder whose data lies
/// deeper than that is reported as <see cref="ContentProbeKind.None"/>;
/// <b>Add SoundCharts Data</b> still reads it. Results are cached by path and
/// last write time.
/// </para>
/// <para>
/// It answers "is there chart data here", not "what is all of it": adding
/// the item to a map reads it fully with <see cref="DatasetInspector"/>, the
/// same path as <b>Add SoundCharts Data</b>.
/// </para>
/// </remarks>
public sealed class ContentProbe
{
    /// <summary>The most files a folder probe reads before giving up.</summary>
    public const int DefaultMaxFiles = 256;

    /// <summary>
    /// How deep below a folder the probe looks. Three levels reach the cells
    /// of an S-57 exchange set (<c>ENC_ROOT/US5MA1BO/US5MA1BO.000</c>) from the
    /// folder that holds it.
    /// </summary>
    public const int DefaultMaxDepth = 3;

    /// <summary>The most entries the cache holds before it starts again.</summary>
    private const int CacheCapacity = 128;

    /// <summary>
    /// The S-100 exchange catalogue's root element (S-100 Edition 5.x
    /// Part 17, exchange catalogue XML schema).
    /// </summary>
    private const string S100CatalogueRoot = "S100_ExchangeCatalogue";

    private static readonly EnumerationOptions Shallow = new() { IgnoreInaccessible = true, RecurseSubdirectories = false };

    private readonly Func<string, string?> _detectProduct;
    private readonly TimeProvider _time;
    private readonly Dictionary<(string Path, long Stamp), ContentProbeResult> _cache = [];
    private readonly Lock _cacheLock = new();

    /// <summary>
    /// Creates a probe that detects products with EncDotNet.S100's
    /// <see cref="DatasetPipelineFactory.DetectProductSpec(string)"/>.
    /// </summary>
    public ContentProbe()
        : this(DatasetPipelineFactory.DetectProductSpec)
    {
    }

    /// <summary>Creates a probe with its own product detection and limits.</summary>
    /// <param name="detectProduct">Gets a file's product from its content, or <see langword="null"/> when it isn't a dataset.</param>
    /// <param name="maxFiles">The most files a folder probe reads.</param>
    /// <param name="maxDepth">How many folder levels below a folder the probe looks.</param>
    /// <param name="budget">The longest a folder probe runs; <see langword="null"/> for 250 ms.</param>
    /// <param name="timeProvider">Measures the budget; <see langword="null"/> for the system clock.</param>
    public ContentProbe(
        Func<string, string?> detectProduct,
        int maxFiles = DefaultMaxFiles,
        int maxDepth = DefaultMaxDepth,
        TimeSpan? budget = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(detectProduct);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFiles);
        ArgumentOutOfRangeException.ThrowIfNegative(maxDepth);
        _detectProduct = detectProduct;
        MaxFiles = maxFiles;
        MaxDepth = maxDepth;
        Budget = budget ?? TimeSpan.FromMilliseconds(250);
        _time = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Gets the most files a folder probe reads.</summary>
    public int MaxFiles { get; }

    /// <summary>Gets how many folder levels below a folder the probe looks.</summary>
    public int MaxDepth { get; }

    /// <summary>Gets the longest a folder probe runs.</summary>
    public TimeSpan Budget { get; }

    /// <summary>Probes <paramref name="path"/>. Never throws for what is on disk.</summary>
    /// <param name="path">A file or folder.</param>
    /// <returns>What the path holds; <see cref="ContentProbeKind.None"/> when it doesn't exist.</returns>
    public ContentProbeResult Probe(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new ContentProbeResult(path, ContentProbeKind.None, null);
        }

        var isFolder = Directory.Exists(full);
        if (!isFolder && !File.Exists(full))
        {
            return new ContentProbeResult(full, ContentProbeKind.None, null);
        }

        var key = (full.ToUpperInvariant(), Stamp(full, isFolder));
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                return cached;
            }
        }

        var result = isFolder ? ProbeFolder(full) : ProbeFile(full);
        lock (_cacheLock)
        {
            if (_cache.Count >= CacheCapacity)
            {
                _cache.Clear();
            }

            _cache[key] = result;
        }

        return result;
    }

    /// <summary>
    /// Probes each of <paramref name="paths"/>, a Catalog selection, and
    /// keeps those holding chart data, once each, in their order.
    /// </summary>
    /// <param name="paths">Files and folders.</param>
    /// <returns>The paths with chart data; empty when there are none.</returns>
    public IReadOnlyList<ContentProbeResult> ChartData(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        return paths
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(Probe)
            .Where(r => r.IsChartData)
            .DistinctBy(r => r.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private ContentProbeResult ProbeFile(string path)
    {
        if (IsExchangeSetCatalogue(path) || IsZipExchangeSet(path))
        {
            return new ContentProbeResult(path, ContentProbeKind.ExchangeSet, null);
        }

        return Detect(path) is { } product
            ? new ContentProbeResult(path, ContentProbeKind.Dataset, product)
            : new ContentProbeResult(path, ContentProbeKind.None, null);
    }

    private ContentProbeResult ProbeFolder(string root)
    {
        var started = _time.GetTimestamp();
        var read = 0;
        var folders = new Queue<(string Path, int Depth)>();
        folders.Enqueue((root, 0));
        while (folders.TryDequeue(out var folder))
        {
            if (HasExchangeSetCatalogue(folder.Path))
            {
                // A3 reads a folder holding an exchange set below it as a folder of datasets.
                return new ContentProbeResult(root, folder.Depth == 0 ? ContentProbeKind.ExchangeSet : ContentProbeKind.Folder, null);
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(folder.Path, "*", Shallow);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files)
            {
                if (++read > MaxFiles || _time.GetElapsedTime(started) > Budget)
                {
                    return new ContentProbeResult(root, ContentProbeKind.None, null);
                }

                if (Detect(file) is { } product)
                {
                    return new ContentProbeResult(root, ContentProbeKind.Folder, product);
                }
            }

            if (folder.Depth < MaxDepth)
            {
                try
                {
                    foreach (var child in Directory.EnumerateDirectories(folder.Path, "*", Shallow))
                    {
                        folders.Enqueue((child, folder.Depth + 1));
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // An unreadable subfolder is skipped, as Add SoundCharts Data skips it.
                }
            }
        }

        return new ContentProbeResult(root, ContentProbeKind.None, null);
    }

    /// <summary>Gets a file's product, or <see langword="null"/>; update files (<c>.001</c>…) are never datasets on their own.</summary>
    private string? Detect(string path)
    {
        if (DatasetInspector.IsUpdateFile(path))
        {
            return null;
        }

        try
        {
            return _detectProduct(path);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The library reads whatever file the user right-clicked; anything it can't read isn't chart data.
            return null;
        }
    }

    private static bool HasExchangeSetCatalogue(string folder) =>
        IsExchangeSetCatalogue(Path.Combine(folder, "CATALOG.XML")) || IsExchangeSetCatalogue(Path.Combine(folder, "CATALOG.031"));

    /// <summary>
    /// Gets whether <paramref name="path"/> is an S-100 <c>CATALOG.XML</c>
    /// (its root element is the exchange catalogue) or an S-57
    /// <c>CATALOG.031</c> (an ISO/IEC 8211 file).
    /// </summary>
    private static bool IsExchangeSetCatalogue(string path)
    {
        var name = Path.GetFileName(path);
        if (name.Equals("CATALOG.XML", StringComparison.OrdinalIgnoreCase))
        {
            return HasXmlRoot(path, S100CatalogueRoot);
        }

        return name.Equals("CATALOG.031", StringComparison.OrdinalIgnoreCase) && IsIso8211(path);
    }

    private static bool HasXmlRoot(string path, string localName)
    {
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                IgnoreWhitespace = true,
            };
            using var reader = XmlReader.Create(path, settings);
            return reader.MoveToContent() == XmlNodeType.Element && reader.LocalName == localName;
        }
        catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets whether a file starts with an ISO/IEC 8211 data descriptive record
    /// leader: a five-digit record length and the leader identifier <c>L</c>
    /// at offset 6 (ISO/IEC 8211 §6.1.2, used by S-57 Part 3 §7).
    /// </summary>
    private static bool IsIso8211(string path)
    {
        try
        {
            Span<byte> leader = stackalloc byte[24];
            using var stream = File.OpenRead(path);
            if (stream.ReadAtLeast(leader, leader.Length, throwOnEndOfStream: false) < leader.Length)
            {
                return false;
            }

            for (var i = 0; i < 5; i++)
            {
                if (!char.IsAsciiDigit((char)leader[i]))
                {
                    return false;
                }
            }

            return leader[6] == (byte)'L';
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>Gets whether a <c>.zip</c> holds an exchange-set catalogue, from its entry names alone.</summary>
    private static bool IsZipExchangeSet(string path)
    {
        if (!Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            using var zip = ZipFile.OpenRead(path);
            return zip.Entries.Any(e =>
                e.Name.Equals("CATALOG.XML", StringComparison.OrdinalIgnoreCase)
                || e.Name.Equals("CATALOG.031", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static long Stamp(string path, bool isFolder)
    {
        try
        {
            return isFolder ? Directory.GetLastWriteTimeUtc(path).Ticks : File.GetLastWriteTimeUtc(path).Ticks;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return 0;
        }
    }
}
