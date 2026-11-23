using EncDotNet.S100.Datasets.S124;
using EncDotNet.S100.Datasets.S124.DataModel;

namespace EncDotNet.SoundCharts.ObjectInfo;

/// <summary>
/// Whether an S-124 navigational warning is in force at map time, for
/// Object information (design brief §6.5, D33), from the library's typed
/// S-124 model.
/// </summary>
/// <remarks>
/// <para>
/// The rule combines what the library reads, until it says "in force" itself:
/// </para>
/// <list type="bullet">
/// <item>Not yet published: the preamble's <c>publicationTime</c> (S-124
/// Edition 2.0.0) is after the instant.</item>
/// <item>Cancelled: the preamble's <c>cancellationDate</c> is at or before
/// the instant; the warning is valid before it (Edition 2.0.0 §9.2.1).</item>
/// <item>Out of its dates: every part that gives <c>fixedDateRange</c>s has
/// none containing the instant, and no part is without dates.</item>
/// </list>
/// <para>
/// The server doesn't filter warnings by time yet (U7), so the chart still
/// draws every warning; Object information is where map time shows.
/// </para>
/// </remarks>
public static class NavigationalWarningValidity
{
    /// <summary>Gets whether <paramref name="warning"/> is in force at <paramref name="instant"/>.</summary>
    /// <param name="warning">The warning.</param>
    /// <param name="instant">The map instant (UTC).</param>
    public static bool IsInForce(S124NavigationalWarning warning, DateTime instant)
    {
        ArgumentNullException.ThrowIfNull(warning);
        var at = new DateTimeOffset(DateTime.SpecifyKind(instant, DateTimeKind.Utc));
        return IsInForce(
            warning.Preamble?.PublicationTime,
            warning.Preamble?.CancellationDate,
            [.. warning.Parts.Select(p => p.ValidityRanges)],
            at);
    }

    /// <summary>
    /// Reads the S-124 dataset at <paramref name="path"/> and says whether
    /// its warning is in force at <paramref name="instant"/>.
    /// </summary>
    /// <param name="path">The S-124 dataset's path.</param>
    /// <param name="instant">The map instant (UTC).</param>
    /// <returns>Whether it's in force.</returns>
    public static bool IsInForce(string path, DateTime instant)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var warning = S124NavigationalWarning.From(S124Dataset.Open(path), out _);
        return IsInForce(warning, instant);
    }

    /// <summary>The rule, on the values it reads, so it's tested without a dataset.</summary>
    /// <param name="published">When the warning was published, or <see langword="null"/>.</param>
    /// <param name="cancelled">When the warning cancels itself, or <see langword="null"/>.</param>
    /// <param name="partRanges">Each part's validity ranges; an empty list is a part without dates.</param>
    /// <param name="instant">The instant.</param>
    internal static bool IsInForce(
        DateTimeOffset? published,
        DateTimeOffset? cancelled,
        IReadOnlyList<IReadOnlyList<S124DateRange>> partRanges,
        DateTimeOffset instant)
    {
        if (published is { } p && p > instant)
        {
            return false;
        }

        if (cancelled is { } c && c <= instant)
        {
            return false;
        }

        return partRanges.Count == 0
            || partRanges.Any(ranges => ranges.Count == 0 || ranges.Any(r => r.Contains(instant)));
    }
}
