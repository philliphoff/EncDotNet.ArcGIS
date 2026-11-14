using EncDotNet.S100.Collections;
using EncDotNet.S100.Collections.Library;
using EncDotNet.S100.DataModel;

namespace EncDotNet.SoundCharts.Tests.Library;

/// <summary>Builds Library snapshots for tests, without indexing anything.</summary>
internal static class LibraryTestItems
{
    public static CollectionItem Item(
        string name,
        string product = "S-101",
        string? version = "2.0.0",
        int? edition = 3,
        int? update = null,
        ItemLocation? location = null,
        GeoBounds? bounds = null,
        GeoCoverage? coverage = null,
        IReadOnlyDictionary<string, string>? properties = null) => new()
        {
            Key = name,
            ProductSpec = product,
            ProductSpecVersion = version,
            Name = name,
            Edition = edition,
            Update = update,
            Bounds = bounds,
            Coverage = coverage,
            Location = location ?? new LocalItemLocation(@"C:\charts", $"{name}.000", []),
            Properties = properties ?? new Dictionary<string, string>(),
        };

    public static LibrarySource Source(string path, params CollectionItem[] items)
    {
        var id = Guid.NewGuid();
        return new LibrarySource(
            new LocalFolderSource(id, null, path),
            new SourceIndex(id, DateTimeOffset.UtcNow, "fingerprint", items, []),
            LibrarySourceState.Ready);
    }

    public static LibraryCollection Collection(string name, params LibrarySource[] sources) =>
        new(new DatasetCollection(Guid.NewGuid(), name, [.. sources.Select(s => s.Definition)], DateTimeOffset.UtcNow), sources);

    public static GeoPosition At(double latitude, double longitude) => new(latitude, longitude);
}
