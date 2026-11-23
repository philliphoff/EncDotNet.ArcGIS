using EncDotNet.SoundCharts.Library;
using static EncDotNet.SoundCharts.Tests.Library.LibraryTestItems;

namespace EncDotNet.SoundCharts.Tests.Library;

public class LibraryTreeBuilderTests
{
    [Fact]
    public void Build_ListsCollectionsSourcesAndDatasets()
    {
        var source = Source(@"C:\charts", Item("B"), Item("a", update: 2));
        var collection = Collection("Charts", source);

        var rows = LibraryTreeBuilder.Build([collection]);

        var row = Assert.Single(rows);
        Assert.Equal(LibraryNodeKind.Collection, row.Kind);
        Assert.Equal("Charts", row.Title);
        Assert.Equal("2 datasets", row.Meta);
        Assert.Null(row.ProductTag);
        var sourceRow = Assert.Single(row.Children);
        Assert.Equal(LibraryNodeKind.Source, sourceRow.Kind);
        Assert.Equal("charts", sourceRow.Title);
        Assert.Equal("DIR", sourceRow.ProductTag);
        Assert.Equal("2 datasets", sourceRow.Meta);
        Assert.Equal(["a", "B"], sourceRow.Children.Select(d => d.Title));
        Assert.All(sourceRow.Children, d => Assert.Equal("S-101", d.ProductTag));
        Assert.Equal("Ed 3 Upd 2", sourceRow.Children[0].Meta);
        Assert.Equal("Ed 3", sourceRow.Children[1].Meta);
        Assert.All(sourceRow.Children, d => Assert.NotNull(d.Item));
    }

    [Fact]
    public void Build_Filter_KeepsMatchesAndExpandsThem()
    {
        var match = Source(@"C:\one", Item("US5MA1BO"), Item("US4MA2AA"));
        var none = Source(@"C:\two", Item("FR1234"));
        var other = Collection("Other", Source(@"C:\three", Item("DE5555")));

        var rows = LibraryTreeBuilder.Build([Collection("Charts", match, none), other], filter: "ma1");

        var row = Assert.Single(rows);
        Assert.True(row.IsExpanded);
        var sourceRow = Assert.Single(row.Children);
        Assert.True(sourceRow.IsExpanded);
        Assert.Equal("US5MA1BO", Assert.Single(sourceRow.Children).Title);
    }

    [Fact]
    public void Build_KeepsExpansionAndSelectionById()
    {
        var source = Source(@"C:\charts", Item("A"));
        var collection = Collection("Charts", source);
        var datasetId = LibraryTreeBuilder.Build([collection])[0].Children[0].Children[0].Id;

        var rows = LibraryTreeBuilder.Build(
            [collection],
            expanded: new HashSet<string> { LibraryTreeBuilder.CollectionNodeId(collection.Id) },
            selectedId: datasetId);

        Assert.True(rows[0].IsExpanded);
        Assert.False(rows[0].Children[0].IsExpanded);
        Assert.True(rows[0].Children[0].Children[0].IsSelected);
    }

    [Fact]
    public void Build_TagsMissingAndNotForNavigationDatasets()
    {
        using var temp = new TempFolder();
        var gone = Item("GONE", location: new S100.Collections.LocalItemLocation(temp.Path, "GONE.000", []));
        var flagged = Item(
            "FLAGGED",
            location: new S100.Collections.LocalItemLocation(temp.Path, "FLAGGED.000", []),
            properties: new Dictionary<string, string> { ["notForNavigation"] = "true" });
        temp.File("FLAGGED.000");

        var datasets = LibraryTreeBuilder.Build([Collection("Charts", Source(temp.Path, gone, flagged))])[0].Children[0].Children;

        Assert.Contains(new LibraryNodeTag("Not for navigation"), datasets.Single(d => d.Title == "FLAGGED").Tags);
        var missing = datasets.Single(d => d.Title == "GONE");
        Assert.Equal(new LibraryNodeTag("Missing", IsWarning: true), missing.Tags[0]);
        Assert.True(missing.IsWarning);
    }

    [Fact]
    public void DatasetCount_SaysOneOrMany()
    {
        Assert.Equal("1 dataset", LibraryTreeBuilder.DatasetCount(1));
        Assert.Equal("12 datasets", LibraryTreeBuilder.DatasetCount(12));
    }
}
