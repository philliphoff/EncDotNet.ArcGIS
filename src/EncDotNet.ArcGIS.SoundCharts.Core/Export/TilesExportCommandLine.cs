using System.Globalization;
using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts.Export;

/// <summary>
/// Builds the <c>s100 tiles export</c> arguments that render one layer of a
/// map frame as a Cloud-Optimized GeoTIFF at one zoom level.
/// </summary>
public static class TilesExportCommandLine
{
    /// <summary>
    /// Gets the arguments: the layer's inputs and start-up options as its
    /// chart server has them, its palette and time step, and the plan's zoom,
    /// pixel ratio and bounds.
    /// </summary>
    /// <param name="request">The layer and output.</param>
    /// <param name="plan">The tiles for the frame and resolution.</param>
    /// <param name="workingDirectory">
    /// The export's working folder, where a GRIB2 run exported beside other
    /// datasets is written as one file (see <see cref="ServerCommandLine.Inputs"/>);
    /// <see langword="null"/> for none.
    /// </param>
    /// <exception cref="NotSupportedException">The layer's sources can't be given to one command (see <see cref="ServerCommandLine.Inputs"/>).</exception>
    /// <exception cref="IOException">A run's file couldn't be written.</exception>
    public static IReadOnlyList<string> Build(ChartRasterRequest request, ChartTilePlan plan, string? workingDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(plan);

        var zoom = plan.Zoom.ToString(CultureInfo.InvariantCulture);
        List<string> arguments = ["tiles", "export", .. ServerCommandLine.Inputs(request.Datasets, workingDirectory)];
        arguments.AddRange(
        [
            "-o", request.OutputPath,
            "--container", "geotiff",
            "--format", "png",
            "--min-zoom", zoom,
            "--max-zoom", zoom,
            "--bbox", plan.BoundingBox,
            "--tile-size", (ChartTilePlan.TileSize * plan.PixelRatio).ToString(CultureInfo.InvariantCulture),
            "--palette", request.Palette,
        ]);
        if (request.TimeStep is int step and > 0)
        {
            arguments.AddRange(["--time-step", step.ToString(CultureInfo.InvariantCulture)]);
        }

        arguments.AddRange(request.Datasets.Options.ToArguments());
        return arguments;
    }
}
