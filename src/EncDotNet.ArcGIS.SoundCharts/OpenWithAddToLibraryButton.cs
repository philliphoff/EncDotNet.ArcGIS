using ArcGIS.Desktop.Framework.Contracts;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.OpenWith;

namespace EncDotNet.SoundCharts;

/// <summary>
/// <b>Open with SoundCharts</b> &gt; <b>Add to Library</b>
/// (<c>SoundCharts_OpenWithAddToLibrary</c> in Config.daml): adds the
/// right-clicked Catalog items to the SoundCharts Library, indexed in place
/// without loading them, and shows them in the Library pane (issue #14).
/// </summary>
internal sealed class OpenWithAddToLibraryButton : Button
{
    protected override async void OnClick()
    {
        try
        {
            var paths = OpenWithTargets.Paths;
            if (paths.Count == 0)
            {
                return;
            }

            AddInLog.Write($"Open with SoundCharts: adding {string.Join(", ", paths)} to the Library.");
            await LibraryHost.AddAsync(paths);
        }
        catch (Exception ex)
        {
            // The boundary with Pro: log rather than let the exception reach Pro.
            AddInLog.Write(LogLevel.Error, $"Open with SoundCharts > Add to Library failed: {ex}");
        }
    }
}
