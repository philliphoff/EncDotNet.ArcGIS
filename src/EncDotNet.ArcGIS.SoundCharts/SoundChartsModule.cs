using ArcGIS.Desktop.Framework.Contracts;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The SoundCharts add-in module (<c>SoundCharts_Module</c> in Config.daml).
/// Pro creates it when it starts, because the module is <c>autoLoad</c>.
/// </summary>
internal sealed class SoundChartsModule : Module
{
    protected override bool Initialize()
    {
        try
        {
            AddInLog.Write($"SoundCharts loaded: {AboutInfo.Collect().ToSingleLine()}");
        }
        catch (Exception ex)
        {
            // The boundary with Pro: a failure here must not stop the module loading.
            AddInLog.Write($"SoundCharts loaded; collecting versions failed: {ex}");
        }

        return base.Initialize();
    }

    protected override bool CanUnload() => true;
}
