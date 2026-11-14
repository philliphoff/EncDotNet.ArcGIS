using System.Globalization;
using System.Resources;

namespace EncDotNet.SoundCharts.Resources;

/// <summary>
/// Localized UI strings, from <c>Strings.resx</c>. XAML refers to them with
/// <c>{x:Static res:Strings.Key}</c>. Keep this class and the .resx in step.
/// </summary>
public static class Strings
{
    private static readonly ResourceManager Manager =
        new("EncDotNet.SoundCharts.Resources.Strings", typeof(Strings).Assembly);

    /// <summary>Gets the About window's title.</summary>
    public static string About_Title => Get(nameof(About_Title));

    /// <summary>Gets the About window's heading.</summary>
    public static string About_Heading => Get(nameof(About_Heading));

    /// <summary>Gets the label for the add-in version.</summary>
    public static string About_AddInVersion => Get(nameof(About_AddInVersion));

    /// <summary>Gets the label for the EncDotNet.S100 version.</summary>
    public static string About_S100Version => Get(nameof(About_S100Version));

    /// <summary>Gets the label for the ArcGIS Pro version.</summary>
    public static string About_ProVersion => Get(nameof(About_ProVersion));

    /// <summary>Gets the label for the .NET runtime version.</summary>
    public static string About_RuntimeVersion => Get(nameof(About_RuntimeVersion));

    /// <summary>Gets the label for the process architecture.</summary>
    public static string About_Architecture => Get(nameof(About_Architecture));

    /// <summary>
    /// Gets the format for an emulated architecture: <c>{0}</c> is the
    /// process architecture and <c>{1}</c> the operating system's.
    /// </summary>
    public static string About_ArchitectureEmulated => Get(nameof(About_ArchitectureEmulated));

    /// <summary>Gets the not-for-navigation notice.</summary>
    public static string About_NotForNavigation => Get(nameof(About_NotForNavigation));

    /// <summary>Gets the accessible name of the version list.</summary>
    public static string About_Versions => Get(nameof(About_Versions));

    /// <summary>Gets the Copy button's text.</summary>
    public static string Button_Copy => Get(nameof(Button_Copy));

    /// <summary>Gets the Close button's text.</summary>
    public static string Button_Close => Get(nameof(Button_Close));

    /// <summary>Gets the Copy button's tooltip.</summary>
    public static string Tooltip_AboutCopy => Get(nameof(Tooltip_AboutCopy));

    /// <summary>Gets the heading before the first click.</summary>
    public static string ObjectInfo_EmptyTitle => Get(nameof(ObjectInfo_EmptyTitle));

    /// <summary>Gets the line under the heading before the first click.</summary>
    public static string ObjectInfo_EmptyDescription => Get(nameof(ObjectInfo_EmptyDescription));

    /// <summary>Gets the heading when the map has no visible SoundCharts layers.</summary>
    public static string ObjectInfo_NoChartLayersTitle => Get(nameof(ObjectInfo_NoChartLayersTitle));

    /// <summary>Gets the line under the no-layers heading.</summary>
    public static string ObjectInfo_NoChartLayersDescription => Get(nameof(ObjectInfo_NoChartLayersDescription));

    /// <summary>Gets the heading while a pick runs.</summary>
    public static string ObjectInfo_Working => Get(nameof(ObjectInfo_Working));

    /// <summary>Gets the heading when a pick failed unexpectedly.</summary>
    public static string ObjectInfo_FailedTitle => Get(nameof(ObjectInfo_FailedTitle));

    /// <summary>Gets the line under the failure heading.</summary>
    public static string ObjectInfo_FailedDescription => Get(nameof(ObjectInfo_FailedDescription));

    /// <summary>Gets the heading when the datasets have nothing at the point.</summary>
    public static string ObjectInfo_NothingHereTitle => Get(nameof(ObjectInfo_NothingHereTitle));

    /// <summary>Gets the line under the nothing-here heading.</summary>
    public static string ObjectInfo_NothingHereDescription => Get(nameof(ObjectInfo_NothingHereDescription));

    /// <summary>Gets the heading for one feature.</summary>
    public static string ObjectInfo_OneFeature => Get(nameof(ObjectInfo_OneFeature));

    /// <summary>Gets the heading format for several features: <c>{0}</c> is the count.</summary>
    public static string ObjectInfo_FeatureCountFormat => Get(nameof(ObjectInfo_FeatureCountFormat));

    /// <summary>Gets the clicked position: <c>{0}</c> latitude and <c>{1}</c> longitude, degrees.</summary>
    public static string ObjectInfo_LocationFormat => Get(nameof(ObjectInfo_LocationFormat));

    /// <summary>Gets a dataset group's heading: <c>{0}</c> the dataset name and <c>{1}</c> its product specification.</summary>
    public static string ObjectInfo_DatasetFormat => Get(nameof(ObjectInfo_DatasetFormat));

    /// <summary>Gets a feature's id line: <c>{0}</c> is the id.</summary>
    public static string ObjectInfo_IdFormat => Get(nameof(ObjectInfo_IdFormat));

    /// <summary>Gets the source dataset line: <c>{0}</c> is its path.</summary>
    public static string ObjectInfo_SourceFormat => Get(nameof(ObjectInfo_SourceFormat));

    /// <summary>Gets the attributes heading.</summary>
    public static string ObjectInfo_AttributesHeading => Get(nameof(ObjectInfo_AttributesHeading));

    /// <summary>Gets the text for a feature without attributes.</summary>
    public static string ObjectInfo_NoAttributes => Get(nameof(ObjectInfo_NoAttributes));

    /// <summary>Gets the associations heading.</summary>
    public static string ObjectInfo_ReferencesHeading => Get(nameof(ObjectInfo_ReferencesHeading));

    /// <summary>Gets an association: <c>{0}</c> the role and <c>{1}</c> the target.</summary>
    public static string ObjectInfo_ReferenceFormat => Get(nameof(ObjectInfo_ReferenceFormat));

    /// <summary>Gets an attribute row's tooltip: <c>{0}</c> the code and <c>{1}</c> the value as encoded.</summary>
    public static string ObjectInfo_AttributeTooltipFormat => Get(nameof(ObjectInfo_AttributeTooltipFormat));

    /// <summary>Gets the strip on data its provider marks not for navigation.</summary>
    public static string ObjectInfo_NotForNavigation => Get(nameof(ObjectInfo_NotForNavigation));

    /// <summary>Gets the not-for-navigation strip's tooltip.</summary>
    public static string ObjectInfo_NotForNavigationToolTip => Get(nameof(ObjectInfo_NotForNavigationToolTip));

    /// <summary>Gets the time note when no map time was used.</summary>
    public static string ObjectInfo_TimeDefault => Get(nameof(ObjectInfo_TimeDefault));

    /// <summary>Gets the time note: <c>{0}</c> is the time.</summary>
    public static string ObjectInfo_TimeFormat => Get(nameof(ObjectInfo_TimeFormat));

    /// <summary>Gets a UTC time: <c>{0}</c> is the time.</summary>
    public static string ObjectInfo_UtcFormat => Get(nameof(ObjectInfo_UtcFormat));

    /// <summary>Gets a dataset that couldn't be read: <c>{0}</c> its name and <c>{1}</c> the reason.</summary>
    public static string ObjectInfo_ProblemFormat => Get(nameof(ObjectInfo_ProblemFormat));

    /// <summary>Gets the accessible name of the feature list.</summary>
    public static string ObjectInfo_Features => Get(nameof(ObjectInfo_Features));

    /// <summary>Gets the accessible name of the selected feature's details.</summary>
    public static string ObjectInfo_Details => Get(nameof(ObjectInfo_Details));

    /// <summary>Gets the accessible name of the problem list.</summary>
    public static string ObjectInfo_Problems => Get(nameof(ObjectInfo_Problems));

    /// <summary>Gets the label for the chart server (s100) version.</summary>
    public static string About_ServerVersion => Get(nameof(About_ServerVersion));

    /// <summary>Gets the value shown when no chart server is installed.</summary>
    public static string About_ServerNotInstalled => Get(nameof(About_ServerNotInstalled));

    /// <summary>Gets the format for an overriding chart server: <c>{0}</c> is its version and <c>{1}</c> its path.</summary>
    public static string About_ServerOverride => Get(nameof(About_ServerOverride));

    /// <summary>Gets the accessible name of the Log pane's level filter.</summary>
    public static string Log_Filter => Get(nameof(Log_Filter));

    /// <summary>Gets the Info and above filter choice.</summary>
    public static string Log_Filter_InfoAndAbove => Get(nameof(Log_Filter_InfoAndAbove));

    /// <summary>Gets the Warnings and errors filter choice.</summary>
    public static string Log_Filter_WarningsAndErrors => Get(nameof(Log_Filter_WarningsAndErrors));

    /// <summary>Gets the Errors filter choice.</summary>
    public static string Log_Filter_Errors => Get(nameof(Log_Filter_Errors));

    /// <summary>Gets the Everything, Debug included filter choice.</summary>
    public static string Log_Filter_Everything => Get(nameof(Log_Filter_Everything));

    /// <summary>Gets the accessible name of the Log pane's list.</summary>
    public static string Log_Entries => Get(nameof(Log_Entries));

    /// <summary>Gets the Time column header.</summary>
    public static string Log_Column_Time => Get(nameof(Log_Column_Time));

    /// <summary>Gets the Level column header.</summary>
    public static string Log_Column_Level => Get(nameof(Log_Column_Level));

    /// <summary>Gets the Source column header.</summary>
    public static string Log_Column_Source => Get(nameof(Log_Column_Source));

    /// <summary>Gets the Message column header.</summary>
    public static string Log_Column_Message => Get(nameof(Log_Column_Message));

    /// <summary>Gets the format of the source filter note: <c>{0}</c> is the source.</summary>
    public static string Log_ShowingSource => Get(nameof(Log_ShowingSource));

    /// <summary>Gets the Copy all button's text.</summary>
    public static string Button_CopyAll => Get(nameof(Button_CopyAll));

    /// <summary>Gets the Open Log Folder button's text.</summary>
    public static string Button_OpenLogFolder => Get(nameof(Button_OpenLogFolder));

    /// <summary>Gets the Show all button's text.</summary>
    public static string Button_ShowAll => Get(nameof(Button_ShowAll));

    /// <summary>Gets the Copy all button's tooltip.</summary>
    public static string Tooltip_LogCopyAll => Get(nameof(Tooltip_LogCopyAll));

    /// <summary>Gets the Open Log Folder button's tooltip.</summary>
    public static string Tooltip_LogOpenFolder => Get(nameof(Tooltip_LogOpenFolder));

    /// <summary>Gets the Show all button's tooltip.</summary>
    public static string Tooltip_LogShowAll => Get(nameof(Tooltip_LogShowAll));

    /// <summary>Gets the title of SoundCharts notifications.</summary>
    public static string Notification_Title => Get(nameof(Notification_Title));

    /// <summary>Gets the server-stopped notification: <c>{0}</c> is the reason.</summary>
    public static string Notification_ServerStopped => Get(nameof(Notification_ServerStopped));

    /// <summary>Gets the hint that a notification opens the log.</summary>
    public static string Notification_ShowLog => Get(nameof(Notification_ShowLog));

    /// <summary>Gets the name of the Chart layer.</summary>
    public static string Layer_Chart => Get(nameof(Layer_Chart));

    /// <summary>Gets the name of the Water level layer (S-104).</summary>
    public static string Layer_WaterLevel => Get(nameof(Layer_WaterLevel));

    /// <summary>Gets the name of the Surface currents layer (S-111).</summary>
    public static string Layer_SurfaceCurrents => Get(nameof(Layer_SurfaceCurrents));

    /// <summary>Gets the group layer's name: <c>{0}</c> is the source.</summary>
    public static string Group_Name => Get(nameof(Group_Name));

    /// <summary>Gets the group's source when several inputs were added: <c>{0}</c> is the dataset count.</summary>
    public static string Group_DatasetCount => Get(nameof(Group_DatasetCount));

    /// <summary>Gets a layer's description: <c>{0}</c> is the products, <c>{1}</c> the producers.</summary>
    public static string Credits_Description => Get(nameof(Credits_Description));

    /// <summary>Gets a layer's credits: <c>{0}</c> is the producers, <c>{1}</c> the <c>s100</c> version.</summary>
    public static string Credits_Attribution => Get(nameof(Credits_Attribution));

    /// <summary>Gets the producer when no dataset names one.</summary>
    public static string Credits_UnknownProducer => Get(nameof(Credits_UnknownProducer));

    /// <summary>Gets the message for a path that doesn't exist: <c>{0}</c> is the path.</summary>
    public static string AddData_NotFound => Get(nameof(AddData_NotFound));

    /// <summary>Gets the warning for an input without datasets: <c>{0}</c> is its name.</summary>
    public static string AddData_NoDatasets => Get(nameof(AddData_NoDatasets));

    /// <summary>Gets the warning for an unreadable file: <c>{0}</c> is its name, <c>{1}</c> the reason.</summary>
    public static string AddData_Unreadable => Get(nameof(AddData_Unreadable));

    /// <summary>Gets the warning for a file that isn't a dataset: <c>{0}</c> is its name.</summary>
    public static string AddData_NotADataset => Get(nameof(AddData_NotADataset));

    /// <summary>Gets the warning for an unsupported edition: <c>{0}</c> is the file name, <c>{1}</c> the library's message.</summary>
    public static string AddData_Edition => Get(nameof(AddData_Edition));

    /// <summary>Gets the warning for a catalogue whose product disagrees with the content: <c>{0}</c> file, <c>{1}</c> catalogue product, <c>{2}</c> content product.</summary>
    public static string AddData_ProductMismatch => Get(nameof(AddData_ProductMismatch));

    /// <summary>Gets the warning for a catalogue whose edition disagrees with the content: <c>{0}</c> file, <c>{1}</c> catalogue, <c>{2}</c> content.</summary>
    public static string AddData_EditionMismatch => Get(nameof(AddData_EditionMismatch));

    /// <summary>Gets the warning for a catalogue entry without a file: <c>{0}</c> is its path.</summary>
    public static string AddData_Missing => Get(nameof(AddData_Missing));

    /// <summary>Gets the note for files skipped in a folder: <c>{0}</c> is the count, <c>{1}</c> the folder.</summary>
    public static string AddData_SkippedFiles => Get(nameof(AddData_SkippedFiles));

    /// <summary>Gets the file dialog's title.</summary>
    public static string AddData_FileDialogTitle => Get(nameof(AddData_FileDialogTitle));

    /// <summary>Gets the folder dialog's title.</summary>
    public static string AddData_FolderDialogTitle => Get(nameof(AddData_FolderDialogTitle));

    /// <summary>Gets the file dialog's filter.</summary>
    public static string AddData_FileFilter => Get(nameof(AddData_FileFilter));

    /// <summary>Gets the progress dialog's title.</summary>
    public static string AddData_ProgressTitle => Get(nameof(AddData_ProgressTitle));

    /// <summary>Gets the progress message while datasets are read.</summary>
    public static string AddData_ProgressReading => Get(nameof(AddData_ProgressReading));

    /// <summary>Gets the progress message while chart servers start.</summary>
    public static string AddData_ProgressStarting => Get(nameof(AddData_ProgressStarting));

    /// <summary>Gets the summary when no dataset could be added.</summary>
    public static string AddData_NothingAdded => Get(nameof(AddData_NothingAdded));

    /// <summary>Gets the summary of an add: <c>{0}</c> is the datasets, <c>{1}</c> the group name.</summary>
    public static string AddData_Added => Get(nameof(AddData_Added));

    /// <summary>Gets a dataset count: <c>{0}</c> is the number.</summary>
    public static string AddData_DatasetCount => Get(nameof(AddData_DatasetCount));

    /// <summary>Gets a count of one dataset.</summary>
    public static string AddData_OneDataset => Get(nameof(AddData_OneDataset));

    /// <summary>Gets the heading above add warnings.</summary>
    public static string AddData_WarningsHeading => Get(nameof(AddData_WarningsHeading));

    /// <summary>Gets the message when there's no active map.</summary>
    public static string AddData_NoMap => Get(nameof(AddData_NoMap));

    /// <summary>Gets the message when adding fails: <c>{0}</c> is the reason.</summary>
    public static string AddData_Failed => Get(nameof(AddData_Failed));

    /// <summary>Gets the message when a chart server fails to start: <c>{0}</c> is the layer, <c>{1}</c> the reason.</summary>
    public static string AddData_ServerFailed => Get(nameof(AddData_ServerFailed));

    /// <summary>Gets the not-for-navigation line in the add summary.</summary>
    public static string AddData_NotForNavigation => Get(nameof(AddData_NotForNavigation));

    /// <summary>Gets the message when there's nothing to zoom to.</summary>
    public static string ZoomToData_NoExtent => Get(nameof(ZoomToData_NoExtent));

    /// <summary>Gets the line after the listed warnings: <c>{0}</c> is how many more are in the log.</summary>
    public static string AddData_MoreInLog => Get(nameof(AddData_MoreInLog));

    /// <summary>Gets the export window's title.</summary>
    public static string Export_Title => Get(nameof(Export_Title));

    /// <summary>Gets the export window's heading: <c>{0}</c> is the layout's name.</summary>
    public static string Export_Heading => Get(nameof(Export_Heading));

    /// <summary>Gets the summary for one rendered layer.</summary>
    public static string Export_OneLayer => Get(nameof(Export_OneLayer));

    /// <summary>Gets the summary for several rendered layers: <c>{0}</c> is the count.</summary>
    public static string Export_LayerCount => Get(nameof(Export_LayerCount));

    /// <summary>Gets the label of the output file box.</summary>
    public static string Export_FileLabel => Get(nameof(Export_FileLabel));

    /// <summary>Gets the label of the format list.</summary>
    public static string Export_FormatLabel => Get(nameof(Export_FormatLabel));

    /// <summary>Gets the label of the resolution list.</summary>
    public static string Export_ResolutionLabel => Get(nameof(Export_ResolutionLabel));

    /// <summary>Gets a resolution: <c>{0}</c> is dots per inch.</summary>
    public static string Export_ResolutionFormat => Get(nameof(Export_ResolutionFormat));

    /// <summary>Gets the PDF format's name.</summary>
    public static string Export_FormatPdf => Get(nameof(Export_FormatPdf));

    /// <summary>Gets the PNG format's name.</summary>
    public static string Export_FormatPng => Get(nameof(Export_FormatPng));

    /// <summary>Gets the JPEG format's name.</summary>
    public static string Export_FormatJpeg => Get(nameof(Export_FormatJpeg));

    /// <summary>Gets the TIFF format's name.</summary>
    public static string Export_FormatTiff => Get(nameof(Export_FormatTiff));

    /// <summary>Gets the save dialog filter for PDF.</summary>
    public static string Export_FilterPdf => Get(nameof(Export_FilterPdf));

    /// <summary>Gets the save dialog filter for PNG.</summary>
    public static string Export_FilterPng => Get(nameof(Export_FilterPng));

    /// <summary>Gets the save dialog filter for JPEG.</summary>
    public static string Export_FilterJpeg => Get(nameof(Export_FilterJpeg));

    /// <summary>Gets the save dialog filter for TIFF.</summary>
    public static string Export_FilterTiff => Get(nameof(Export_FilterTiff));

    /// <summary>Gets the save dialog's title.</summary>
    public static string Export_FileDialogTitle => Get(nameof(Export_FileDialogTitle));

    /// <summary>Gets the note under the export choices.</summary>
    public static string Export_Note => Get(nameof(Export_Note));

    /// <summary>Gets the message when a layout has no SoundCharts layers to export.</summary>
    public static string Export_NoLayers => Get(nameof(Export_NoLayers));

    /// <summary>Gets the question offering the not-for-navigation text: <c>{0}</c> is the text.</summary>
    public static string Export_NoticeQuestion => Get(nameof(Export_NoticeQuestion));

    /// <summary>Gets the not-for-navigation layout text: <c>{0}</c> is the producers.</summary>
    public static string Export_NoticeText => Get(nameof(Export_NoticeText));

    /// <summary>Gets the progress message while an export renders.</summary>
    public static string Export_Progress => Get(nameof(Export_Progress));

    /// <summary>Gets the message after an export: <c>{0}</c> is the file.</summary>
    public static string Export_Done => Get(nameof(Export_Done));

    /// <summary>Gets the message after an export with layers drawn from tiles: <c>{0}</c> is the file, <c>{1}</c> the count.</summary>
    public static string Export_Partial => Get(nameof(Export_Partial));

    /// <summary>Gets the message when Pro doesn't write the file: <c>{0}</c> is the file.</summary>
    public static string Export_NotWritten => Get(nameof(Export_NotWritten));

    /// <summary>Gets the message when the export fails: <c>{0}</c> is the reason.</summary>
    public static string Export_Failed => Get(nameof(Export_Failed));

    /// <summary>Gets the Browse button's text.</summary>
    public static string Button_Browse => Get(nameof(Button_Browse));

    /// <summary>Gets the Export button's text.</summary>
    public static string Button_Export => Get(nameof(Button_Export));

    /// <summary>Gets the Cancel button's text.</summary>
    public static string Button_Cancel => Get(nameof(Button_Cancel));

    /// <summary>Gets the export Browse button's tooltip.</summary>
    public static string Tooltip_ExportBrowse => Get(nameof(Tooltip_ExportBrowse));

    /// <summary>Gets the format list's tooltip.</summary>
    public static string Tooltip_ExportFormat => Get(nameof(Tooltip_ExportFormat));

    /// <summary>Gets the resolution list's tooltip.</summary>
    public static string Tooltip_ExportResolution => Get(nameof(Tooltip_ExportResolution));

    private static string Get(string name) =>
        Manager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
}
