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

    /// <summary>Gets a count of one dataset.</summary>
    public static string Library_OneDataset => Get(nameof(Library_OneDataset));

    /// <summary>Gets a count of datasets: <c>{0}</c> is the count.</summary>
    public static string Library_DatasetCountFormat => Get(nameof(Library_DatasetCountFormat));

    /// <summary>Gets a dataset's edition: <c>{0}</c> is the edition.</summary>
    public static string Library_EditionFormat => Get(nameof(Library_EditionFormat));

    /// <summary>Gets a dataset's edition and update: <c>{0}</c> is the edition, <c>{1}</c> the update.</summary>
    public static string Library_EditionUpdateFormat => Get(nameof(Library_EditionUpdateFormat));

    /// <summary>Gets the tag of a dataset whose file is gone.</summary>
    public static string Library_Tag_Missing => Get(nameof(Library_Tag_Missing));

    /// <summary>Gets the not-for-navigation notice and tag.</summary>
    public static string Library_NotForNavigation => Get(nameof(Library_NotForNavigation));

    /// <summary>Gets the heading of the validation details.</summary>
    public static string Library_Group_Validation => Get(nameof(Library_Group_Validation));

    /// <summary>Gets the heading of the time details.</summary>
    public static string Library_Group_Time => Get(nameof(Library_Group_Time));

    /// <summary>Gets the label of the edition compatibility.</summary>
    public static string Library_Field_Compatibility => Get(nameof(Library_Field_Compatibility));

    /// <summary>Gets the label of the validation status.</summary>
    public static string Library_Field_Status => Get(nameof(Library_Field_Status));

    /// <summary>Gets the label of the time a dataset covers.</summary>
    public static string Library_Field_TimeRange => Get(nameof(Library_Field_TimeRange));

    /// <summary>Gets the label of a dataset's time steps.</summary>
    public static string Library_Field_TimeSteps => Get(nameof(Library_Field_TimeSteps));

    /// <summary>Gets the compatibility of an edition SoundCharts reads.</summary>
    public static string Library_Compatibility_Supported => Get(nameof(Library_Compatibility_Supported));

    /// <summary>Gets the compatibility of an older compatible edition: <c>{0}</c> is the edition SoundCharts reads.</summary>
    public static string Library_Compatibility_NewerFormat => Get(nameof(Library_Compatibility_NewerFormat));

    /// <summary>Gets the compatibility of a newer edition on the same major: <c>{0}</c> is the edition SoundCharts reads.</summary>
    public static string Library_Compatibility_OlderFormat => Get(nameof(Library_Compatibility_OlderFormat));

    /// <summary>Gets the compatibility of an edition on another major: <c>{0}</c> is the edition SoundCharts reads.</summary>
    public static string Library_Compatibility_UnsupportedFormat => Get(nameof(Library_Compatibility_UnsupportedFormat));

    /// <summary>Gets the compatibility when the dataset states no edition: <c>{0}</c> is the edition SoundCharts reads.</summary>
    public static string Library_Compatibility_UnknownFormat => Get(nameof(Library_Compatibility_UnknownFormat));

    /// <summary>Gets the status of a dataset not validated yet.</summary>
    public static string Library_Validation_NotValidated => Get(nameof(Library_Validation_NotValidated));

    /// <summary>Gets the status while validation runs.</summary>
    public static string Library_Validation_Running => Get(nameof(Library_Validation_Running));

    /// <summary>Gets the status of a valid dataset.</summary>
    public static string Library_Validation_Valid => Get(nameof(Library_Validation_Valid));

    /// <summary>Gets the status of a valid dataset: <c>{0}</c> is the number of rules.</summary>
    public static string Library_Validation_ValidRulesFormat => Get(nameof(Library_Validation_ValidRulesFormat));

    /// <summary>Gets the status of a dataset that failed validation without counted findings.</summary>
    public static string Library_Validation_Invalid => Get(nameof(Library_Validation_Invalid));

    /// <summary>Gets the status when the product has no rules.</summary>
    public static string Library_Validation_NoRules => Get(nameof(Library_Validation_NoRules));

    /// <summary>Gets the status when validation couldn't run: <c>{0}</c> is why.</summary>
    public static string Library_Validation_FailedFormat => Get(nameof(Library_Validation_FailedFormat));

    /// <summary>Gets a count of one error.</summary>
    public static string Library_Validation_OneError => Get(nameof(Library_Validation_OneError));

    /// <summary>Gets a count of errors: <c>{0}</c> is the count.</summary>
    public static string Library_Validation_ErrorsFormat => Get(nameof(Library_Validation_ErrorsFormat));

    /// <summary>Gets a count of one warning.</summary>
    public static string Library_Validation_OneWarning => Get(nameof(Library_Validation_OneWarning));

    /// <summary>Gets a count of warnings: <c>{0}</c> is the count.</summary>
    public static string Library_Validation_WarningsFormat => Get(nameof(Library_Validation_WarningsFormat));

    /// <summary>Gets a finding: <c>{0}</c> is its severity, <c>{1}</c> its message.</summary>
    public static string Library_Validation_FindingFormat => Get(nameof(Library_Validation_FindingFormat));

    /// <summary>Gets why validation stopped: <c>{0}</c> is the time limit in minutes.</summary>
    public static string Library_ValidationTimedOut => Get(nameof(Library_ValidationTimedOut));

    /// <summary>Gets why validation failed without a message: <c>{0}</c> is the exit code.</summary>
    public static string Library_ValidationExitCode => Get(nameof(Library_ValidationExitCode));

    /// <summary>Gets why validation couldn't run: <c>{0}</c> is the program.</summary>
    public static string Library_ValidationNotStarted => Get(nameof(Library_ValidationNotStarted));

    /// <summary>Gets the message when there's no s100 to validate with.</summary>
    public static string Library_ValidationNoServer => Get(nameof(Library_ValidationNoServer));

    /// <summary>Gets the message when a zipped dataset is validated.</summary>
    public static string Library_ValidationZip => Get(nameof(Library_ValidationZip));

    /// <summary>Gets the time steps of a dataset without any.</summary>
    public static string Library_TimeSteps_None => Get(nameof(Library_TimeSteps_None));

    /// <summary>Gets one time step.</summary>
    public static string Library_TimeSteps_One => Get(nameof(Library_TimeSteps_One));

    /// <summary>Gets a count of time steps: <c>{0}</c> is the count.</summary>
    public static string Library_TimeSteps_CountFormat => Get(nameof(Library_TimeSteps_CountFormat));

    /// <summary>Gets an hourly step interval.</summary>
    public static string Library_TimeSteps_Hourly => Get(nameof(Library_TimeSteps_Hourly));

    /// <summary>Gets a daily step interval.</summary>
    public static string Library_TimeSteps_Daily => Get(nameof(Library_TimeSteps_Daily));

    /// <summary>Gets a step interval in hours: <c>{0}</c> is the hours.</summary>
    public static string Library_TimeSteps_EveryHoursFormat => Get(nameof(Library_TimeSteps_EveryHoursFormat));

    /// <summary>Gets a step interval in minutes: <c>{0}</c> is the minutes.</summary>
    public static string Library_TimeSteps_EveryMinutesFormat => Get(nameof(Library_TimeSteps_EveryMinutesFormat));

    /// <summary>Gets the time steps while they're read.</summary>
    public static string Library_TimeSteps_Reading => Get(nameof(Library_TimeSteps_Reading));

    /// <summary>Gets the heading of the empty Library.</summary>
    public static string Library_EmptyTitle => Get(nameof(Library_EmptyTitle));

    /// <summary>Gets the text of the empty Library.</summary>
    public static string Library_EmptyDescription => Get(nameof(Library_EmptyDescription));

    /// <summary>Gets the hint that files can be dropped on the pane.</summary>
    public static string Library_DropHint => Get(nameof(Library_DropHint));

    /// <summary>Gets the filter box's placeholder.</summary>
    public static string Library_FilterWatermark => Get(nameof(Library_FilterWatermark));

    /// <summary>Gets the filter box's accessible name.</summary>
    public static string Library_Filter => Get(nameof(Library_Filter));

    /// <summary>Gets the tree's accessible name.</summary>
    public static string Library_Tree => Get(nameof(Library_Tree));

    /// <summary>Gets the details panel's accessible name.</summary>
    public static string Library_Details => Get(nameof(Library_Details));

    /// <summary>Gets the details panel's text without a selection.</summary>
    public static string Library_SelectItemPlaceholder => Get(nameof(Library_SelectItemPlaceholder));

    /// <summary>Gets the Add a folder button's text.</summary>
    public static string Library_AddFolder => Get(nameof(Library_AddFolder));

    /// <summary>Gets the Add an exchange set button's text.</summary>
    public static string Library_AddExchangeSet => Get(nameof(Library_AddExchangeSet));

    /// <summary>Gets the Add a collection manifest button's text.</summary>
    public static string Library_AddManifest => Get(nameof(Library_AddManifest));

    /// <summary>Gets the Refresh button's text.</summary>
    public static string Library_Refresh => Get(nameof(Library_Refresh));

    /// <summary>Gets the Remove button's text.</summary>
    public static string Library_Remove => Get(nameof(Library_Remove));

    /// <summary>Gets the Add to Map button's text.</summary>
    public static string Library_AddToMap => Get(nameof(Library_AddToMap));

    /// <summary>Gets the Show Footprint button's text.</summary>
    public static string Library_ShowFootprint => Get(nameof(Library_ShowFootprint));

    /// <summary>Gets the Validate button's text.</summary>
    public static string Library_Validate => Get(nameof(Library_Validate));

    /// <summary>Gets the Open Containing Folder button's text.</summary>
    public static string Library_OpenContainingFolder => Get(nameof(Library_OpenContainingFolder));

    /// <summary>Gets the Add a folder button's tooltip.</summary>
    public static string Tooltip_LibraryAddFolder => Get(nameof(Tooltip_LibraryAddFolder));

    /// <summary>Gets the Add an exchange set button's tooltip.</summary>
    public static string Tooltip_LibraryAddExchangeSet => Get(nameof(Tooltip_LibraryAddExchangeSet));

    /// <summary>Gets the Add a collection manifest button's tooltip.</summary>
    public static string Tooltip_LibraryAddManifest => Get(nameof(Tooltip_LibraryAddManifest));

    /// <summary>Gets the Refresh button's tooltip.</summary>
    public static string Tooltip_LibraryRefresh => Get(nameof(Tooltip_LibraryRefresh));

    /// <summary>Gets the Remove button's tooltip.</summary>
    public static string Tooltip_LibraryRemove => Get(nameof(Tooltip_LibraryRemove));

    /// <summary>Gets the Add to Map button's tooltip.</summary>
    public static string Tooltip_LibraryAddToMap => Get(nameof(Tooltip_LibraryAddToMap));

    /// <summary>Gets the Show Footprint button's tooltip.</summary>
    public static string Tooltip_LibraryShowFootprint => Get(nameof(Tooltip_LibraryShowFootprint));

    /// <summary>Gets the Validate button's tooltip.</summary>
    public static string Tooltip_LibraryValidate => Get(nameof(Tooltip_LibraryValidate));

    /// <summary>Gets the Open Containing Folder button's tooltip.</summary>
    public static string Tooltip_LibraryOpenContainingFolder => Get(nameof(Tooltip_LibraryOpenContainingFolder));

    /// <summary>Gets the folder dialog's title.</summary>
    public static string Library_AddFolderTitle => Get(nameof(Library_AddFolderTitle));

    /// <summary>Gets the exchange set dialog's title.</summary>
    public static string Library_AddExchangeSetTitle => Get(nameof(Library_AddExchangeSetTitle));

    /// <summary>Gets the collection manifest dialog's title.</summary>
    public static string Library_AddManifestTitle => Get(nameof(Library_AddManifestTitle));

    /// <summary>Gets the exchange set dialog's file filter.</summary>
    public static string Library_ExchangeSetFilter => Get(nameof(Library_ExchangeSetFilter));

    /// <summary>Gets the collection manifest dialog's file filter.</summary>
    public static string Library_ManifestFilter => Get(nameof(Library_ManifestFilter));

    /// <summary>Gets the caption of messages from the Library.</summary>
    public static string Library_Caption => Get(nameof(Library_Caption));

    /// <summary>Gets the heading of the list of paths not added.</summary>
    public static string Library_ImportProblemsHeading => Get(nameof(Library_ImportProblemsHeading));

    /// <summary>Gets why a path wasn't added: <c>{0}</c> is the path.</summary>
    public static string Library_Problem_NotFoundFormat => Get(nameof(Library_Problem_NotFoundFormat));

    /// <summary>Gets why a path wasn't added: <c>{0}</c> is the path.</summary>
    public static string Library_Problem_AlreadyInLibraryFormat => Get(nameof(Library_Problem_AlreadyInLibraryFormat));

    /// <summary>Gets why a path wasn't added: <c>{0}</c> is the path, <c>{1}</c> why.</summary>
    public static string Library_Problem_UnreadableFormat => Get(nameof(Library_Problem_UnreadableFormat));

    /// <summary>Gets the question before removing: <c>{0}</c> is the collection or source.</summary>
    public static string Library_RemoveConfirmFormat => Get(nameof(Library_RemoveConfirmFormat));

    /// <summary>Gets the message when there's no map for Show Footprint.</summary>
    public static string Library_NoMap => Get(nameof(Library_NoMap));

    /// <summary>Gets the message when a dataset has no footprint.</summary>
    public static string Library_NoFootprint => Get(nameof(Library_NoFootprint));

    /// <summary>Gets the message when a dataset's file is gone: <c>{0}</c> is the path.</summary>
    public static string Library_FileMissingFormat => Get(nameof(Library_FileMissingFormat));

    /// <summary>Gets the message when an action fails: <c>{0}</c> is why.</summary>
    public static string Library_ActionFailedFormat => Get(nameof(Library_ActionFailedFormat));

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
