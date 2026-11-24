using ArcGIS.Desktop.Framework.Threading.Tasks;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts;

/// <summary>
/// Runs work on the thread pool behind Pro's modal progress dialog with a
/// Cancel button. The dialog's status line shows what the work reports.
/// </summary>
/// <remarks>
/// Pro wires a dialog's Cancel button to a
/// <see cref="CancelableProgressor"/> only for work it runs through
/// <see cref="QueuedTask.Run{T}(Func{Task{T}}, CancelableProgressor, TaskCreationOptions)"/>,
/// and shows and hides the dialog with that task. The queued delegate only
/// starts the work on the thread pool and returns its task,
/// so reading files doesn't hold the main CIM thread.
/// </remarks>
internal static class CancelableProgress
{
    /// <summary>
    /// Runs <paramref name="work"/> behind a cancelable progress dialog.
    /// </summary>
    /// <typeparam name="T">The work's result.</typeparam>
    /// <param name="message">The dialog's message.</param>
    /// <param name="work">
    /// The work. It's given a progress sink, whose text becomes the dialog's
    /// status line, and the token the Cancel button cancels.
    /// </param>
    /// <returns>The work's result, or <see langword="null"/> when the user canceled.</returns>
    public static async Task<T?> RunAsync<T>(string message, Func<IProgress<string>, CancellationToken, Task<T>> work)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(work);

        // Shown at once: the work this is for takes long enough to need Cancel.
        using var dialog = new ProgressDialog(message, Strings.Progress_Canceling, false);
        using var source = new CancelableProgressorSource(dialog);
        var progressor = source.Progressor;
        var token = progressor.CancellationToken;
        var status = new StatusProgress(progressor);
        try
        {
            return await QueuedTask.Run(() => Task.Run(() => work(status, token), token), progressor);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return null;
        }
    }

    /// <summary>
    /// Sets the progressor's status straight from the reporting thread, as
    /// work on the main CIM thread does, rather than through the UI thread
    /// that <see cref="Progress{T}"/> would post to.
    /// </summary>
    private sealed class StatusProgress(CancelableProgressor progressor) : IProgress<string>
    {
        public void Report(string value) => progressor.Status = value;
    }
}
