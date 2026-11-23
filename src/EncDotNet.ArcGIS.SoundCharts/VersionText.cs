using System.Reflection;

namespace EncDotNet.SoundCharts;

/// <summary>Formats version numbers for display.</summary>
internal static class VersionText
{
    /// <summary>The text shown when a version can't be determined.</summary>
    public const string Unknown = "unknown";

    private const int ShortCommitLength = 7;

    /// <summary>
    /// Gets the display version of <paramref name="assembly"/>: its
    /// informational version, formatted by <see cref="FormatInformational"/>,
    /// or its assembly version when it has none.
    /// </summary>
    public static string FromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            return FormatInformational(informational);
        }

        return assembly.GetName().Version?.ToString() ?? Unknown;
    }

    /// <summary>
    /// Formats an informational version such as <c>0.27.0+001df0e5bb6e…</c>
    /// as <c>0.27.0 (001df0e)</c>. A version without build metadata is
    /// returned unchanged, and metadata that isn't a commit hash is kept whole.
    /// </summary>
    public static string FormatInformational(string informationalVersion)
    {
        ArgumentNullException.ThrowIfNull(informationalVersion);

        var plus = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        if (plus < 0)
        {
            return informationalVersion;
        }

        var version = informationalVersion[..plus];
        var metadata = informationalVersion[(plus + 1)..];
        if (metadata.Length == 0)
        {
            return version;
        }

        if (metadata.Length >= ShortCommitLength && metadata.All(char.IsAsciiHexDigit))
        {
            return $"{version} ({metadata[..ShortCommitLength]})";
        }

        return $"{version} ({metadata})";
    }
}
