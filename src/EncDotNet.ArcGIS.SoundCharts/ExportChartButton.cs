using System.Globalization;
using System.IO;
using System.Windows;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using EncDotNet.SoundCharts.Export;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Resources;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>Export SoundCharts Chart...</b> (<c>SoundCharts_ExportChart</c> in
/// Config.daml, design brief §2.1, issue #12): exports the active layout
/// with its SoundCharts layers rendered at the layout's scale and resolution.
/// </summary>
internal sealed class ExportChartButton : Button
{
    /// <summary>The name of the "Not for navigation" text element the export adds when asked (D34).</summary>
    private const string NoticeElementName = "SoundCharts Not for navigation";

    protected override async void OnClick()
    {
        ProgressDialog? progress = null;
        try
        {
            if (LayoutView.Active?.Layout is not { } layout)
            {
                return;
            }

            var survey = await QueuedTask.Run(() => ChartExporter.Survey(layout));
            if (survey.Layers.Count == 0)
            {
                MessageBox.Show(Strings.Export_NoLayers, Strings.Export_Title, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var model = new ExportChartViewModel(layout.Name, DefaultPath(layout.Name), survey.Layers.Count);
            var window = new ExportChartWindow(model) { Owner = FrameworkApplication.Current.MainWindow };
            if (window.ShowDialog() != true)
            {
                return;
            }

            var settings = model.ToSettings();
            if (!survey.HasNotice && !await OfferNoticeAsync(layout, survey))
            {
                return;
            }

            progress = new ProgressDialog(Strings.Export_Progress);
            progress.Show();
            var outcome = await ChartExporter.CreateDefault().ExportAsync(layout, survey, settings);
            progress.Hide();
            Report(outcome, settings);
        }
        catch (Exception ex)
        {
            // The boundary with Pro: log rather than let the exception reach Pro.
            progress?.Hide();
            AddInLog.Write(LogLevel.Error, $"Export SoundCharts Chart failed: {ex}");
            MessageBox.Show(
                string.Format(CultureInfo.CurrentCulture, Strings.Export_Failed, ex.Message),
                Strings.Export_Title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            progress?.Dispose();
        }
    }

    /// <summary>
    /// Offers the "Not for navigation" text (D34); never adds it unasked.
    /// Returns <see langword="false"/> when the user cancels the export.
    /// </summary>
    private static async Task<bool> OfferNoticeAsync(Layout layout, LayoutSurvey survey)
    {
        var text = string.Format(CultureInfo.CurrentCulture, Strings.Export_NoticeText, survey.Producers);
        var answer = MessageBox.Show(
            string.Format(CultureInfo.CurrentCulture, Strings.Export_NoticeQuestion, text),
            Strings.Export_Title,
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);
        if (answer == MessageBoxResult.Cancel)
        {
            return false;
        }

        if (answer == MessageBoxResult.Yes)
        {
            await QueuedTask.Run(() => AddNotice(layout, text));
        }

        return true;
    }

    /// <summary>Adds the notice in small type at the page's lower left. Runs on the MCT.</summary>
    private static void AddNotice(Layout layout, string text)
    {
        var units = layout.GetPage()?.Units ?? LinearUnit.Inches;
        var margin = LinearUnit.Inches.ConvertTo(0.25, units);
        var symbol = SymbolFactory.Instance.ConstructTextSymbol(ColorFactory.Instance.BlackRGB, 7, "Tahoma", "Regular");
        var anchor = MapPointBuilderEx.CreateMapPoint(margin, margin);
        ElementFactory.Instance.CreateTextGraphicElement(layout, TextType.PointText, anchor, symbol, text, NoticeElementName);
        AddInLog.Write($"Export SoundCharts Chart: added \"{text}\" to the layout '{layout.Name}'.");
    }

    private static void Report(ExportOutcome outcome, ChartExportSettings settings)
    {
        if (!outcome.Exported)
        {
            MessageBox.Show(
                string.Format(CultureInfo.CurrentCulture, Strings.Export_NotWritten, settings.OutputPath),
                Strings.Export_Title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        AddInLog.Write($"Export SoundCharts Chart: wrote {settings.OutputPath} ({outcome.RenderedLayers} layer(s) rendered).");
        var partial = outcome.Failures.Count > 0;
        MessageBox.Show(
            partial
                ? string.Format(CultureInfo.CurrentCulture, Strings.Export_Partial, settings.OutputPath, outcome.Failures.Count)
                : string.Format(CultureInfo.CurrentCulture, Strings.Export_Done, settings.OutputPath),
            Strings.Export_Title,
            MessageBoxButton.OK,
            partial ? MessageBoxImage.Warning : MessageBoxImage.Information);
    }

    /// <summary>A PDF named after the layout in the project's home folder.</summary>
    private static string DefaultPath(string layoutName)
    {
        var folder = Project.Current?.HomeFolderPath;
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
        {
            folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        var name = string.Concat(layoutName.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)).Trim();
        return Path.Combine(folder, (name.Length == 0 ? "Layout" : name) + ChartExportFormats.Extension(ChartExportFormat.Pdf));
    }
}
