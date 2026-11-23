using System.Security.Cryptography;
using System.Text;

namespace EncDotNet.SoundCharts.Charts;

/// <summary>
/// The render key (design brief §5.4, D27): 16 lower-case hexadecimal
/// characters that change whenever anything that changes a chart's tiles
/// does, so a new key gives new tile URLs and Pro's tile cache can't show
/// old tiles.
/// </summary>
/// <remarks>
/// <para>
/// The key is the first 8 bytes of the SHA-256 of canonical text: a schema
/// line, the server-side settings
/// (<see cref="ChartRenderOptions.ToCanonicalString"/>), the <c>s100</c>
/// version, and the <see cref="DataRevision"/> of the datasets. The same
/// settings, server and files give the same key on any machine. The palette
/// isn't in it: it's already the WMTS style.
/// </para>
/// <para>
/// Until the server takes the key as a style suffix (U1, see
/// <see cref="Settings.ChartServerSupport.SettingsStyles"/>), every change
/// to the start-up options is a server restart, and the key is the suffix
/// of the new server's URL token, so a URL shows which settings drew it.
/// </para>
/// </remarks>
public static class RenderKey
{
    /// <summary>The length of a key in characters.</summary>
    public const int Length = 16;

    private const string Schema = "soundcharts-render-key/1";

    /// <summary>Computes the render key.</summary>
    /// <param name="options">The start-up options that change the tiles.</param>
    /// <param name="serverVersion">The <c>s100</c> version, or <see langword="null"/> when unknown.</param>
    /// <param name="dataRevision">The datasets' <see cref="DataRevision.Of"/> text.</param>
    public static string Compute(ChartRenderOptions options, string? serverVersion, string dataRevision)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(dataRevision);

        var text = new StringBuilder(Schema).Append('\n')
            .Append(options.ToCanonicalString()).Append('\n')
            .Append("s100=").Append(serverVersion?.Trim() ?? string.Empty).Append('\n')
            .Append(dataRevision)
            .ToString();
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)).AsSpan(0, Length / 2));
    }

    /// <summary>Computes the render key of <paramref name="datasets"/> as they are on disk now.</summary>
    /// <param name="datasets">The chart's datasets and start-up options.</param>
    /// <param name="serverVersion">The <c>s100</c> version, or <see langword="null"/> when unknown.</param>
    public static string Compute(DatasetSet datasets, string? serverVersion)
    {
        ArgumentNullException.ThrowIfNull(datasets);
        return Compute(datasets.Options, serverVersion, DataRevision.Of(datasets.Sources));
    }

    /// <summary>Gets whether <paramref name="key"/> looks like a render key: 16 lower-case hexadecimal characters.</summary>
    /// <param name="key">The text to check.</param>
    public static bool IsValid(string? key) =>
        key is { Length: Length } && key.All(c => char.IsAsciiDigit(c) || c is >= 'a' and <= 'f');
}
