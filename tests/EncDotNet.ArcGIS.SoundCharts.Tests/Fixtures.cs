namespace EncDotNet.SoundCharts.Tests;

/// <summary>Finds test data: the committed fixtures, and optional upstream datasets.</summary>
internal static class Fixtures
{
    /// <summary>
    /// The environment variable naming EncDotNet.S100's <c>tests/datasets</c>
    /// folder. Tests that need real S-102, S-104 or S-111 data skip without it.
    /// CI sets it to a sparse checkout of the EncDotNet.S100 release the add-in
    /// pins.
    /// </summary>
    public const string UpstreamDatasetsVariable = "SOUNDCHARTS_S100_TEST_DATASETS";

    /// <summary>Gets the full path of a committed fixture.</summary>
    public static string PathOf(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    /// <summary>
    /// Gets the full path of a file under EncDotNet.S100's <c>tests/datasets</c>
    /// folder, or <c>null</c> when <see cref="UpstreamDatasetsVariable"/> isn't
    /// set or the file isn't there.
    /// </summary>
    public static string? Upstream(string relativePath)
    {
        var root = Environment.GetEnvironmentVariable(UpstreamDatasetsVariable);
        if (string.IsNullOrWhiteSpace(root))
        {
            return null;
        }

        var path = Path.Combine(root, relativePath);
        return File.Exists(path) ? path : null;
    }

    /// <summary>
    /// Copies Hurricane Ian's advisory 20 (EncDotNet.S100's
    /// <c>tests/datasets/TC/al092022</c>: the forecast, wind radii and best
    /// track zips) into <paramref name="folder"/>, creating it, or returns
    /// <see langword="false"/> when <see cref="UpstreamDatasetsVariable"/>
    /// isn't set.
    /// </summary>
    /// <param name="folder">The folder to copy into.</param>
    /// <param name="atcfId">
    /// The storm id to name the copies by, such as <c>ep012022</c>, for a
    /// second storm; <see langword="null"/> keeps <c>al092022</c>.
    /// </param>
    /// <param name="advisory">The advisory number to name the forecast and wind radii zips by; <see langword="null"/> keeps <c>020</c>.</param>
    public static bool CopyUpstreamStorm(string folder, string? atcfId = null, string? advisory = null)
    {
        if (Upstream(Path.Combine("TC", "al092022", "al092022_5day_020.zip")) is not { } forecast)
        {
            return false;
        }

        Directory.CreateDirectory(folder);
        foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(forecast)!, "*.zip"))
        {
            var name = Path.GetFileName(file).Replace("al092022", atcfId ?? "al092022", StringComparison.Ordinal);
            if (advisory is not null)
            {
                name = name.Replace("_020.zip", $"_{advisory}.zip", StringComparison.Ordinal);
            }

            File.Copy(file, Path.Combine(folder, name), overwrite: true);
        }

        return true;
    }

    /// <summary>
    /// Writes a GRIB2 model run published one file per forecast hour into
    /// <paramref name="folder"/>, creating it: EncDotNet.S100's
    /// <c>tests/datasets/GRIB2/gfswave-useast.grib2</c> (GFS-Wave, 2026-10-25
    /// 00Z, hours 0, 3 and 6) split by forecast hour into
    /// <c>gfswave.t00z.global.0p25.f000.grib2</c>, <c>…f003…</c> and
    /// <c>…f006…</c>, as NOMADS and AWS name them. Returns the files, or an
    /// empty list when <see cref="UpstreamDatasetsVariable"/> isn't set.
    /// </summary>
    /// <param name="folder">The folder to write into.</param>
    /// <param name="hours">The forecast hours to write; <see langword="null"/> for all three.</param>
    public static IReadOnlyList<string> WriteUpstreamRun(string folder, IReadOnlyCollection<int>? hours = null)
    {
        if (Upstream(Path.Combine("GRIB2", "gfswave-useast.grib2")) is not { } source)
        {
            return [];
        }

        Directory.CreateDirectory(folder);
        var files = new List<string>();
        foreach (var (hour, messages) in GribMessagesByForecastHour(File.ReadAllBytes(source)))
        {
            if (hours is not null && !hours.Contains(hour))
            {
                continue;
            }

            var path = Path.Combine(folder, $"gfswave.t00z.global.0p25.f{hour:000}.grib2");
            File.WriteAllBytes(path, messages);
            files.Add(path);
        }

        return files;
    }

    // Splits a GRIB2 file into its messages and groups them by the forecast
    // time of their product definition (template 4.0, octets 19-22).
    private static SortedDictionary<int, byte[]> GribMessagesByForecastHour(byte[] bytes)
    {
        var groups = new SortedDictionary<int, List<byte>>();
        var offset = 0;
        while (offset + 16 <= bytes.Length)
        {
            var length = checked((int)System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(bytes.AsSpan(offset + 8, 8)));
            var message = bytes.AsSpan(offset, length);
            var hour = -1;
            for (var section = 16; section < length - 4;)
            {
                var sectionLength = (int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(message.Slice(section, 4));
                if (message[section + 4] == 4)
                {
                    hour = (int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(message.Slice(section + 18, 4));
                }

                section += sectionLength;
            }

            if (!groups.TryGetValue(hour, out var group))
            {
                groups[hour] = group = [];
            }

            group.AddRange(message.ToArray());
            offset += length;
        }

        return new SortedDictionary<int, byte[]>(groups.ToDictionary(g => g.Key, g => g.Value.ToArray()));
    }
}
