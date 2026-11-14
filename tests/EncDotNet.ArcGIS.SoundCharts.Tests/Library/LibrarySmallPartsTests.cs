using EncDotNet.S100.Collections;
using EncDotNet.SoundCharts.Library;
using static EncDotNet.SoundCharts.Tests.Library.LibraryTestItems;

namespace EncDotNet.SoundCharts.Tests.Library;

public class LibrarySmallPartsTests
{
    [Fact]
    public void Footprint_FromCoverage_ClosesEachRing()
    {
        var coverage = new GeoCoverage([new GeoPolygon([At(1, 10), At(2, 10), At(2, 11)])]);

        var footprint = LibraryFootprint.Of(Item("A", coverage: coverage));

        Assert.NotNull(footprint);
        Assert.False(footprint.IsBoundingBox);
        var ring = Assert.Single(footprint.Rings);
        Assert.Equal([(10.0, 1.0), (10.0, 2.0), (11.0, 2.0), (10.0, 1.0)], ring);
        Assert.Equal((10.0, 1.0, 11.0, 2.0), (footprint.West, footprint.South, footprint.East, footprint.North));
    }

    [Fact]
    public void Footprint_WithoutCoverage_IsTheBoundingBox()
    {
        var footprint = LibraryFootprint.Of(Item("A", bounds: new GeoBounds(41.25, -70.35, 41.325, -70.275)));

        Assert.NotNull(footprint);
        Assert.True(footprint.IsBoundingBox);
        Assert.Equal(5, Assert.Single(footprint.Rings).Count);
    }

    [Fact]
    public void Footprint_AcrossTheAntimeridian_RunsEastPast180()
    {
        var footprint = LibraryFootprint.Of(Item("A", bounds: new GeoBounds(-20, 170, -10, -170)));

        Assert.NotNull(footprint);
        Assert.Equal(190, footprint.East);
    }

    [Fact]
    public void Footprint_WithNothing_IsNull()
    {
        Assert.Null(LibraryFootprint.Of(Item("A")));
    }

    [Fact]
    public void Paths_LocalFile()
    {
        var item = Item("US5MA1BO", location: new LocalItemLocation(@"C:\charts", "ENC_ROOT/US5MA1BO/US5MA1BO.000", ["ENC_ROOT/US5MA1BO/US5MA1BO.001"]));

        Assert.Equal(@"C:\charts\ENC_ROOT\US5MA1BO\US5MA1BO.000", LibraryItemPaths.DatasetFile(item));
        Assert.Equal(@"C:\charts\ENC_ROOT\US5MA1BO\US5MA1BO.000", LibraryItemPaths.AddToMapPath(item));
        Assert.Equal(@"C:\charts\ENC_ROOT\US5MA1BO\US5MA1BO.000", LibraryItemPaths.FileToReveal(item));
    }

    [Fact]
    public void Paths_InsideAZip_AreTheZip()
    {
        var a = Item("A", location: new LocalItemLocation(@"C:\charts\set.zip", "S-101/A.000", [], "CATALOG.XML", IsZip: true));
        var b = Item("B", location: new LocalItemLocation(@"C:\charts\set.zip", "S-101/B.000", [], "CATALOG.XML", IsZip: true));

        Assert.Null(LibraryItemPaths.DatasetFile(a));
        Assert.Equal(@"C:\charts\set.zip", LibraryItemPaths.FileToReveal(a));
        Assert.Equal([@"C:\charts\set.zip"], LibraryItemPaths.AddToMapPaths([a, b]));
    }

    [Fact]
    public void Paths_Online_AreNone()
    {
        var item = Item("A", location: new RemoteItemLocation(new Uri("https://example.com/A.zip")));

        Assert.Null(LibraryItemPaths.DatasetFile(item));
        Assert.Empty(LibraryItemPaths.AddToMapPaths([item]));
    }

    [Fact]
    public void NewCollectionName_OnePath_IsItsSuggestedName()
    {
        Assert.Equal("Charts", LibraryImportNames.NewCollectionName([(@"C:\data\charts", "Charts")]));
        Assert.Null(LibraryImportNames.NewCollectionName([]));
    }

    [Fact]
    public void NewCollectionName_SeveralPaths_IsTheirFolder()
    {
        Assert.Equal("Region", LibraryImportNames.NewCollectionName([(@"C:\Region\A", "A"), (@"C:\Region\B\", "B")]));
        Assert.Equal("A", LibraryImportNames.NewCollectionName([(@"C:\One\A", "A"), (@"C:\Two\B", "B")]));
    }

    [Fact]
    public void Problem_DescribesWhy()
    {
        Assert.Equal(@"C:\x: not found", new LibraryImportProblem(@"C:\x", LibraryImportProblemKind.NotFound).Describe());
        Assert.Equal(@"C:\x: already in the Library", new LibraryImportProblem(@"C:\x", LibraryImportProblemKind.AlreadyInLibrary).Describe());
        Assert.Equal(@"C:\x: couldn't be read. Bad JSON.", new LibraryImportProblem(@"C:\x", LibraryImportProblemKind.Unreadable, "Bad JSON.").Describe());
        Assert.Equal(@"C:\x: couldn't be read.", new LibraryImportProblem(@"C:\x", LibraryImportProblemKind.Unreadable).Describe());
    }

    [Fact]
    public async Task ValidationStore_KeepsResultsWhileTheFileIsUnchanged()
    {
        using var temp = new TempFolder();
        var file = temp.File("A.000", "one");
        var store = new DatasetValidationStore();
        var changes = new List<string>();
        store.Changed += (_, path) => changes.Add(path);
        var validator = new DatasetValidator(
            "s100.exe",
            (_, _) =>
            {
                Assert.Equal(DatasetValidationState.Running, store.Get(file).State);
                return Task.FromResult(new ValidationProcessResult(0, """{ "valid": true, "rulesEvaluated": 4, "findings": [] }""", string.Empty));
            });

        Assert.Equal(DatasetValidationState.NotValidated, store.Get(file).State);
        var result = await store.ValidateAsync(file, validator, TestContext.Current.CancellationToken);

        Assert.Equal(DatasetValidationState.Valid, result.State);
        Assert.Same(result, store.Get(file));
        Assert.Equal(2, changes.Count);

        File.AppendAllText(file, " and an update");
        Assert.Equal(DatasetValidationState.NotValidated, store.Get(file).State);
    }

    [Fact]
    public async Task ValidationStore_NewUpdateFile_ResetsTheBaseCell()
    {
        using var temp = new TempFolder();
        var cell = temp.File("US5MA1BO.000", "base");
        var store = new DatasetValidationStore();
        store.Set(cell, new DatasetValidation(DatasetValidationState.Valid));
        await Task.Yield();

        temp.File("US5MA1BO.001", "update");

        Assert.Equal(DatasetValidationState.NotValidated, store.Get(cell).State);
    }

    [Fact]
    public void Reader_NotADataset_GivesNothing()
    {
        using var temp = new TempFolder();
        var file = temp.File("notes.txt", "hello");
        using var reader = new LibraryDatasetReader();

        Assert.Null(reader.Probe(file, TestContext.Current.CancellationToken));
        Assert.Empty(reader.ReadTimeSteps(file));
        Assert.Empty(reader.ReadTimeSteps(Path.Combine(temp.Path, "missing.h5")));
    }

    [Fact]
    public void Reader_WaterLevel_HasTimeSteps()
    {
        var dataset = Fixtures.Upstream(Path.Combine("S104", "104US004SC1BO_20251217T12Z.h5"));
        Assert.SkipWhen(dataset is null, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");
        using var reader = new LibraryDatasetReader();

        var steps = reader.ReadTimeSteps(dataset);

        Assert.True(steps.Count > 1);
        Assert.True(steps.SequenceEqual(steps.Order()));
    }
}
