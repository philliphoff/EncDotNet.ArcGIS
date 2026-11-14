namespace EncDotNet.SoundCharts;

/// <summary>Expands Windows 8.3 short names (<c>PHOFF~1.RED</c>) in paths.</summary>
/// <remarks>
/// The chart server must not run from a short-path folder: in step 0,
/// <c>s100 tiles serve</c> crashed in its file watcher while its working
/// folder was an 8.3 path (design brief §1.2, D19).
/// </remarks>
public static class LongPath
{
    /// <summary>
    /// Gets the long form of <paramref name="path"/>, which must exist, or
    /// the full path unchanged when Windows can't expand it.
    /// </summary>
    /// <param name="path">An existing file or folder.</param>
    public static string Expand(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var full = Path.GetFullPath(path);
        if (!OperatingSystem.IsWindows() || !full.Contains('~', StringComparison.Ordinal))
        {
            return full;
        }

        var buffer = new char[1024];
        var length = NativeMethods.GetLongPathName(full, buffer, (uint)buffer.Length);
        if (length > buffer.Length)
        {
            buffer = new char[length];
            length = NativeMethods.GetLongPathName(full, buffer, (uint)buffer.Length);
        }

        return length == 0 || length > buffer.Length ? full : new string(buffer, 0, (int)length);
    }

    /// <summary>Gets whether <paramref name="path"/> has a segment that looks like an 8.3 short name.</summary>
    /// <param name="path">The path to check.</param>
    public static bool LooksShort(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return path.Split('\\', '/').Any(segment =>
        {
            var tilde = segment.IndexOf('~', StringComparison.Ordinal);
            return tilde > 0 && tilde + 1 < segment.Length && char.IsAsciiDigit(segment[tilde + 1]);
        });
    }
}
