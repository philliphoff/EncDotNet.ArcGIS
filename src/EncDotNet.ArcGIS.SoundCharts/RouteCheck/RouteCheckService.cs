using System.Globalization;
using System.IO;
using EncDotNet.S100;
using EncDotNet.S100.Crs.ProjNet;
using EncDotNet.S100.Datasets.Pipelines;
using EncDotNet.S100.Datasets.Pipelines.Catalog;
using EncDotNet.S100.Datasets.Pipelines.Navigation;
using EncDotNet.S100.Navigation;
using EncDotNet.S100.Pipelines;
using EncDotNet.S100.Scripting;
using EncDotNet.SoundCharts.ObjectInfo;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts.RouteCheck;

/// <summary>
/// Checks routes against the datasets of a map's SoundCharts layers with
/// EncDotNet.S100's <see cref="RouteChecker"/>, in-process and off the UI
/// thread (design brief §1.1: the library runs in-process for route checks).
/// </summary>
/// <remarks>
/// <para>
/// The datasets are loaded once per set of files (path, size and time), and
/// the route-check sources built from them are kept with them, so checking a
/// changed route against the same chart doesn't reload it. One check runs at a
/// time.
/// </para>
/// <para>
/// Findings are worded by the library's <see cref="RouteFindingFormatter"/>,
/// the viewer's English route-check wording.
/// </para>
/// </remarks>
public sealed class RouteCheckService
{
    // The viewer's footer names these products, in this order
    // (RouteCheckCard.FooterProducts). S-57 is the add-in's: the catalog
    // keeps S-57 cells as S-57, and a chart of S-57 cells isn't "no data".
    private static readonly string[] FooterProducts = ["S-57", "S-101", "S-102", "S-104", "S-122", "S-124"];

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ProjNetCrsTransformFactory _transforms = new();
    private readonly Lazy<ILuaEngine> _luaEngine = new(() => LuaEngineFactory.Create(), LazyThreadSafetyMode.ExecutionAndPublication);
    private LoadedCatalog? _catalog;

    /// <summary>Checks a route on the thread pool.</summary>
    /// <param name="route">The route.</param>
    /// <param name="datasets">The datasets of the map's SoundCharts layers.</param>
    /// <param name="ship">The Options page's ship parameters.</param>
    /// <param name="now">The time to check at when the route has no departure time.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The check's outcome.</returns>
    public Task<RouteCheckOutcome> CheckAsync(
        RouteInput route,
        IReadOnlyList<ChartDataset> datasets,
        ShipParameters ship,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => Check(route, datasets, ship, now, cancellationToken), cancellationToken);

    /// <summary>Checks a route on the calling thread.</summary>
    /// <param name="route">The route.</param>
    /// <param name="datasets">The datasets of the map's SoundCharts layers.</param>
    /// <param name="ship">The Options page's ship parameters.</param>
    /// <param name="now">The time to check at when the route has no departure time.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The check's outcome.</returns>
    public RouteCheckOutcome Check(
        RouteInput route,
        IReadOnlyList<ChartDataset> datasets,
        ShipParameters ship,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(datasets);
        ArgumentNullException.ThrowIfNull(ship);

        _gate.Wait(cancellationToken);
        try
        {
            var prepared = RouteCheckPlanning.Prepare(route, ship, now);
            var catalog = Load(datasets, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var checker = new RouteChecker(catalog.Sources.Get(catalog.Datasets), new RouteCheckOptions
            {
                Formatter = new RouteFindingFormatter(CultureInfo.CurrentCulture),
            });
            var report = checker.Check(prepared.Plan, prepared.Settings, cancellationToken);
            return new RouteCheckOutcome(route, prepared, report, Products(report, catalog.Datasets, prepared.Plan), catalog.Problems);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// The products the footer names, as the viewer's route check does
    /// (<c>RouteCheckCard.Products</c>): the clearance check's depth and water
    /// level sources, the charts, depths, ice and warnings near the route,
    /// then weather and cyclone advisories when the check read them. S-57
    /// cells near the route are named too.
    /// </summary>
    internal static IReadOnlyList<string> Products(RouteCheckReport report, IReadOnlyList<LoadedDataset> datasets, PassagePlan plan)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (report.Clearance?.Sources is { } sources)
        {
            if (sources.BathymetryDatasets.Count > 0)
            {
                used.Add("S-102");
            }

            if (sources.FallbackDatasets.Count > 0)
            {
                used.Add("S-101");
            }

            if (sources.WaterLevelSources.Count > 0)
            {
                used.Add("S-104");
            }
        }

        var extent = Extent(plan);
        foreach (var dataset in datasets)
        {
            if (extent is not null && Overlaps(dataset.Bounds, extent.Value)
                && FooterProducts.FirstOrDefault(p => string.Equals(p, dataset.Spec.Name, StringComparison.OrdinalIgnoreCase)) is { } product
                && product != "S-104")
            {
                used.Add(product);
            }
        }

        List<string> products = [.. FooterProducts.Where(used.Contains)];
        if (report.Weather is { DatasetsUsed.Count: > 0 })
        {
            products.Add(Strings.RouteCheck_FooterWeather);
        }

        if (report.Weather is { Cyclones.Count: > 0 })
        {
            products.Add(Strings.RouteCheck_FooterCyclones);
        }

        return products;
    }

    private LoadedCatalog Load(IReadOnlyList<ChartDataset> datasets, CancellationToken cancellationToken)
    {
        var files = datasets
            .DistinctBy(d => Path.GetFullPath(d.Path), StringComparer.OrdinalIgnoreCase)
            .Select(d => (Dataset: d, Stamp: Stamp(d.Path)))
            .ToList();
        var key = string.Join('|', files.Select(f => $"{Path.GetFullPath(f.Dataset.Path)}*{f.Stamp}"));
        if (_catalog is { } cached && cached.Key == key)
        {
            return cached;
        }

        List<string> problems = [];
        List<FileDatasetInput> inputs = [];
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (dataset, _) in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? spec;
            try
            {
                spec = DatasetPipelineFactory.DetectProductSpec(dataset.Path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                problems.Add(string.Format(CultureInfo.CurrentCulture, Strings.RouteCheck_DatasetProblemFormat, dataset.Name, ex.Message));
                continue;
            }

            if (spec is null)
            {
                continue;
            }

            // Findings name a feature's dataset by this id; the dataset's name
            // reads best, and the path keeps two same-named files apart.
            var id = ids.Add(dataset.Name) ? dataset.Name : dataset.Path;
            ids.Add(id);
            inputs.Add(new FileDatasetInput(new DatasetId(id), spec, dataset.Path));
        }

        var catalog = FileDatasetCatalog.Build(inputs, _transforms, _luaEngine.Value);
        problems.AddRange(catalog.Warnings);
        var loaded = new LoadedCatalog(key, catalog.Datasets, new RouteCheckSourceCache(_transforms), problems);
        _catalog = loaded;
        return loaded;
    }

    private static string Stamp(string path)
    {
        var file = new FileInfo(path);
        return file.Exists
            ? $"{file.Length}@{file.LastWriteTimeUtc.Ticks}"
            : Directory.Exists(path) ? $"dir@{Directory.GetLastWriteTimeUtc(path).Ticks}" : "missing";
    }

    private static (double South, double West, double North, double East)? Extent(PassagePlan plan)
    {
        var points = plan.Legs.SelectMany(l => l.Points).ToList();
        return points.Count == 0
            ? null
            : (points.Min(p => p.Latitude), points.Min(p => p.Longitude), points.Max(p => p.Latitude), points.Max(p => p.Longitude));
    }

    private static bool Overlaps(BoundingBox? bounds, (double South, double West, double North, double East) extent) =>
        bounds is not null
        && bounds.SouthLatitude <= extent.North && bounds.NorthLatitude >= extent.South
        && bounds.WestLongitude <= extent.East && bounds.EastLongitude >= extent.West;

    private sealed record LoadedCatalog(
        string Key,
        IReadOnlyList<LoadedDataset> Datasets,
        RouteCheckSourceCache Sources,
        IReadOnlyList<string> Problems);
}
