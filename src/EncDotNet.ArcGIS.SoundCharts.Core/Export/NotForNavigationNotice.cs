using System.Text.RegularExpressions;

namespace EncDotNet.SoundCharts.Export;

/// <summary>
/// Whether a layout already says its chart is not for navigation (design
/// brief §7.1, D34): <b>Export SoundCharts Chart...</b> offers to add the
/// notice only when no text element says it.
/// </summary>
public static class NotForNavigationNotice
{
    /// <summary>The words a layout text element must hold, in any case.</summary>
    public const string Phrase = "not for navigation";

    /// <summary>Gets whether any of <paramref name="texts"/> holds <see cref="Phrase"/>.</summary>
    /// <param name="texts">The layout's text elements' text.</param>
    public static bool IsPresent(IEnumerable<string?> texts)
    {
        ArgumentNullException.ThrowIfNull(texts);
        return texts.Any(text => text is not null && Normalize(text).Contains(Phrase, StringComparison.OrdinalIgnoreCase));
    }

    // Layout text can break lines or carry formatting tags between the words.
    private static string Normalize(string text)
    {
        var plain = Regex.Replace(text, "<[^>]*>", " ");
        return string.Join(' ', plain.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
