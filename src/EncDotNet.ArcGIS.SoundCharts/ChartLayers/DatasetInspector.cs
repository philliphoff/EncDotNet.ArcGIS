using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using EncDotNet.S100;
using EncDotNet.S100.Core;
using EncDotNet.S100.Crs.ProjNet;
using EncDotNet.S100.Datasets.Grib;
using EncDotNet.S100.Datasets.Pipelines;
using EncDotNet.S100.Datasets.S57;
using EncDotNet.S100.Datasets.TropicalCyclones;
using EncDotNet.S100.ExchangeSets;
using EncDotNet.S100.Pipelines;
using EncDotNet.SoundCharts.Charts;
using EncDotNet.SoundCharts.Resources;
using CatalogueBox = EncDotNet.S100.ExchangeSets.BoundingBox;
using PipelineBox = EncDotNet.S100.Pipelines.BoundingBox;

namespace EncDotNet.SoundCharts.ChartLayers;

/// <summary>
/// Finds the datasets in what the user chose to add, a dataset file, an
/// exchange set (folder, <c>CATALOG.XML</c>, <c>CATALOG.031</c> or
/// <c>.zip</c>) or a folder, and reads each one's product, edition and
/// footprint from its content with EncDotNet.S100, never from the file
/// extension (issue #5), and whether it changes with time.
/// </summary>
/// <remarks>
/// <para>
/// Each dataset is opened once with the library's dataset processors, the
/// same ones Object information uses: its content gives the product and
/// edition, and its metadata the footprint. The edition is assessed against
/// the editions EncDotNet.S100 supports, and an exchange set's catalogue is
/// checked against each dataset's content.
/// </para>
/// <para>
/// A folder of one storm's NHC tropical cyclone advisory zips
/// (<see cref="DatasetSource.IsAdvisoryFolder"/>) is one <c>TC</c> dataset
/// located at the folder, as the Library lists it (issue #69), whether it's
/// what the user chose or a folder inside it. Other files in it are read as
/// in any folder.
/// </para>
/// <para>
/// A folder holding one GRIB2 model run, one file per forecast hour
/// (<see cref="DatasetSource.IsRunFolder"/>), is one <c>GRIB2</c> dataset
/// located at the folder, as the Library lists it (issue #73). A storm's
/// advisory zips beside the run are the storm's latest forecast zip, a
/// dataset of its own, as in EncDotNet.S100's Library and CLI
/// (EncDotNet.S100 #1687, #1694); the run's <c>.idx</c> inventories aren't
/// counted as files that aren't datasets.
/// </para>
/// <para>
/// A <c>.zip</c> exchange set is extracted to <see cref="ExtractionFolder"/>
/// (once per zip file version), so its datasets are plain files the chart
/// server, Object information and Zoom to Data all read alike.
/// </para>
/// <para>Runs on the caller's thread and takes a while for big exchange sets: call it off Pro's UI thread and the MCT.</para>
/// </remarks>
public sealed class DatasetInspector : IDisposable
{
    private const string CompleteMarker = ".soundcharts-extracted";

    private readonly IDatasetProcessorFactory _factory;
    private readonly bool _ownsFactory;
    private readonly ICrsTransformFactory _crs = new ProjNetCrsTransformFactory();

    /// <summary>Creates an inspector that opens datasets with the catalogues bundled in EncDotNet.S100.</summary>
    /// <param name="extractionFolder">Where <c>.zip</c> exchange sets are extracted.</param>
    public DatasetInspector(string extractionFolder)
        : this(BundledDatasetProcessorFactory.Create(), extractionFolder, ownsFactory: true)
    {
    }

    /// <summary>Creates an inspector that opens datasets with <paramref name="factory"/>.</summary>
    /// <param name="factory">Creates the dataset processors.</param>
    /// <param name="extractionFolder">Where <c>.zip</c> exchange sets are extracted.</param>
    /// <param name="ownsFactory"><see langword="true"/> to dispose <paramref name="factory"/> with the inspector.</param>
    public DatasetInspector(IDatasetProcessorFactory factory, string extractionFolder, bool ownsFactory = false)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentException.ThrowIfNullOrWhiteSpace(extractionFolder);
        _factory = factory;
        _ownsFactory = ownsFactory;
        ExtractionFolder = extractionFolder;
    }

    /// <summary>Gets the folder <c>.zip</c> exchange sets are extracted to.</summary>
    public string ExtractionFolder { get; }

    /// <summary>
    /// Finds and reads the datasets in <paramref name="path"/>. Problems
    /// with single datasets become <see cref="DatasetInspection.Warnings"/>,
    /// not exceptions.
    /// </summary>
    /// <param name="path">A dataset file, an exchange set (folder, catalogue file or <c>.zip</c>) or a folder.</param>
    /// <param name="progress">Told the name of each dataset as it's read.</param>
    /// <param name="cancellationToken">Stops between datasets.</param>
    /// <exception cref="FileNotFoundException"><paramref name="path"/> doesn't exist.</exception>
    public async Task<DatasetInspection> InspectAsync(string path, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var full = Path.GetFullPath(path);
        if (!File.Exists(full) && !Directory.Exists(full))
        {
            throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture, Strings.AddData_NotFound, full), full);
        }

        var source = DatasetSource.FromPath(full);
        var warnings = new List<string>();
        var datasets = new List<ChartLayerDataset>();

        if (source.Kind == DatasetSourceKind.Dataset)
        {
            if (Inspect(full, null, warnings) is { } dataset)
            {
                datasets.Add(dataset);
            }

            return new DatasetInspection(Path.GetFileNameWithoutExtension(full), datasets, warnings);
        }

        string root;
        string input;
        if (File.Exists(full) && Path.GetExtension(full).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            input = Path.GetFileNameWithoutExtension(full);
            root = FindCatalogueRoot(Extract(full));
        }
        else if (File.Exists(full))
        {
            // CATALOG.XML or CATALOG.031: the exchange set is its folder.
            root = Path.GetDirectoryName(full)!;
            input = Path.GetFileName(root);
        }
        else
        {
            root = full;
            input = Path.GetFileName(full.TrimEnd('\\', '/'));
        }

        var catalogueXml = Path.Combine(root, "CATALOG.XML");
        var catalogue031 = Path.Combine(root, "CATALOG.031");
        if (File.Exists(catalogueXml))
        {
            await InspectS100ExchangeSetAsync(root, datasets, warnings, progress, cancellationToken).ConfigureAwait(false);
        }

        if (File.Exists(catalogue031))
        {
            InspectS57ExchangeSet(root, datasets, warnings, progress, cancellationToken);
        }

        if (!File.Exists(catalogueXml) && !File.Exists(catalogue031))
        {
            InspectFolder(root, datasets, warnings, progress, cancellationToken);
        }

        var distinct = datasets.DistinctBy(d => d.Path, StringComparer.OrdinalIgnoreCase).ToList();
        if (distinct.Count == 0 && warnings.Count == 0)
        {
            warnings.Add(string.Format(CultureInfo.CurrentCulture, Strings.AddData_NoDatasets, input));
        }

        return new DatasetInspection(input, distinct, warnings);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsFactory)
        {
            (_factory as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// Gets whether <paramref name="path"/> is an S-57 or S-101 update file
    /// (<c>.001</c> to <c>.999</c>). Updates are applied with their base cell
    /// (<c>.000</c>), never drawn on their own.
    /// </summary>
    /// <param name="path">The file's path.</param>
    public static bool IsUpdateFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Length == 4
            && extension[1..].All(char.IsAsciiDigit)
            && extension != ".000";
    }

    /// <summary>
    /// Gets the producer code in an S-57 or S-100 dataset file name: the two
    /// letters after an S-1xx file's three-digit product number
    /// (<c>101AA00DS0003</c> gives <c>AA</c>), or the first two letters of
    /// an S-57 cell name (<c>US5MA1BO</c> gives <c>US</c>); <see langword="null"/>
    /// when the name doesn't follow either pattern.
    /// </summary>
    /// <param name="path">The dataset's path.</param>
    public static string? ProducerCodeFromName(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        if (name.Length >= 5 && name[..3].All(char.IsAsciiDigit) && name[3..5].All(char.IsAsciiLetterUpper))
        {
            return name[3..5];
        }

        return name.Length >= 8 && name[..2].All(char.IsAsciiLetterUpper) && char.IsAsciiDigit(name[2])
            ? name[..2]
            : null;
    }

    /// <summary>
    /// Gets whether a dataset changes with time: its processor is
    /// time-aware (<see cref="ITimeAwareDatasetProcessor"/>) and lists more
    /// than one distinct time step. Such a dataset goes in a time layer
    /// (design brief §3.1, §6.5) whatever its product.
    /// </summary>
    /// <remarks>
    /// A single step has nothing to step through, and step snapping would
    /// hide it at almost every map time, so a dataset with one step stays in
    /// <b>Chart</b>, drawn at that step. A processor that can't list its
    /// steps counts as not time-varying.
    /// </remarks>
    /// <param name="processor">The dataset's processor.</param>
    public static bool IsTimeVarying(IDatasetProcessor processor)
    {
        ArgumentNullException.ThrowIfNull(processor);
        if (processor is not ITimeAwareDatasetProcessor timed)
        {
            return false;
        }

        try
        {
            return timed.AvailableTimes.Distinct().Skip(1).Any();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The library reads files the user chose; a malformed one can throw almost anything.
            return false;
        }
    }

    /// <summary>Reads one dataset; <see langword="null"/> with a warning when it can't be drawn.</summary>
    /// <param name="path">The file, or a folder read as one dataset.</param>
    /// <param name="declared">What an exchange set's catalogue says the file is, if anything.</param>
    /// <param name="warnings">Collects warnings.</param>
    private ChartLayerDataset? Inspect(string path, Declared? declared, List<string> warnings)
    {
        var name = Path.GetFileName(path);
        string? product;
        try
        {
            product = DatasetPipelineFactory.DetectProductSpec(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            warnings.Add(string.Format(CultureInfo.CurrentCulture, Strings.AddData_Unreadable, name, ex.Message));
            return null;
        }

        if (product is null)
        {
            warnings.Add(string.Format(CultureInfo.CurrentCulture, Strings.AddData_NotADataset, name));
            return null;
        }

        IDatasetProcessor processor;
        try
        {
            processor = _factory.CreateProcessor(path);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The library reads files the user chose; a malformed one can throw almost anything.
            warnings.Add(string.Format(CultureInfo.CurrentCulture, Strings.AddData_Unreadable, name, ex.Message));
            return null;
        }

        try
        {
            var spec = processor.Spec;
            var specName = string.IsNullOrWhiteSpace(spec.Name) ? product : spec.Name;
            var edition = spec.Edition == default ? null : spec.Edition.ToString();

            var assessment = processor.VersionAssessment ?? (edition is null ? null : SupportedSpecEditions.Assess(spec));
            if (assessment is { IsWarning: true } && assessment.BuildMessage() is { } message)
            {
                warnings.Add(string.Format(CultureInfo.CurrentCulture, Strings.AddData_Edition, name, message));
            }

            if (declared is { Product: { } saidProduct })
            {
                if (!string.Equals(saidProduct, specName, StringComparison.Ordinal))
                {
                    warnings.Add(string.Format(CultureInfo.CurrentCulture, Strings.AddData_ProductMismatch, name, saidProduct, specName));
                }
                else if (declared.Edition is { } saidEdition && saidEdition != default && edition is not null && saidEdition != spec.Edition)
                {
                    warnings.Add(string.Format(
                        CultureInfo.CurrentCulture, Strings.AddData_EditionMismatch, name, $"{saidProduct} {saidEdition}", $"{specName} {edition}"));
                }
            }

            var extent = ToExtent(TryGeographicExtent(processor.Metadata)) ?? declared?.Extent;
            return new ChartLayerDataset(
                path,
                specName,
                edition,
                extent,
                declared?.Producer ?? ProducerCodeFromName(path),
                declared?.NotForNavigation ?? false,
                IsTimeVarying(processor));
        }
        finally
        {
            (processor as IDisposable)?.Dispose();
        }
    }

    private async Task InspectS100ExchangeSetAsync(
        string root,
        List<ChartLayerDataset> datasets,
        List<string> warnings,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        using var exchangeSet = await ExchangeSet.OpenAsync(FileSystemAssetSource.Create(root), "CATALOG.XML", cancellationToken).ConfigureAwait(false);
        foreach (var entry in exchangeSet.Catalogue.CurrentDatasets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(entry.RelativePath))
            {
                continue;
            }

            var path = Path.GetFullPath(Path.Combine(root, entry.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (IsUpdateFile(path))
            {
                continue;
            }

            if (!File.Exists(path))
            {
                warnings.Add(string.Format(CultureInfo.CurrentCulture, Strings.AddData_Missing, entry.RelativePath));
                continue;
            }

            progress?.Report(Path.GetFileName(path));
            var said = Said(entry.ProductSpecification);
            var declared = new Declared(said.Product, said.Edition, ToExtent(entry.ResolveBoundingBox()), entry.ProducingAgency, entry.NotForNavigation);
            if (Inspect(path, declared, warnings) is { } dataset)
            {
                datasets.Add(dataset);
            }
        }
    }

    private void InspectS57ExchangeSet(
        string root,
        List<ChartLayerDataset> datasets,
        List<string> warnings,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        foreach (var cell in S57ExchangeSetCatalog.ReadBaseCells(root))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = Path.GetFullPath(Path.Combine(root, cell.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!File.Exists(path))
            {
                warnings.Add(string.Format(CultureInfo.CurrentCulture, Strings.AddData_Missing, cell.RelativePath));
                continue;
            }

            progress?.Report(Path.GetFileName(path));
            var declared = new Declared(null, null, ToExtent(cell.BoundingBox), null, false);
            if (Inspect(path, declared, warnings) is { } dataset)
            {
                datasets.Add(dataset);
            }
        }
    }

    private void InspectFolder(
        string root,
        List<ChartLayerDataset> datasets,
        List<string> warnings,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var skipped = 0;
        var storms = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var runs = new Dictionary<string, RunFolder?>(StringComparer.OrdinalIgnoreCase);
        var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };
        foreach (var path in Directory.EnumerateFiles(root, "*", options).Order(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsUpdateFile(path) || Path.GetFileName(path) == CompleteMarker)
            {
                continue;
            }

            // A run folder's GRIB2 files are read together, as one dataset at the folder (issue #73).
            if (RunFolderOf(path, runs) is { } run)
            {
                switch (run.Claim(path))
                {
                    case RunClaim.Run:
                        // Read once, from its first file, whether or not it can be drawn.
                        if (!run.Read)
                        {
                            run.Read = true;
                            progress?.Report(Path.GetFileName(run.Folder));
                            if (Inspect(run.Folder, null, warnings) is { } grib)
                            {
                                datasets.Add(grib);
                            }
                        }

                        continue;
                    case RunClaim.Ignored:
                        continue;
                }
            }

            // An advisory folder's zips are read together, as one dataset at the folder (issue #69).
            if (AdvisoryFolderOf(path, storms) is { } advisory)
            {
                if (datasets.All(d => !string.Equals(d.Path, advisory, StringComparison.OrdinalIgnoreCase)))
                {
                    progress?.Report(Path.GetFileName(advisory));
                    if (Inspect(advisory, null, warnings) is { } storm)
                    {
                        datasets.Add(storm);
                    }
                }

                continue;
            }

            // In a folder, files that aren't datasets (read-me files, pictures) are expected: count them, don't list them.
            string? product;
            try
            {
                product = DatasetPipelineFactory.DetectProductSpec(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                product = null;
            }

            if (product is null)
            {
                skipped++;
                continue;
            }

            progress?.Report(Path.GetFileName(path));
            if (Inspect(path, null, warnings) is { } dataset)
            {
                datasets.Add(dataset);
            }
        }

        if (skipped > 0)
        {
            warnings.Add(string.Format(CultureInfo.CurrentCulture, Strings.AddData_SkippedFiles, skipped, Path.GetFileName(root)));
        }
    }

    /// <summary>
    /// Gets the advisory folder <paramref name="path"/> belongs to: its
    /// folder, when that is an advisory folder and the file is one of its
    /// storm's NHC zips; otherwise <see langword="null"/>.
    /// </summary>
    /// <param name="path">A file in the folder being inspected.</param>
    /// <param name="storms">Each folder seen so far and its storm's ATCF id, <see langword="null"/> when it isn't an advisory folder.</param>
    private static string? AdvisoryFolderOf(string path, Dictionary<string, string?> storms)
    {
        if (!NhcAdvisoryFiles.TryParseFileName(Path.GetFileName(path), out _, out var atcfId, out _)
            || Path.GetDirectoryName(path) is not { } folder)
        {
            return null;
        }

        if (!storms.TryGetValue(folder, out var storm))
        {
            storm = DatasetSource.IsAdvisoryFolder(folder)
                && NhcAdvisoryFiles.Find(folder) is { } advisory
                && NhcAdvisoryFiles.TryParseFileName(Path.GetFileName(advisory.ForecastZip), out _, out var id, out _)
                    ? id
                    : null;
            storms[folder] = storm;
        }

        return string.Equals(storm, atcfId, StringComparison.Ordinal) ? folder : null;
    }

    /// <summary>
    /// Gets the run folder <paramref name="path"/> is in: its folder, when
    /// that is a GRIB2 run (<see cref="DatasetSource.IsRunFolder"/>);
    /// otherwise <see langword="null"/>.
    /// </summary>
    /// <param name="path">A file in the folder being inspected.</param>
    /// <param name="runs">Each folder seen so far, <see langword="null"/> when it isn't a run.</param>
    private static RunFolder? RunFolderOf(string path, Dictionary<string, RunFolder?> runs)
    {
        if (Path.GetDirectoryName(path) is not { } folder)
        {
            return null;
        }

        if (!runs.TryGetValue(folder, out var run))
        {
            run = DatasetSource.IsRunFolder(folder) ? RunFolder.Open(folder) : null;
            runs[folder] = run;
        }

        return run;
    }

    /// <summary>Extracts a <c>.zip</c> exchange set once per version of the file; returns the extracted folder.</summary>
    private string Extract(string zipPath)
    {
        var target = Path.Combine(ExtractionFolder, ExtractedName(zipPath));
        if (File.Exists(Path.Combine(target, CompleteMarker)))
        {
            return target;
        }

        var staging = target + ".partial-" + Guid.NewGuid().ToString("N")[..8];
        Directory.CreateDirectory(staging);
        try
        {
            // ExtractToDirectory refuses entries that would land outside the folder.
            ZipFile.ExtractToDirectory(zipPath, staging);
            File.WriteAllText(Path.Combine(staging, CompleteMarker), zipPath);
            if (Directory.Exists(target))
            {
                Directory.Delete(target, recursive: true);
            }

            Directory.Move(staging, target);
        }
        catch
        {
            try
            {
                Directory.Delete(staging, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Left for the next extraction of this zip to replace.
            }

            throw;
        }

        return target;
    }

    /// <summary>Gets the extraction folder name of a zip: its name and a hash of its path, size and time.</summary>
    private static string ExtractedName(string zipPath)
    {
        var info = new FileInfo(zipPath);
        var identity = string.Create(
            CultureInfo.InvariantCulture,
            $"{zipPath.ToUpperInvariant()}|{info.Length}|{info.LastWriteTimeUtc.Ticks}");
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity)).AsSpan(0, 4));
        return $"{Path.GetFileNameWithoutExtension(zipPath)}-{hash}";
    }

    /// <summary>Gets the shallowest folder under <paramref name="folder"/> holding an exchange set catalogue.</summary>
    private static string FindCatalogueRoot(string folder)
    {
        var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, MatchCasing = MatchCasing.CaseInsensitive };
        return Directory.EnumerateFiles(folder, "CATALOG.*", options)
            .Where(f => Path.GetFileName(f).Equals("CATALOG.XML", StringComparison.OrdinalIgnoreCase)
                || Path.GetFileName(f).Equals("CATALOG.031", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetDirectoryName)
            .OfType<string>()
            .OrderBy(d => d.Length)
            .FirstOrDefault()
            ?? folder;
    }

    /// <summary>
    /// Gets the product (canonical, such as <c>S-101</c>) and edition an
    /// exchange-set catalogue declares for a dataset, either or both unknown.
    /// </summary>
    private static (string? Product, SpecVersion? Edition) Said(ProductSpecification? specification)
    {
        if (specification is null)
        {
            return (null, null);
        }

        SpecVersion? edition = specification.TryToSpecRef(out var parsed) ? parsed.Edition : null;
        var product = DatasetPipelineFactory.MapProductIdentifierToSpec(specification.ProductIdentifier)
            ?? (specification.TryToSpecRef(out var named) ? named.Name : null);
        return (product, edition);
    }

    private PipelineBox? TryGeographicExtent(DatasetMetadata metadata)
    {
        try
        {
            return metadata.GetGeographicExtent(_crs);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // An unknown CRS: fall back to the catalogue's box, if any.
            return null;
        }
    }

    private static GeoExtent? ToExtent(PipelineBox? box) =>
        box is null ? null : Valid(new GeoExtent(box.WestLongitude, box.SouthLatitude, box.EastLongitude, box.NorthLatitude));

    private static GeoExtent? ToExtent(CatalogueBox? box) =>
        box is null ? null : Valid(new GeoExtent(box.WestBoundLongitude, box.SouthBoundLatitude, box.EastBoundLongitude, box.NorthBoundLatitude));

    private static GeoExtent? Valid(GeoExtent extent) => extent.IsValid ? extent : null;

    /// <summary>What an exchange set's catalogue says about a dataset.</summary>
    private sealed record Declared(string? Product, SpecVersion? Edition, GeoExtent? Extent, string? Producer, bool NotForNavigation);

    /// <summary>What a file in a GRIB2 run folder is.</summary>
    private enum RunClaim
    {
        /// <summary>One of the run's GRIB2 files.</summary>
        Run,

        /// <summary>Part of the folder's datasets without being one: an <c>.idx</c> inventory, or a storm's zip other than its latest forecast.</summary>
        Ignored,

        /// <summary>Anything else, read as in any folder, such as a storm's latest forecast zip or a chart.</summary>
        Other,
    }

    /// <summary>
    /// A GRIB2 run folder: the run's files, and each storm's latest forecast
    /// zip, which EncDotNet.S100 lists beside the run (#1694).
    /// </summary>
    private sealed class RunFolder
    {
        private readonly HashSet<string> _files;
        private readonly Dictionary<string, string> _latestForecasts;

        private RunFolder(string folder, HashSet<string> files, Dictionary<string, string> latestForecasts)
        {
            Folder = folder;
            _files = files;
            _latestForecasts = latestForecasts;
        }

        public string Folder { get; }

        /// <summary>Gets or sets whether the run has been read as a dataset.</summary>
        public bool Read { get; set; }

        public static RunFolder Open(string folder)
        {
            var files = new HashSet<string>(GribRunFolder.Files(folder), StringComparer.OrdinalIgnoreCase);
            var latest = new Dictionary<string, string>(StringComparer.Ordinal);
            IEnumerable<string> zips;
            try
            {
                zips = Directory.EnumerateFiles(folder, "*.zip").Order(StringComparer.Ordinal).ToList();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                zips = [];
            }

            foreach (var zip in zips)
            {
                if (NhcAdvisoryFiles.TryParseFileName(Path.GetFileName(zip), out var kind, out var atcfId, out var advisory)
                    && kind == NhcFileKind.Forecast
                    && (!latest.TryGetValue(atcfId, out var current) || IsLater(advisory, current)))
                {
                    latest[atcfId] = zip;
                }
            }

            return new RunFolder(folder, files, latest);
        }

        public RunClaim Claim(string path)
        {
            if (_files.Contains(path))
            {
                return RunClaim.Run;
            }

            if (path.EndsWith(".idx", StringComparison.OrdinalIgnoreCase))
            {
                return RunClaim.Ignored;
            }

            if (NhcAdvisoryFiles.TryParseFileName(Path.GetFileName(path), out _, out var atcfId, out _)
                && _latestForecasts.TryGetValue(atcfId, out var forecast))
            {
                return string.Equals(path, forecast, StringComparison.OrdinalIgnoreCase) ? RunClaim.Other : RunClaim.Ignored;
            }

            return RunClaim.Other;
        }

        private static bool IsLater(string advisory, string currentZip)
        {
            NhcAdvisoryFiles.TryParseFileName(Path.GetFileName(currentZip), out _, out _, out var current);
            var (number, suffix) = NhcAdvisoryFiles.AdvisoryOrder(advisory);
            var (currentNumber, currentSuffix) = NhcAdvisoryFiles.AdvisoryOrder(current);
            return number != currentNumber
                ? number > currentNumber
                : string.CompareOrdinal(suffix, currentSuffix) > 0;
        }
    }
}
