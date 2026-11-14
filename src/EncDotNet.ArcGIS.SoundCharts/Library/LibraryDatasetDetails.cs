using EncDotNet.S100.Collections.Library;

namespace EncDotNet.SoundCharts.Library;

/// <summary>
/// The details of the dataset selected in the Library: product, edition and
/// compatibility, extent, time, updates, source and validation status, in
/// groups as the viewer's details pane (issue #14).
/// </summary>
/// <param name="Name">The dataset's name.</param>
/// <param name="Product">Its product, as <c>S-101 · Electronic Navigational Chart (2.0.0)</c>.</param>
/// <param name="Groups">The detail groups, empty ones left out.</param>
/// <param name="NotForNavigation">Whether the source marks it not for navigation.</param>
public sealed record LibraryDatasetDetails(
    string Name,
    string Product,
    IReadOnlyList<LibraryDetailGroup> Groups,
    bool NotForNavigation);
