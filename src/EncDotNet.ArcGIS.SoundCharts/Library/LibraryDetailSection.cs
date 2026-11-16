using EncDotNet.S100.Collections.Library;

namespace EncDotNet.SoundCharts.Library;

/// <summary>A group of the Library's details (Product, Time, Source, Validation), with a section header.</summary>
/// <param name="Title">The group's title.</param>
/// <param name="Header">The title as the section header shows it, in capitals.</param>
/// <param name="Fields">Its rows, in order.</param>
public sealed record LibraryDetailSection(string Title, string Header, IReadOnlyList<LibraryDetailRow> Fields)
{
    /// <summary>Gets a section for one of EncDotNet.S100's groups, its fields as plain rows.</summary>
    /// <param name="group">The group.</param>
    /// <returns>The section.</returns>
    public static LibraryDetailSection From(LibraryDetailGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        return new LibraryDetailSection(group.Title, group.Header, [.. group.Fields.Select(LibraryDetailRow.From)]);
    }

    /// <summary>Gets a section with a header in EncDotNet.S100's style.</summary>
    /// <param name="title">The section's title.</param>
    /// <param name="fields">Its rows.</param>
    /// <returns>The section.</returns>
    public static LibraryDetailSection Create(string title, IReadOnlyList<LibraryDetailRow> fields)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(fields);
        return new LibraryDetailSection(title, new LibraryDetailGroup(title, []).Header, fields);
    }
}
