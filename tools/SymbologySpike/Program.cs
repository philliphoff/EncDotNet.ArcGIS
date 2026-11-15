using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using EncDotNet.S100;
using EncDotNet.S100.Datasets.Pipelines;
using EncDotNet.S100.Datasets.Pipelines.Portrayal;
using EncDotNet.S100.Pipelines;
using EncDotNet.S100.Quantities;

namespace EncDotNet.SoundCharts.SymbologySpike;

/// <summary>
/// A14 spike (#16): portrays one vector dataset with EncDotNet.S100 in
/// process and writes its drawing instructions as feature data (GeoJSON,
/// one row per instruction) plus Pro layer files whose CIM renderers draw
/// them. <c>tools/SymbologySpike/render_layout.py</c> loads them into a
/// file geodatabase and exports a layout with arcpy.
/// </summary>
/// <remarks>
/// Usage: <c>SymbologySpike &lt;dataset&gt; &lt;output folder&gt; [--palette day|dusk|night]
/// [--safety-contour metres] [--category all|standard|base|other]</c>.
/// </remarks>
internal static class Program
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: SymbologySpike <dataset> <output folder> [--palette day] [--safety-contour 30]");
            return 2;
        }

        var dataset = Path.GetFullPath(args[0]);
        var output = Directory.CreateDirectory(Path.GetFullPath(args[1])).FullName;
        var palette = PaletteType.Day;
        var safetyContour = 30.0;
        var sizes = SizeProfile.True;
        string? symbolsDir = null;
        for (var i = 2; i + 1 < args.Length; i += 2)
        {
            switch (args[i])
            {
                case "--palette":
                    palette = Enum.Parse<PaletteType>(args[i + 1], ignoreCase: true);
                    break;
                case "--symbols-dir":
                    symbolsDir = args[i + 1];
                    break;
                case "--sizes":
                    sizes = args[i + 1] == "raster" ? SizeProfile.Raster : SizeProfile.True;
                    break;
                case "--safety-contour":
                    safetyContour = double.Parse(args[i + 1], CultureInfo.InvariantCulture);
                    break;
                default:
                    break;
            }
        }

        var timings = new JsonObject();
        var watch = Stopwatch.StartNew();
        using var factory = BundledDatasetProcessorFactory.Create();
        var processor = factory.CreateProcessorWithFilesystemUpdates(dataset);
        timings["openMs"] = watch.ElapsedMilliseconds;
        if (processor is not IVectorPortrayalSource source)
        {
            Console.Error.WriteLine($"{processor.Spec.Name} isn't a vector product.");
            return 3;
        }

        var mariner = MarinerSettings.Default with
        {
            SafetyContour = Depth.FromMetres(safetyContour),
            SafetyDepth = Depth.FromMetres(safetyContour),
            DeepContour = Depth.FromMetres(safetyContour),
        };

        RenderContext Context(EcdisDisplayCategory category, MarinerSettings settings) => new()
        {
            Palette = palette,
            Mariner = settings,
            EcdisDisplay = new EcdisDisplaySettings { Category = category },
        };

        // Every viewing group: the display category becomes a definition
        // query on the ViewingGroup field, not a re-portrayal.
        watch.Restart();
        var portrayal = await source.BuildVectorPortrayalAsync(Context(EcdisDisplayCategory.All, mariner));
        timings["portrayFirstMs"] = watch.ElapsedMilliseconds;

        // What a mariner setting change costs: the Lua rules again.
        watch.Restart();
        _ = await source.BuildVectorPortrayalAsync(Context(EcdisDisplayCategory.All, mariner with { SafetyContour = Depth.FromMetres(safetyContour / 2) }));
        timings["portrayAgainMs"] = watch.ElapsedMilliseconds;

        // The viewing groups in each display category, for the queries.
        var categories = new JsonObject();
        foreach (var category in new[] { EcdisDisplayCategory.DisplayBase, EcdisDisplayCategory.Standard, EcdisDisplayCategory.OtherInformation })
        {
            var filtered = await source.BuildVectorPortrayalAsync(Context(category, mariner));
            var groups = filtered.SubLayers.SelectMany(l => l.Instructions).Select(i => i.ViewingGroup).Distinct().Order();
            categories[category.ToString()] = new JsonArray([.. groups.Select(g => (JsonNode)g)]);
        }

        watch.Restart();
        var export = new PortrayalExport(portrayal, sizes);
        export.Build();
        timings["convertMs"] = watch.ElapsedMilliseconds;

        var gdb = Path.Combine(output, "spike.gdb");
        export.Write(output, gdb);

        var summary = export.Summary();
        summary["dataset"] = Path.GetFileName(dataset);
        summary["product"] = processor.Spec.Name;
        summary["palette"] = palette.ToString();
        summary["safetyContour"] = safetyContour;
        summary["sizes"] = sizes.Name;
        summary["timings"] = timings;
        summary["categoryViewingGroups"] = categories;
        if (symbolsDir is not null)
        {
            summary["catalogueSymbols"] = PortrayalExport.ConvertAll(portrayal, Directory.EnumerateFiles(symbolsDir, "*.svg"));
        }
        File.WriteAllText(Path.Combine(output, "summary.json"), summary.ToJsonString(Indented));
        Console.WriteLine(summary.ToJsonString(Indented));
        (processor as IDisposable)?.Dispose();
        return 0;
    }
}
