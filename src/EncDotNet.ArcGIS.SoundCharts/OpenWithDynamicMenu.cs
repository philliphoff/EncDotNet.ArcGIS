using ArcGIS.Desktop.Framework.Contracts;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.OpenWith;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>Open with SoundCharts</b> on Pro's Catalog context menus
/// (<c>SoundCharts_OpenWithDynamicMenu</c> in Config.daml, issue #6): an
/// inline dynamic menu that adds the <c>SoundCharts_OpenWithMenu</c>
/// submenu only when the right-clicked items hold chart data.
/// </summary>
/// <remarks>
/// DAML can only hide a command for good or disable it, and its conditions
/// can't see what was right-clicked. A dynamic menu that adds nothing is
/// how the command stays out of the menu for files and folders that aren't
/// S-57 or S-1xx data. What the items are comes from their content
/// (<see cref="ContentProbe"/>), never their extension: the add-in claims no
/// file types (epic #1).
/// </remarks>
internal sealed class OpenWithDynamicMenu : DynamicMenu
{
    /// <summary>The DAML id of the submenu offered for chart data.</summary>
    private const string OpenWithMenuId = "SoundCharts_OpenWithMenu";

    private static readonly Lazy<ContentProbe> Probe = new(() => new ContentProbe());

    protected override void OnPopup()
    {
        try
        {
            var paths = CatalogSelection.RightClickedPaths(ContextItem);
            var targets = paths.Count == 0 ? [] : Probe.Value.ChartData(paths);
            OpenWithTargets.Set(targets);
            if (paths.Count > 0)
            {
                AddInLog.Write(
                    LogLevel.Debug,
                    $"Open with SoundCharts: {targets.Count} of {paths.Count} item(s) hold chart data" +
                    (targets.Count == 0 ? "." : ": " + string.Join(", ", targets.Select(t => $"{t.Path} ({t.Kind}{(t.Product is null ? string.Empty : ", " + t.Product)})"))));
            }

            if (targets.Count > 0)
            {
                AddReference(OpenWithMenuId);
            }
        }
        catch (Exception ex)
        {
            // The boundary with Pro: a failure leaves the menu without the command.
            OpenWithTargets.Set([]);
            AddInLog.Write(LogLevel.Error, $"Open with SoundCharts failed: {ex}");
        }
    }
}
