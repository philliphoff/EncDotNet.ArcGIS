using System.Buffers.Text;
using System.Security.Cryptography;

namespace EncDotNet.SoundCharts.Server;

/// <summary>
/// The random token a chart server puts at the start of every URL, so
/// another process on the machine can't guess the URLs (design brief §4.3).
/// </summary>
public static class ServerToken
{
    /// <summary>The number of random bytes in a token: 128 bits.</summary>
    public const int RandomBytes = 16;

    /// <summary>
    /// Creates a new token: 128 random bits in base64url (22 characters),
    /// then <c>-{suffix}</c> when <paramref name="suffix"/> is given. Slice A5
    /// passes the render key (D27) as the suffix, so the URL shows which
    /// settings drew a tile.
    /// </summary>
    /// <param name="suffix">Text to append, or <see langword="null"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="suffix"/> has characters a token can't hold.</exception>
    public static string Create(string? suffix = null)
    {
        var random = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(RandomBytes));
        if (string.IsNullOrEmpty(suffix))
        {
            return random;
        }

        var token = $"{random}-{suffix}";
        return IsValid(token)
            ? token
            : throw new ArgumentException($"'{suffix}' can't be part of a server token: use letters, digits, '-' and '_'.", nameof(suffix));
    }

    /// <summary>
    /// Gets whether <paramref name="token"/> is one <c>s100 tiles serve</c>
    /// accepts: not empty, and only ASCII letters, digits, <c>-</c> and <c>_</c>.
    /// </summary>
    /// <param name="token">The token to check.</param>
    public static bool IsValid(string? token) =>
        !string.IsNullOrEmpty(token) && token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
