namespace EncDotNet.SoundCharts.ObjectInfo;

/// <summary>
/// A dataset drawn by a SoundCharts layer, as Object information needs it:
/// where the file is and how to name it.
/// </summary>
/// <param name="Path">The dataset file's full path.</param>
/// <param name="DisplayName">
/// The name to show for the dataset, such as the cell name; <c>null</c> to
/// use the file name.
/// </param>
/// <param name="NotForNavigation">
/// <c>true</c> when the data provider marks the dataset not for navigation
/// (from the exchange set or the Library source), so Object information shows
/// the <b>NOT FOR NAVIGATION</b> strip (design brief §7.1, D34).
/// </param>
public sealed record ChartDataset(string Path, string? DisplayName = null, bool NotForNavigation = false)
{
    /// <summary>
    /// Gets the name to show: <see cref="DisplayName"/> when set, otherwise the
    /// file name without its folder.
    /// </summary>
    public string Name => DisplayName ?? System.IO.Path.GetFileName(Path);
}
