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

    private static string Get(string name) =>
        Manager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
}
