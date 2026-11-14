using System.Buffers.Binary;
using System.Runtime.InteropServices;
using EncDotNet.SoundCharts.Charts;
using EncDotNet.SoundCharts.Export;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Server;
using EncDotNet.SoundCharts.Tests.Server;

namespace EncDotNet.SoundCharts.Tests.Export;

public class TilesExportRendererTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Render_RunsTilesExportInTheWorkingFolder()
    {
        using var temp = new TempFolder();
        var launcher = new FakeServerProcessLauncher();
        var output = Path.Combine(temp.Path, "out", "layer-0.tif");
        launcher.OnNextStart(process =>
        {
            process.Write("Wrote layer-0.tif");
            File.WriteAllText(output, "tiff");
            process.Exit(0);
        });
        var log = new SoundChartsLog(null);
        var renderer = new TilesExportRenderer(Executable(temp), launcher, log, Path.Combine(temp.Path, "work"));

        var raster = await renderer.RenderAsync(Request(output, 300), Token);

        Assert.Equal(output, raster.Path);
        Assert.Contains("512-pixel tiles", raster.Description, StringComparison.Ordinal);
        var started = launcher.Last.StartInfo;
        Assert.Equal(Path.Combine(temp.Path, "work"), started.WorkingDirectory);
        Assert.Equal(["tiles", "export"], started.Arguments.Take(2));
        Assert.Equal("1", started.Environment["S100_NO_UPDATE_CHECK"]);
        Assert.Contains(log.Snapshot(), e => e.Source == TilesExportRenderer.LogSource && e.Message == "Wrote layer-0.tif");
    }

    [Fact]
    public async Task Render_AFailingExport_ThrowsWithItsLastError()
    {
        using var temp = new TempFolder();
        var launcher = new FakeServerProcessLauncher();
        launcher.OnNextStart(process =>
        {
            process.Write("No dataset covers --bbox.", isError: true);
            process.Exit(2);
        });
        var renderer = new TilesExportRenderer(Executable(temp), launcher, new SoundChartsLog(null), temp.Path);

        var ex = await Assert.ThrowsAsync<ChartRasterException>(
            () => renderer.RenderAsync(Request(Path.Combine(temp.Path, "x.tif"), 96), Token));

        Assert.Contains("No dataset covers --bbox.", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_WithoutAServer_Throws()
    {
        using var temp = new TempFolder();
        var renderer = new TilesExportRenderer(null, new FakeServerProcessLauncher(), new SoundChartsLog(null), temp.Path);

        await Assert.ThrowsAsync<ChartRasterException>(() => renderer.RenderAsync(Request(Path.Combine(temp.Path, "x.tif"), 96), Token));
    }

    [Fact]
    public async Task Render_Cancelled_KillsTheProcess()
    {
        using var temp = new TempFolder();
        var launcher = new FakeServerProcessLauncher();
        var renderer = new TilesExportRenderer(Executable(temp), launcher, new SoundChartsLog(null), temp.Path);
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Token);

        var render = renderer.RenderAsync(Request(Path.Combine(temp.Path, "x.tif"), 96), cancel.Token);
        await cancel.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => render);
        Assert.True(launcher.Last.WasKilled);
    }

    /// <summary>
    /// Renders the same frame at 96 and 300 dpi with the real bundled
    /// <c>s100</c>: the same zoom and Web Mercator extent, at twice the
    /// pixels. Skipped when the server hasn't been built.
    /// </summary>
    [Fact]
    public async Task Render_WithTheBundledServer_WritesTheSameExtentAtTwiceThePixels()
    {
        var executable = ServerExecutable.Locate(AppContext.BaseDirectory, RuntimeInformation.OSArchitecture, overridePath: null);
        Assert.SkipWhen(executable is null, "No bundled server; run tools/build-server.ps1 before building.");

        using var temp = new TempFolder();
        var dataset = Path.Combine(temp.Path, "synthetic-aton.gml");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "TestData", "synthetic-aton.gml"), dataset);
        var renderer = new TilesExportRenderer(executable, new ProcessServerLauncher(), new SoundChartsLog(null), Path.Combine(temp.Path, "work"));

        // The two AtoNs at 10°N 30°W, a 4 x 3 inch frame at zoom 15.
        var (x, y) = ChartTilePlan.FromLonLat(-30.005, 10.005);
        var half = 2 * 96 * ChartTilePlan.MetresPerTilePixel(15);
        var frame = new ExportFrame(x - half, y - (half * 0.75), x + half, y + (half * 0.75), 4, 3);
        var datasets = new DatasetSet([DatasetSource.Dataset(dataset)]);

        var screen = await renderer.RenderAsync(new ChartRasterRequest("Chart", datasets, "day", frame, 96, Path.Combine(temp.Path, "96.tif")), Token);
        var print = await renderer.RenderAsync(new ChartRasterRequest("Chart", datasets, "day", frame, 300, Path.Combine(temp.Path, "300.tif")), Token);

        var (screenWidth, screenHeight) = TiffSize(screen.Path);
        var (printWidth, printHeight) = TiffSize(print.Path);
        Assert.Equal(0, screenWidth % 256);
        Assert.Equal(screenWidth * 2, printWidth);
        Assert.Equal(screenHeight * 2, printHeight);
        Assert.Contains("zoom 15", screen.Description, StringComparison.Ordinal);
        Assert.Contains("zoom 15", print.Description, StringComparison.Ordinal);
    }

    private static ChartRasterRequest Request(string output, int dpi) =>
        new("Chart", new DatasetSet([DatasetSource.Dataset(@"C:\data\US5MA1BO.000")]), "day", ExportPiecesTests.Frame(), dpi, output);

    private static ServerExecutable Executable(TempFolder temp) => new(temp.File("s100.exe"), "win-x64", IsOverride: false);

    /// <summary>The size of a little-endian TIFF's first image: a COG's full resolution.</summary>
    private static (int Width, int Height) TiffSize(string path)
    {
        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes[0] == (byte)'I' && bytes[1] == (byte)'I', "Not a little-endian TIFF.");
        var bigTiff = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(2)) == 43;
        var ifd = bigTiff ? (long)BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(8)) : BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4));
        var count = bigTiff ? (long)BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan((int)ifd)) : BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan((int)ifd));
        var entrySize = bigTiff ? 20 : 12;
        var first = ifd + (bigTiff ? 8 : 2);
        int width = 0, height = 0;
        for (var i = 0; i < count; i++)
        {
            var entry = bytes.AsSpan((int)(first + (i * entrySize)));
            var tag = BinaryPrimitives.ReadUInt16LittleEndian(entry);
            var type = BinaryPrimitives.ReadUInt16LittleEndian(entry[2..]);
            var valueOffset = bigTiff ? 12 : 8;
            var value = type == 3 ? BinaryPrimitives.ReadUInt16LittleEndian(entry[valueOffset..]) : (int)BinaryPrimitives.ReadUInt32LittleEndian(entry[valueOffset..]);
            if (tag == 256)
            {
                width = value;
            }
            else if (tag == 257)
            {
                height = value;
            }
        }

        return (width, height);
    }
}
