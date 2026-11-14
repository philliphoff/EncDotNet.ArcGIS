using System.Globalization;
using ArcGIS.Desktop.Framework;
using EncDotNet.SoundCharts.Logging;
using EncDotNet.SoundCharts.Resources;
using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts;

/// <summary>
/// The Pro notification shown once when a chart server stops after failing
/// (design brief §4.2, D18). Selecting it opens the SoundCharts Log at that
/// chart's lines.
/// </summary>
internal sealed class ChartServerNotification : Notification
{
    private readonly string _logSource;

    private ChartServerNotification(string logSource, string reason)
    {
        _logSource = logSource;
        Title = Strings.Notification_Title;
        Message = string.Format(CultureInfo.CurrentCulture, Strings.Notification_ServerStopped, reason)
            + Environment.NewLine + Strings.Notification_ShowLog;
        ImageUrl = "pack://application:,,,/ArcGIS.Desktop.Resources;component/Images/GenericError32.png";
    }

    /// <summary>Shows the notification for a failed server; raised on a background thread. Never throws.</summary>
    public static void OnServerFailed(object? sender, ChartServerFailedEventArgs e)
    {
        try
        {
            var notification = new ChartServerNotification(e.Server.LogSource, e.Reason);
            FrameworkApplication.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    FrameworkApplication.AddNotification(notification);
                }
                catch (Exception ex)
                {
                    AddInLog.Write(LogLevel.Error, $"Showing the chart server notification failed: {ex}");
                }
            });
        }
        catch (Exception ex)
        {
            AddInLog.Write(LogLevel.Error, $"Showing the chart server notification failed: {ex}");
        }
    }

    protected override void OnClick()
    {
        try
        {
            LogPaneViewModel.Show(_logSource);
        }
        catch (Exception ex)
        {
            AddInLog.Write(LogLevel.Error, $"Opening the SoundCharts Log failed: {ex}");
        }
    }
}
