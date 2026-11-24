using System.Globalization;
using EncDotNet.SoundCharts.Charts;
using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts.Tests.Server;

public class ServerCommandLineTests
{
    private static readonly ServerEndpoint Endpoint = new(52448, "tok-en_1");

    [Fact]
    public void Build_ServesOneExchangeSet_OnLoopbackWithThePortAndToken()
    {
        var set = new DatasetSet([DatasetSource.ExchangeSet(@"C:\Charts\US_ENC")]);

        var arguments = ServerCommandLine.Build(set, Endpoint, @"C:\Cache\tile-cache", 2048);

        Assert.Equal(
            [
                "tiles", "serve", @"C:\Charts\US_ENC",
                "--host", "127.0.0.1",
                "--port", "52448",
                "--token", "tok-en_1",
                "--refresh", "0",
                "--no-viewer",
                "--cache-dir", @"C:\Cache\tile-cache",
                "--cache-dir-mb", "2048",
            ],
            arguments);
    }

    [Fact]
    public void Build_ServesSeveralDatasets_AsLayers()
    {
        var set = new DatasetSet([DatasetSource.Dataset(@"C:\Charts\b.000"), DatasetSource.Dataset(@"C:\Charts\a.h5")]);

        var arguments = ServerCommandLine.Build(set, Endpoint, tileCache: null, 1024);

        Assert.Equal(
            ["tiles", "serve", "--layer", @"C:\Charts\a.h5", "--layer", @"C:\Charts\b.000"],
            arguments.Take(6));
        Assert.DoesNotContain("--cache-dir", arguments);
    }

    [Fact]
    public void Inputs_AnAdvisoryFolder_IsItsLatestForecastZip()
    {
        // Issue #69: tiles serve refuses a folder as a --layer, and would read
        // charts beside the storm's zips if given the folder as its input.
        using var temp = new TempFolder();
        var folder = Path.Combine(temp.Path, "ian");
        Assert.SkipUnless(Fixtures.CopyUpstreamStorm(folder, advisory: "019"), $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        Fixtures.CopyUpstreamStorm(folder);
        var forecast = Path.Combine(folder, "al092022_5day_020.zip");
        var storm = DatasetSource.Dataset(folder);

        Assert.Equal([forecast], ServerCommandLine.Inputs(new DatasetSet([storm])));
        var cell = temp.File("a.000");
        var layers = ServerCommandLine.Inputs(new DatasetSet([storm, DatasetSource.Dataset(cell)]));
        Assert.Equal(["--layer", "--layer"], layers.Where((_, i) => i % 2 == 0));
        Assert.Equal(new[] { cell, forecast }.Order(StringComparer.Ordinal), layers.Where((_, i) => i % 2 == 1).Order(StringComparer.Ordinal));

        // A folder source is what the user chose, and is served as they chose it.
        Assert.Equal(folder, ServerCommandLine.InputPath(DatasetSource.Folder(folder)));
    }

    [Fact]
    public void Inputs_ARunFolderAlone_IsTheFolderWithOnlyGrib2()
    {
        // Issue #73: tiles serve reads a run as one dataset only from a folder
        // given as its input; --only keeps a storm or chart beside it out.
        using var temp = new TempFolder();
        var folder = Path.Combine(temp.Path, "gfswave");
        Assert.SkipWhen(Fixtures.WriteUpstreamRun(folder).Count == 0, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        var work = Path.Combine(temp.Path, "work");

        Assert.Equal([folder, "--only", "GRIB2"], ServerCommandLine.Inputs(new DatasetSet([DatasetSource.Dataset(folder)]), work));
        Assert.Equal([folder, "--only", "GRIB2"], ServerCommandLine.Inputs(new DatasetSet([DatasetSource.Dataset(folder)])));
        Assert.False(Directory.Exists(work));

        // A folder source is what the user chose, and is served as they chose it.
        Assert.Equal([folder], ServerCommandLine.Inputs(new DatasetSet([DatasetSource.Folder(folder)]), work));
    }

    [Fact]
    public void Inputs_ARunFolderWithSubfolders_IsTheRunAsOneFile()
    {
        // The server would read a sub-folder's datasets too, even with --only GRIB2.
        using var temp = new TempFolder();
        var folder = Path.Combine(temp.Path, "gfswave");
        var files = Fixtures.WriteUpstreamRun(folder);
        Assert.SkipWhen(files.Count == 0, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        Fixtures.WriteUpstreamRun(Path.Combine(folder, "older"), [0]);
        var work = Path.Combine(temp.Path, "work");

        var input = Assert.Single(ServerCommandLine.Inputs(new DatasetSet([DatasetSource.Dataset(folder)]), work));

        Assert.StartsWith(Path.Combine(work, RunFolderInput.FolderName), input, StringComparison.Ordinal);
        Assert.Equal("gfswave.grib2", Path.GetFileName(input));
        Assert.Equal(files.SelectMany(File.ReadAllBytes), File.ReadAllBytes(input));
    }

    [Fact]
    public void Inputs_ARunFolderBesideOtherDatasets_IsALayerOfTheRunAsOneFile()
    {
        // tiles serve refuses a folder as a --layer, and draws each of the
        // run's files as a --layer of its own at every time.
        using var temp = new TempFolder();
        var folder = Path.Combine(temp.Path, "gfswave");
        var files = Fixtures.WriteUpstreamRun(folder);
        Assert.SkipWhen(files.Count == 0, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        var cap = temp.File("warning.xml");
        var work = Path.Combine(temp.Path, "work");
        var set = new DatasetSet([DatasetSource.Dataset(folder), DatasetSource.Dataset(cap)]);

        var inputs = ServerCommandLine.Inputs(set, work);

        Assert.Equal(["--layer", "--layer"], inputs.Where((_, i) => i % 2 == 0));
        var run = Assert.Single(inputs.Where((_, i) => i % 2 == 1), path => path != cap);
        Assert.Equal(files.SelectMany(File.ReadAllBytes), File.ReadAllBytes(run));

        // Written once; a new hour gives a new file.
        Assert.Equal(inputs, ServerCommandLine.Inputs(set, work));
        File.Copy(files[0], Path.Combine(folder, "gfswave.t00z.global.0p25.f009.grib2"));
        var newer = ServerCommandLine.Inputs(set, work);
        Assert.NotEqual(inputs, newer);
        Assert.Equal(new FileInfo(run).Length + new FileInfo(files[0]).Length, new FileInfo(newer[Array.IndexOf(inputs.ToArray(), run)]).Length);

        // Without a folder to write to, the set can't be served.
        Assert.Throws<NotSupportedException>(() => ServerCommandLine.Inputs(set));
    }

    [Fact]
    public void Build_ARunFolderBesideOtherDatasets_WritesTheRunInTheWorkingFolder()
    {
        using var temp = new TempFolder();
        var folder = Path.Combine(temp.Path, "gfswave");
        Assert.SkipWhen(Fixtures.WriteUpstreamRun(folder).Count == 0, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        var cap = temp.File("warning.xml");
        var work = Path.Combine(temp.Path, "work");

        var arguments = ServerCommandLine.Build(
            new DatasetSet([DatasetSource.Dataset(folder), DatasetSource.Dataset(cap)]), Endpoint, null, 512, work);

        Assert.Contains(arguments, a => a.StartsWith(work, StringComparison.Ordinal) && a.EndsWith("gfswave.grib2", StringComparison.Ordinal));
    }

    [Fact]
    public void InputPath_AFileOrAnOrdinaryFolder_IsItsPath()
    {
        using var temp = new TempFolder();
        var folder = temp.Folder("charts");
        var cell = temp.File("a.000");

        Assert.Equal(cell, ServerCommandLine.InputPath(DatasetSource.Dataset(cell)));
        Assert.Equal(folder, ServerCommandLine.InputPath(DatasetSource.Dataset(folder)));
    }

    [Fact]
    public void Build_RejectsAnExchangeSetMixedWithDatasets()
    {
        var set = new DatasetSet([DatasetSource.ExchangeSet(@"C:\Charts\US_ENC"), DatasetSource.Dataset(@"C:\Charts\a.000")]);

        Assert.Throws<NotSupportedException>(() => ServerCommandLine.Build(set, Endpoint, null, 1024));
    }

    [Fact]
    public void Build_AddsTheRenderOptions()
    {
        var set = new DatasetSet(
            [DatasetSource.Dataset(@"C:\Charts\a.000")],
            new ChartRenderOptions(SymbolScale: 1.5, ShowText: false));

        var arguments = ServerCommandLine.Build(set, Endpoint, null, 1024);

        Assert.Equal(["--symbol-scale", "1.5", "--no-text"], arguments.TakeLast(3));
    }

    [Fact]
    public void Build_WritesNumbersInvariantly()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var set = new DatasetSet([DatasetSource.Dataset(@"C:\Charts\a.000")], new ChartRenderOptions(TextScale: 0.75));

            var arguments = ServerCommandLine.Build(set, new ServerEndpoint(12345, "t"), null, 1024);

            Assert.Contains("0.75", arguments);
            Assert.Contains("12345", arguments);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }
}
