using System.Security.Cryptography;
using System.Text;

namespace EncDotNet.SoundCharts.Charts;

/// <summary>
/// The datasets one chart server portrays, with the start-up options that
/// change its tiles: the key of the chart server pool (design brief §4.1,
/// D17).
/// </summary>
/// <remarks>
/// <para>
/// A set is immutable. Its sources are kept in a canonical order (by path,
/// ignoring case) without duplicates, so two layers that name the same
/// datasets in a different order share one server. Adding or removing a
/// dataset gives a new set, and so a new server (D20).
/// </para>
/// <para>
/// <see cref="Key"/> is the canonical text the pool compares;
/// <see cref="Id"/> is a short hash of it that names the chart in the log
/// and the server's working folder.
/// </para>
/// </remarks>
public sealed class DatasetSet : IEquatable<DatasetSet>
{
    private const string KeyVersion = "soundcharts-dataset-set/1";

    /// <summary>Creates a set of <paramref name="sources"/>.</summary>
    /// <param name="sources">The datasets, exchange sets and folders to portray; at least one.</param>
    /// <param name="options">The start-up options, or <see langword="null"/> for <see cref="ChartRenderOptions.Default"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="sources"/> is empty or holds <see langword="null"/>.</exception>
    public DatasetSet(IEnumerable<DatasetSource> sources, ChartRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(sources);

        var list = sources.ToList();
        if (list.Any(source => source is null))
        {
            throw new ArgumentException("A dataset set can't hold a null source.", nameof(sources));
        }

        Sources = [.. list
            .Distinct()
            .OrderBy(source => source.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(source => source.Kind)];
        if (Sources.Count == 0)
        {
            throw new ArgumentException("A dataset set needs at least one dataset.", nameof(sources));
        }

        Options = options ?? ChartRenderOptions.Default;
        Key = BuildKey(Sources, Options);
        Id = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Key)).AsSpan(0, 4));
    }

    /// <summary>Gets the sources in canonical order.</summary>
    public IReadOnlyList<DatasetSource> Sources { get; }

    /// <summary>Gets the start-up options that change the tiles.</summary>
    public ChartRenderOptions Options { get; }

    /// <summary>Gets the canonical text that identifies the set.</summary>
    public string Key { get; }

    /// <summary>
    /// Gets a short, stable identifier of the set: the first 8 hexadecimal
    /// characters of the SHA-256 of <see cref="Key"/>, such as <c>3f9a1c02</c>.
    /// </summary>
    public string Id { get; }

    /// <summary>Gets a set with <paramref name="source"/> added, or this set if it's already in.</summary>
    /// <param name="source">The source to add.</param>
    public DatasetSet With(DatasetSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Sources.Contains(source) ? this : new DatasetSet([.. Sources, source], Options);
    }

    /// <summary>
    /// Gets a set without <paramref name="source"/>, or <see langword="null"/>
    /// when it was the last one.
    /// </summary>
    /// <param name="source">The source to remove.</param>
    public DatasetSet? Without(DatasetSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!Sources.Contains(source))
        {
            return this;
        }

        var rest = Sources.Where(s => !s.Equals(source)).ToList();
        return rest.Count == 0 ? null : new DatasetSet(rest, Options);
    }

    /// <summary>Gets a set of the same sources with other start-up options.</summary>
    /// <param name="options">The new options.</param>
    public DatasetSet WithOptions(ChartRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options == Options ? this : new DatasetSet(Sources, options);
    }

    /// <inheritdoc/>
    public bool Equals(DatasetSet? other) => other is not null && string.Equals(Key, other.Key, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as DatasetSet);

    /// <inheritdoc/>
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Key);

    /// <inheritdoc/>
    public override string ToString() => Sources.Count == 1
        ? $"{Id} ({Sources[0].Name})"
        : $"{Id} ({Sources.Count} sources)";

    private static string BuildKey(IReadOnlyList<DatasetSource> sources, ChartRenderOptions options)
    {
        var builder = new StringBuilder(KeyVersion).Append('\n').Append(options.ToCanonicalString()).Append('\n');
        foreach (var source in sources)
        {
            builder.Append(source.Kind).Append('|').Append(source.Path.ToUpperInvariant()).Append('\n');
        }

        return builder.ToString();
    }
}
