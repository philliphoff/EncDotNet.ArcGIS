using EncDotNet.SoundCharts.Export;

namespace EncDotNet.SoundCharts.Tests.Export;

public class ExportChartViewModelTests
{
    [Fact]
    public void New_StartsAt300DpiInTheSuggestedFilesFormat()
    {
        var model = new ExportChartViewModel("Layout", @"C:\out\Layout.png", 2);

        Assert.Equal(ChartExportFormat.Png, model.Format.Format);
        Assert.Equal(300, model.Resolution.Dpi);
        Assert.Contains("Layout", model.Heading, StringComparison.Ordinal);
        Assert.Contains("2", model.Summary, StringComparison.Ordinal);
        Assert.Equal(["96 dpi", "150 dpi", "300 dpi", "600 dpi"], model.Resolutions.Select(r => r.Label));
    }

    [Fact]
    public void Format_Changed_ChangesTheFilesExtension()
    {
        var model = new ExportChartViewModel("Layout", @"C:\out\Layout.pdf", 1);
        var changed = new List<string?>();
        model.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        model.Format = model.Formats.Single(f => f.Format == ChartExportFormat.Tiff);

        Assert.Equal(@"C:\out\Layout.tif", model.OutputPath);
        Assert.Contains(nameof(ExportChartViewModel.OutputPath), changed);
        Assert.Contains("*.tif", model.FileFilter, StringComparison.Ordinal);
    }

    [Fact]
    public void OutputPath_WithAnotherExtension_ChangesTheFormat()
    {
        var model = new ExportChartViewModel("Layout", @"C:\out\Layout.pdf", 1);

        model.OutputPath = @"C:\out\Layout.jpg";

        Assert.Equal(ChartExportFormat.Jpeg, model.Format.Format);
    }

    [Fact]
    public void ToSettings_GivesThePathTheFormatsExtension()
    {
        var model = new ExportChartViewModel("Layout", @"C:\out\Layout.pdf", 1)
        {
            OutputPath = @"C:\out\Layout",
        };
        model.Resolution = model.Resolutions.Single(r => r.Dpi == 600);

        var settings = model.ToSettings();

        Assert.Equal(new ChartExportSettings(@"C:\out\Layout.pdf", ChartExportFormat.Pdf, 600), settings);
    }

    [Fact]
    public void CanExport_NeedsAFileInAnExistingFolder()
    {
        using var temp = new TempFolder();
        var model = new ExportChartViewModel("Layout", Path.Combine(temp.Path, "Layout.pdf"), 1);

        Assert.True(model.CanExport);

        model.OutputPath = Path.Combine(temp.Path, "missing", "Layout.pdf");
        Assert.False(model.CanExport);

        model.OutputPath = "Layout.pdf";
        Assert.False(model.CanExport);
    }
}
