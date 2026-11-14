using EncDotNet.SoundCharts.Charts;
using EncDotNet.SoundCharts.Export;

namespace EncDotNet.SoundCharts.Tests.Export;

public class ExportPiecesTests
{
    [Theory]
    [InlineData("chart.pdf", ChartExportFormat.Pdf)]
    [InlineData(@"C:\out\Chart.PNG", ChartExportFormat.Png)]
    [InlineData("chart.jpeg", ChartExportFormat.Jpeg)]
    [InlineData("chart.jpg", ChartExportFormat.Jpeg)]
    [InlineData("chart.tiff", ChartExportFormat.Tiff)]
    public void FromPath_KnownExtensions_NameTheFormat(string path, ChartExportFormat format)
    {
        Assert.Equal(format, ChartExportFormats.FromPath(path));
    }

    [Theory]
    [InlineData("chart.svg")]
    [InlineData("chart")]
    [InlineData("")]
    [InlineData(null)]
    public void FromPath_OtherNames_AreNoFormat(string? path)
    {
        Assert.Null(ChartExportFormats.FromPath(path));
    }

    [Fact]
    public void WithExtension_KeepsAMatchingExtensionAndReplacesAnother()
    {
        Assert.Equal(@"C:\out\chart.jpeg", ChartExportFormats.WithExtension(@"C:\out\chart.jpeg", ChartExportFormat.Jpeg));
        Assert.Equal(@"C:\out\chart.tif", ChartExportFormats.WithExtension(@"C:\out\chart.pdf", ChartExportFormat.Tiff));
        Assert.Equal(@"C:\out\chart.pdf", ChartExportFormats.WithExtension(@"C:\out\chart", ChartExportFormat.Pdf));
    }

    [Fact]
    public void Resolutions_IncludeThePrintResolutions()
    {
        Assert.Contains(300, ChartExportFormats.Resolutions);
        Assert.Contains(600, ChartExportFormats.Resolutions);
        Assert.All(ChartExportFormats.Resolutions, dpi => Assert.True(ChartExportFormats.IsValidResolution(dpi)));
        Assert.False(ChartExportFormats.IsValidResolution(2400));
    }

    [Fact]
    public void Settings_Validate_ChecksPathFormatAndResolution()
    {
        new ChartExportSettings(@"C:\out\chart.pdf", ChartExportFormat.Pdf, 300).Validate();

        Assert.Throws<ArgumentException>(() => new ChartExportSettings(@"C:\out\chart.png", ChartExportFormat.Pdf, 300).Validate());
        Assert.Throws<ArgumentException>(() => new ChartExportSettings(" ", ChartExportFormat.Pdf, 300).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartExportSettings(@"C:\out\chart.pdf", ChartExportFormat.Pdf, 10).Validate());
    }

    [Theory]
    [InlineData("SoundCharts · Not for navigation · Data: NOAA")]
    [InlineData("NOT FOR NAVIGATION")]
    [InlineData("Chart\r\nnot for\nnavigation")]
    [InlineData("<BOL>Not for</BOL> navigation")]
    public void Notice_IsFoundInAnyTextElement(string text)
    {
        Assert.True(NotForNavigationNotice.IsPresent(["Title", null, text]));
    }

    [Fact]
    public void Notice_IsMissingFromOtherText()
    {
        Assert.False(NotForNavigationNotice.IsPresent(["Nantucket Sound", "Scale 1:36,000", null]));
        Assert.False(NotForNavigationNotice.IsPresent([]));
    }

    [Fact]
    public void Time_ParsesTheWmtsDimension()
    {
        var expected = new DateTime(2025, 12, 17, 20, 0, 0, DateTimeKind.Utc);

        Assert.Equal(expected, ExportTime.TryParse("2025-12-17T20:00:00Z"));
        Assert.Equal(DateTimeKind.Utc, ExportTime.TryParse("2025-12-17T20:00:00Z")?.Kind);
        Assert.Equal(expected, ExportTime.TryParse(new DateTimeOffset(expected)));
        Assert.Null(ExportTime.TryParse("soon"));
        Assert.Null(ExportTime.TryParse(null));
        Assert.Null(ExportTime.TryParse(42));
    }

    [Fact]
    public void Time_NearestStep_IsTheClosestIndex()
    {
        var start = new DateTime(2025, 12, 17, 13, 0, 0, DateTimeKind.Utc);
        DateTime[] steps = [start, start.AddHours(1), start.AddHours(2)];

        Assert.Equal(0, ExportTime.NearestStep(steps, start.AddMinutes(-90)));
        Assert.Equal(1, ExportTime.NearestStep(steps, start.AddMinutes(70)));
        Assert.Equal(2, ExportTime.NearestStep(steps, start.AddDays(3)));
        Assert.Null(ExportTime.NearestStep(steps, null));
        Assert.Null(ExportTime.NearestStep([], start));
    }

    [Fact]
    public void Time_ReadsTileJsonSteps()
    {
        const string json = """{"tilejson":"3.0.0","s100":{"times":["2025-12-17T14:00:00Z","2025-12-17T13:00:00Z","bad"]}}""";

        var steps = ExportTime.ReadTileJsonSteps(json);

        Assert.Equal(
            [new DateTime(2025, 12, 17, 13, 0, 0, DateTimeKind.Utc), new DateTime(2025, 12, 17, 14, 0, 0, DateTimeKind.Utc)],
            steps);
        Assert.Empty(ExportTime.ReadTileJsonSteps("""{"tilejson":"3.0.0"}"""));
        Assert.Empty(ExportTime.ReadTileJsonSteps("not json"));
        Assert.Empty(ExportTime.ReadTileJsonSteps(null));
    }

    [Fact]
    public void CommandLine_RendersTheLayerAtThePlansZoomAndRatio()
    {
        var options = new ChartRenderOptions(SymbolScale: 1.5, ShowText: false);
        var datasets = new DatasetSet([DatasetSource.Dataset(@"C:\data\US5MA1BO.000"), DatasetSource.Dataset(@"C:\data\bathy.h5")], options);
        var request = new ChartRasterRequest("Chart", datasets, "dusk", Frame(), 300, @"C:\work\layer-0.tif") { TimeStep = 3 };
        var plan = ChartTilePlan.For(request.Frame, request.Dpi);

        var arguments = TilesExportCommandLine.Build(request, plan);

        Assert.Equal(["tiles", "export"], arguments.Take(2));
        Assert.Equal(2, arguments.Count(a => a == "--layer"));
        Assert.Equal(@"C:\work\layer-0.tif", After(arguments, "-o"));
        Assert.Equal("geotiff", After(arguments, "--container"));
        Assert.Equal("14", After(arguments, "--min-zoom"));
        Assert.Equal("14", After(arguments, "--max-zoom"));
        Assert.Equal("512", After(arguments, "--tile-size"));
        Assert.Equal("dusk", After(arguments, "--palette"));
        Assert.Equal("3", After(arguments, "--time-step"));
        Assert.Equal(plan.BoundingBox, After(arguments, "--bbox"));
        Assert.Equal("1.5", After(arguments, "--symbol-scale"));
        Assert.Contains("--no-text", arguments);
    }

    [Fact]
    public void CommandLine_AtScreenResolutionAndFirstStep_Uses256PixelTilesAndNoStep()
    {
        var request = new ChartRasterRequest(
            "Chart", new DatasetSet([DatasetSource.Dataset(@"C:\data\US5MA1BO.000")]), "day", Frame(), 96, @"C:\work\layer-0.tif");

        var arguments = TilesExportCommandLine.Build(request, ChartTilePlan.For(request.Frame, 96));

        Assert.Equal(@"C:\data\US5MA1BO.000", arguments[2]);
        Assert.Equal("256", After(arguments, "--tile-size"));
        Assert.DoesNotContain("--time-step", arguments);
    }

    internal static ExportFrame Frame()
    {
        var (minX, minY) = ChartTilePlan.FromLonLat(-70.40, 41.24);
        var size = 960 * ChartTilePlan.MetresPerTilePixel(14);
        return new ExportFrame(minX, minY, minX + size, minY + (size * 0.75), 10, 7.5);
    }

    private static string After(IReadOnlyList<string> arguments, string name)
    {
        var index = arguments.ToList().IndexOf(name);
        Assert.True(index >= 0 && index < arguments.Count - 1, $"No {name} argument.");
        return arguments[index + 1];
    }
}
