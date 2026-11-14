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

    /// <summary>Gets the message for a file that isn't a route (the viewer's).</summary>
    public static string RouteCheck_NotARoute => Get(nameof(RouteCheck_NotARoute));

    /// <summary>Gets the message for a route with fewer than two waypoints (the viewer's).</summary>
    public static string RouteCheck_TooFewWaypoints => Get(nameof(RouteCheck_TooFewWaypoints));

    /// <summary>Gets the line for a dataset that couldn't be read: <c>{0}</c> is the dataset, <c>{1}</c> the reason.</summary>
    public static string RouteCheck_DatasetProblemFormat => Get(nameof(RouteCheck_DatasetProblemFormat));

    /// <summary>Gets the footer's name for weather forecast data (the viewer's).</summary>
    public static string RouteCheck_FooterWeather => Get(nameof(RouteCheck_FooterWeather));

    /// <summary>Gets the footer's name for tropical cyclone advisories (the viewer's).</summary>
    public static string RouteCheck_FooterCyclones => Get(nameof(RouteCheck_FooterCyclones));

    /// <summary>Gets the MUST REVIEW group heading (the viewer's).</summary>
    public static string RouteCheck_GroupMustReview => Get(nameof(RouteCheck_GroupMustReview));

    /// <summary>Gets the CHECK group heading (the viewer's).</summary>
    public static string RouteCheck_GroupCheck => Get(nameof(RouteCheck_GroupCheck));

    /// <summary>Gets the INFO group heading (the viewer's).</summary>
    public static string RouteCheck_GroupInfo => Get(nameof(RouteCheck_GroupInfo));

    /// <summary>Gets the Clearance finding tag (the viewer's).</summary>
    public static string RouteCheck_TagClearance => Get(nameof(RouteCheck_TagClearance));

    /// <summary>Gets the Route check finding tag (the viewer's).</summary>
    public static string RouteCheck_TagRouteCheck => Get(nameof(RouteCheck_TagRouteCheck));

    /// <summary>Gets the S-124 finding tag (the viewer's).</summary>
    public static string RouteCheck_TagS124 => Get(nameof(RouteCheck_TagS124));

    /// <summary>Gets the Confidence finding tag (the viewer's).</summary>
    public static string RouteCheck_TagConfidence => Get(nameof(RouteCheck_TagConfidence));

    /// <summary>Gets the Weather finding tag (the viewer's).</summary>
    public static string RouteCheck_TagWeather => Get(nameof(RouteCheck_TagWeather));

    /// <summary>Gets the Data finding tag (the viewer's).</summary>
    public static string RouteCheck_TagData => Get(nameof(RouteCheck_TagData));

    /// <summary>Gets the findings group layer's name: <c>{0}</c> is the route.</summary>
    public static string RouteCheck_GroupLayerFormat => Get(nameof(RouteCheck_GroupLayerFormat));

    /// <summary>Gets the point findings layer's name.</summary>
    public static string RouteCheck_FindingsLayerPoints => Get(nameof(RouteCheck_FindingsLayerPoints));

    /// <summary>Gets the segment findings layer's name.</summary>
    public static string RouteCheck_FindingsLayerSegments => Get(nameof(RouteCheck_FindingsLayerSegments));

    /// <summary>Gets the pane's heading before any check.</summary>
    public static string RouteCheck_EmptyTitle => Get(nameof(RouteCheck_EmptyTitle));

    /// <summary>Gets the pane's description before any check.</summary>
    public static string RouteCheck_EmptyDescription => Get(nameof(RouteCheck_EmptyDescription));

    /// <summary>Gets the line while a check runs (the viewer's).</summary>
    public static string RouteCheck_Computing => Get(nameof(RouteCheck_Computing));

    /// <summary>Gets the line when a check fails: <c>{0}</c> is the reason (the viewer's).</summary>
    public static string RouteCheck_ErrorFormat => Get(nameof(RouteCheck_ErrorFormat));

    /// <summary>Gets the verdict when water-level data ends before arrival (the viewer's).</summary>
    public static string RouteCheck_VerdictWaterLevelEnds => Get(nameof(RouteCheck_VerdictWaterLevelEnds));

    /// <summary>Gets the verdict below the UKC setting in one place (the viewer's).</summary>
    public static string RouteCheck_VerdictBelowOne => Get(nameof(RouteCheck_VerdictBelowOne));

    /// <summary>Gets the verdict below the UKC setting: <c>{0}</c> is how many places (the viewer's).</summary>
    public static string RouteCheck_VerdictBelowFormat => Get(nameof(RouteCheck_VerdictBelowFormat));

    /// <summary>Gets the verdict with no data for part of the passage (the viewer's).</summary>
    public static string RouteCheck_VerdictIncomplete => Get(nameof(RouteCheck_VerdictIncomplete));

    /// <summary>Gets the tight verdict (the viewer's).</summary>
    public static string RouteCheck_VerdictTight => Get(nameof(RouteCheck_VerdictTight));

    /// <summary>Gets the verdict that meets the settings (the viewer's).</summary>
    public static string RouteCheck_VerdictMeets => Get(nameof(RouteCheck_VerdictMeets));

    /// <summary>Gets the confidence note for S-101 depths: <c>{0}</c> is nautical miles (the viewer's).</summary>
    public static string RouteCheck_ConfidenceS101Format => Get(nameof(RouteCheck_ConfidenceS101Format));

    /// <summary>Gets the confidence note for unconfirmed datums (the viewer's).</summary>
    public static string RouteCheck_ConfidenceDatumUnconfirmed => Get(nameof(RouteCheck_ConfidenceDatumUnconfirmed));

    /// <summary>Gets the confidence note for predicted water level: <c>{0}</c> is nautical miles (the viewer's).</summary>
    public static string RouteCheck_ConfidencePredictedFormat => Get(nameof(RouteCheck_ConfidencePredictedFormat));

    /// <summary>Gets the count to review: <c>{0}</c> (the viewer's).</summary>
    public static string RouteCheck_ToReviewFormat => Get(nameof(RouteCheck_ToReviewFormat));

    /// <summary>Gets the count to check: <c>{0}</c> (the viewer's).</summary>
    public static string RouteCheck_ToCheckFormat => Get(nameof(RouteCheck_ToCheckFormat));

    /// <summary>Gets the tail of a list: <c>{0}</c> more (the viewer's).</summary>
    public static string RouteCheck_MoreFormat => Get(nameof(RouteCheck_MoreFormat));

    /// <summary>Gets the verdict with findings to check: <c>{0}</c> (the viewer's).</summary>
    public static string RouteCheck_VerdictCheckFormat => Get(nameof(RouteCheck_VerdictCheckFormat));

    /// <summary>Gets the subtitle with more to review: <c>{0}</c> is where, <c>{1}</c> how many more (the viewer's).</summary>
    public static string RouteCheck_AndMoreToReviewFormat => Get(nameof(RouteCheck_AndMoreToReviewFormat));

    /// <summary>Gets the verdict without a draft and without findings (the viewer's).</summary>
    public static string RouteCheck_NoDraftVerdictNone => Get(nameof(RouteCheck_NoDraftVerdictNone));

    /// <summary>Gets the subtitle without a draft and without findings (the viewer's).</summary>
    public static string RouteCheck_ClearNoDraft => Get(nameof(RouteCheck_ClearNoDraft));

    /// <summary>Gets the separator between parts of a line (the viewer's).</summary>
    public static string RouteCheck_Separator => Get(nameof(RouteCheck_Separator));

    /// <summary>Gets the Clearance summary row label (the viewer's).</summary>
    public static string RouteCheck_SummaryClearance => Get(nameof(RouteCheck_SummaryClearance));

    /// <summary>Gets the Route summary row label (the viewer's).</summary>
    public static string RouteCheck_SummaryRoute => Get(nameof(RouteCheck_SummaryRoute));

    /// <summary>Gets the Warnings summary row label (the viewer's).</summary>
    public static string RouteCheck_SummaryWarnings => Get(nameof(RouteCheck_SummaryWarnings));

    /// <summary>Gets the Weather summary row label (the viewer's).</summary>
    public static string RouteCheck_SummaryWeather => Get(nameof(RouteCheck_SummaryWeather));

    /// <summary>Gets the clearance row without a draft (the viewer's).</summary>
    public static string RouteCheck_SummaryNotChecked => Get(nameof(RouteCheck_SummaryNotChecked));

    /// <summary>Gets the clearance row without a minimum (the viewer's).</summary>
    public static string RouteCheck_NoClearance => Get(nameof(RouteCheck_NoClearance));

    /// <summary>Gets the clearance row: <c>{0}</c> is the minimum UKC, <c>{1}</c> the required (the viewer's).</summary>
    public static string RouteCheck_SummaryClearanceFormat => Get(nameof(RouteCheck_SummaryClearanceFormat));

    /// <summary>Gets the clearance row without a margin: <c>{0}</c> is the minimum UKC (the viewer's).</summary>
    public static string RouteCheck_SummaryClearanceNoMarginFormat => Get(nameof(RouteCheck_SummaryClearanceNoMarginFormat));

    /// <summary>Gets the clearance chip: <c>{0}</c> places below (the viewer's).</summary>
    public static string RouteCheck_ChipBelowFormat => Get(nameof(RouteCheck_ChipBelowFormat));

    /// <summary>Gets the tight clearance chip (the viewer's).</summary>
    public static string RouteCheck_ChipTight => Get(nameof(RouteCheck_ChipTight));

    /// <summary>Gets the clearance chip without water level (the viewer's).</summary>
    public static string RouteCheck_ChipNoTide => Get(nameof(RouteCheck_ChipNoTide));

    /// <summary>Gets the clearance chip that meets the settings (the viewer's).</summary>
    public static string RouteCheck_ChipMeets => Get(nameof(RouteCheck_ChipMeets));

    /// <summary>Gets the route row without findings (the viewer's).</summary>
    public static string RouteCheck_SummaryNothingFound => Get(nameof(RouteCheck_SummaryNothingFound));

    /// <summary>Gets the warnings row without findings (the viewer's).</summary>
    public static string RouteCheck_SummaryNothingOnRoute => Get(nameof(RouteCheck_SummaryNothingOnRoute));

    /// <summary>Gets the warnings row: <c>{0}</c> near the route (the viewer's).</summary>
    public static string RouteCheck_SummaryNearRouteFormat => Get(nameof(RouteCheck_SummaryNearRouteFormat));

    /// <summary>Gets the warnings row: <c>{0}</c> in force on the route (the viewer's).</summary>
    public static string RouteCheck_SummaryInForceFormat => Get(nameof(RouteCheck_SummaryInForceFormat));

    /// <summary>Gets the warnings row's second part: <c>{0}</c> near (the viewer's).</summary>
    public static string RouteCheck_SummaryNearFormat => Get(nameof(RouteCheck_SummaryNearFormat));

    /// <summary>Gets the weather row: <c>{0}</c> to review (the viewer's).</summary>
    public static string RouteCheck_SummaryWeatherMustReviewFormat => Get(nameof(RouteCheck_SummaryWeatherMustReviewFormat));

    /// <summary>Gets the weather row: <c>{0}</c> above limits (the viewer's).</summary>
    public static string RouteCheck_SummaryWeatherAboveFormat => Get(nameof(RouteCheck_SummaryWeatherAboveFormat));

    /// <summary>Gets the weather row: <c>{0}</c> to check (the viewer's).</summary>
    public static string RouteCheck_SummaryWeatherToCheckFormat => Get(nameof(RouteCheck_SummaryWeatherToCheckFormat));

    /// <summary>Gets the weather row: <c>{0}</c> not checked (the viewer's).</summary>
    public static string RouteCheck_SummaryWeatherNotCheckedFormat => Get(nameof(RouteCheck_SummaryWeatherNotCheckedFormat));

    /// <summary>Gets a leg's name: <c>{0}</c> and <c>{1}</c> are its waypoints (the viewer's).</summary>
    public static string RouteCheck_LegFormat => Get(nameof(RouteCheck_LegFormat));

    /// <summary>Gets the time below the UKC setting: <c>{0}</c> minutes (the viewer's).</summary>
    public static string RouteCheck_MinutesBelowFormat => Get(nameof(RouteCheck_MinutesBelowFormat));

    /// <summary>Gets the footer: <c>{0}</c> is the products used (the viewer's, design brief §7.1).</summary>
    public static string RouteCheck_FooterFormat => Get(nameof(RouteCheck_FooterFormat));

    /// <summary>Gets the footer without data (the viewer's).</summary>
    public static string RouteCheck_FooterNoData => Get(nameof(RouteCheck_FooterNoData));

    /// <summary>Gets the draft used: <c>{0}</c>.</summary>
    public static string RouteCheck_ParameterDraftFormat => Get(nameof(RouteCheck_ParameterDraftFormat));

    /// <summary>Gets the parameters line without a draft (the viewer's).</summary>
    public static string RouteCheck_ParameterNoDraft => Get(nameof(RouteCheck_ParameterNoDraft));

    /// <summary>Gets the UKC margin used: <c>{0}</c>.</summary>
    public static string RouteCheck_ParameterUkcFormat => Get(nameof(RouteCheck_ParameterUkcFormat));

    /// <summary>Gets the UKC margin when none is set (the viewer's).</summary>
    public static string RouteCheck_NoMarginSet => Get(nameof(RouteCheck_NoMarginSet));

    /// <summary>Gets the safety contour used: <c>{0}</c>.</summary>
    public static string RouteCheck_ParameterSafetyContourFormat => Get(nameof(RouteCheck_ParameterSafetyContourFormat));

    /// <summary>Gets the safety depth used: <c>{0}</c>.</summary>
    public static string RouteCheck_ParameterSafetyDepthFormat => Get(nameof(RouteCheck_ParameterSafetyDepthFormat));

    /// <summary>Gets a parameter that came from the route: <c>{0}</c>.</summary>
    public static string RouteCheck_FromRouteFormat => Get(nameof(RouteCheck_FromRouteFormat));

    /// <summary>Gets the Check Selected Line button text.</summary>
    public static string RouteCheck_CheckSelectedLine => Get(nameof(RouteCheck_CheckSelectedLine));

    /// <summary>Gets the Check Selected Line button tooltip.</summary>
    public static string RouteCheck_CheckSelectedLineTooltip => Get(nameof(RouteCheck_CheckSelectedLineTooltip));

    /// <summary>Gets the Draw Route button text.</summary>
    public static string RouteCheck_DrawRoute => Get(nameof(RouteCheck_DrawRoute));

    /// <summary>Gets the Draw Route button tooltip.</summary>
    public static string RouteCheck_DrawRouteTooltip => Get(nameof(RouteCheck_DrawRouteTooltip));

    /// <summary>Gets the Open Route button text.</summary>
    public static string RouteCheck_OpenRoute => Get(nameof(RouteCheck_OpenRoute));

    /// <summary>Gets the Open Route button tooltip.</summary>
    public static string RouteCheck_OpenRouteTooltip => Get(nameof(RouteCheck_OpenRouteTooltip));

    /// <summary>Gets the Open Route dialog's title.</summary>
    public static string RouteCheck_OpenRouteTitle => Get(nameof(RouteCheck_OpenRouteTitle));

    /// <summary>Gets the Open Route dialog's file filter.</summary>
    public static string RouteCheck_OpenRouteFilter => Get(nameof(RouteCheck_OpenRouteFilter));

    /// <summary>Gets the message when no line feature is selected.</summary>
    public static string RouteCheck_NoLineSelected => Get(nameof(RouteCheck_NoLineSelected));

    /// <summary>Gets the message without a map.</summary>
    public static string RouteCheck_NoMap => Get(nameof(RouteCheck_NoMap));

    /// <summary>Gets the findings list's accessible name.</summary>
    public static string RouteCheck_Findings => Get(nameof(RouteCheck_Findings));

    /// <summary>Gets the line when a check found nothing (the viewer's).</summary>
    public static string RouteCheck_Empty => Get(nameof(RouteCheck_Empty));

    /// <summary>Gets the Object information link text.</summary>
    public static string RouteCheck_ObjectInformation => Get(nameof(RouteCheck_ObjectInformation));

    /// <summary>Gets the Object information link tooltip.</summary>
    public static string RouteCheck_ObjectInformationTooltip => Get(nameof(RouteCheck_ObjectInformationTooltip));

    /// <summary>Gets a finding row's tooltip (the viewer's).</summary>
    public static string RouteCheck_ZoomTooltip => Get(nameof(RouteCheck_ZoomTooltip));

    /// <summary>Gets the tooltip of a finding without a place (the viewer's).</summary>
    public static string RouteCheck_NoPlaceTooltip => Get(nameof(RouteCheck_NoPlaceTooltip));

    /// <summary>Gets the message when the findings layers can't be written: <c>{0}</c> is the reason.</summary>
    public static string RouteCheck_FindingsWriteFailedFormat => Get(nameof(RouteCheck_FindingsWriteFailedFormat));

    /// <summary>Gets the heading over datasets that couldn't be read.</summary>
    public static string RouteCheck_ProblemsHeading => Get(nameof(RouteCheck_ProblemsHeading));

    /// <summary>Gets the name of a drawn route.</summary>
    public static string RouteCheck_SketchName => Get(nameof(RouteCheck_SketchName));

    /// <summary>Gets a line feature's route name: <c>{0}</c> is the layer, <c>{1}</c> the feature's object id.</summary>
    public static string RouteCheck_FeatureNameFormat => Get(nameof(RouteCheck_FeatureNameFormat));

    /// <summary>Gets the Check Again button text.</summary>
    public static string RouteCheck_CheckAgain => Get(nameof(RouteCheck_CheckAgain));

    /// <summary>Gets the Check Again button tooltip.</summary>
    public static string RouteCheck_CheckAgainTooltip => Get(nameof(RouteCheck_CheckAgainTooltip));

    private static string Get(string name) =>
        Manager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
}
