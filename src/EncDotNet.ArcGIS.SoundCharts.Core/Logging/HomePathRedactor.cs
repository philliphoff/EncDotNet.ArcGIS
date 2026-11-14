using System.Text;

namespace EncDotNet.SoundCharts.Logging;

/// <summary>
/// Replaces the user's home folder with <c>~</c> in text the add-in hands
/// out (the SoundCharts Log pane's <b>Copy all</b>), so paths such as
/// <c>C:\Users\alice\Charts\US5MA1BO.000</c> don't reveal the account name.
/// The same rule as the SoundCharts viewer's.
/// </summary>
public static class HomePathRedactor
{
    private static readonly string? CurrentHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>Redacts the current user's home folder in <paramref name="text"/>.</summary>
    /// <param name="text">The text to redact.</param>
    public static string Redact(string text) => Redact(text, CurrentHome);

    /// <summary>
    /// Replaces every occurrence of <paramref name="home"/> in
    /// <paramref name="text"/> that is a whole path prefix (followed by a
    /// separator, a quote, whitespace or the end of the text) with <c>~</c>,
    /// ignoring case, so <c>C:\Users\al</c> doesn't turn
    /// <c>C:\Users\alice</c> into <c>~ice</c>. Also matches the home written
    /// with forward slashes and with doubled backslashes (JSON).
    /// </summary>
    /// <param name="text">The text to redact.</param>
    /// <param name="home">The home folder, or <see langword="null"/> to leave the text as it is.</param>
    public static string Redact(string text, string? home)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (string.IsNullOrEmpty(home) || text.Length == 0)
        {
            return text;
        }

        home = home.TrimEnd('/', '\\');

        // A home of "C:" would redact every path; leave the text alone.
        if (home.Length < 3)
        {
            return text;
        }

        text = ReplacePrefix(text, home);
        var otherSeparator = home.Contains('\\') ? home.Replace('\\', '/') : home.Replace('/', '\\');
        if (!string.Equals(otherSeparator, home, StringComparison.Ordinal))
        {
            text = ReplacePrefix(text, otherSeparator);
        }

        if (home.Contains('\\'))
        {
            text = ReplacePrefix(text, home.Replace("\\", "\\\\"));
        }

        return text;
    }

    private static string ReplacePrefix(string text, string home)
    {
        var index = text.IndexOf(home, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return text;
        }

        var builder = new StringBuilder(text.Length);
        var start = 0;
        while (index >= 0)
        {
            var end = index + home.Length;
            if (IsBoundary(text, end))
            {
                builder.Append(text, start, index - start).Append('~');
                start = end;
            }

            index = text.IndexOf(home, end, StringComparison.OrdinalIgnoreCase);
        }

        builder.Append(text, start, text.Length - start);
        return builder.ToString();
    }

    private static bool IsBoundary(string text, int index) =>
        index >= text.Length || text[index] is '\\' or '/' or '"' or '\'' || char.IsWhiteSpace(text[index]);
}
