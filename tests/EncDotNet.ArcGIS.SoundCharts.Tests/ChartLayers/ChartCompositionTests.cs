using EncDotNet.SoundCharts.ChartLayers;
using static EncDotNet.SoundCharts.Tests.ChartLayers.ChartLayerSamples;

namespace EncDotNet.SoundCharts.Tests.ChartLayers;

/// <summary>One SoundCharts group per map, its layers split by time behaviour (design brief D11, D12).</summary>
public class ChartCompositionTests
{
    [Fact]
    public void Add_SplitsDatasetsByTimeBehaviour()
    {
        var composition = ChartComposition.Empty.With("mixed", [Cell, Tide, Currents, S101]);

        Assert.Equal([S101.Path, Cell.Path], composition.Layers[ChartLayerKind.Chart].Datasets.Select(d => d.Path));
        Assert.Equal([Tide], composition.Layers[ChartLayerKind.WaterLevel].Datasets);
        Assert.Equal([Currents], composition.Layers[ChartLayerKind.SurfaceCurrents].Datasets);
    }

    [Fact]
    public void Add_LayerExistsOnlyWithDatasets()
    {
        var composition = ChartComposition.Empty.With("US5MA1BO", [Cell]);

        Assert.Equal([ChartLayerKind.Chart], composition.Layers.Keys);
    }

    [Fact]
    public void Group_NamedAfterItsOneInput()
    {
        Assert.Equal("SoundCharts · US5MA1BO", ChartComposition.Empty.With("US5MA1BO", [Cell]).GroupName);
        Assert.Equal("SoundCharts · S101", ChartComposition.Empty.With("S101", [S101, S101 with { Path = @"C:\Charts\S101\b.000" }]).GroupName);
    }

    [Fact]
    public void Group_WithSeveralInputs_CountsDatasets()
    {
        var composition = ChartComposition.Empty.With("US5MA1BO", [Cell]).With("S104", [Tide]);

        Assert.Equal("SoundCharts · 2 datasets", composition.GroupName);
    }

    [Fact]
    public void SecondAdd_JoinsTheSameLayers()
    {
        var first = ChartComposition.Empty.With("US5MA1BO", [Cell]);

        var second = first.With("S101", [S101]);

        Assert.Equal(2, second.Layers[ChartLayerKind.Chart].Datasets.Count);
        Assert.Equal([ChartLayerKind.Chart], second.ChangedSince(first));
    }

    [Fact]
    public void AddingTheSameDatasetAgain_ChangesNothing()
    {
        var first = ChartComposition.Empty.With("US5MA1BO", [Cell]);

        var again = first.With("US5MA1BO", [Cell]);

        Assert.Single(again.Layers[ChartLayerKind.Chart].Datasets);
        Assert.Empty(again.ChangedSince(first));
        Assert.Equal("SoundCharts · US5MA1BO", again.GroupName);
    }

    [Fact]
    public void ChangedSince_NamesOnlyTheLayersWhoseDatasetsChanged()
    {
        var first = ChartComposition.Empty.With("a", [Cell, Tide]);

        var second = first.With("b", [Currents]);

        Assert.Equal([ChartLayerKind.SurfaceCurrents], second.ChangedSince(first));
        Assert.Equal([ChartLayerKind.WaterLevel, ChartLayerKind.Chart], first.ChangedSince(ChartComposition.Empty));
    }

    [Fact]
    public void From_ExistingMarkers_RebuildsTheComposition()
    {
        var original = ChartComposition.Empty.With("a", [Cell, Tide]);

        var read = ChartComposition.From(original.Group, original.Layers.Values);

        Assert.Equal(original.GroupName, read.GroupName);
        Assert.Empty(read.ChangedSince(original));
        Assert.Empty(original.ChangedSince(read));
    }

    [Fact]
    public void From_LayersWithoutAGroup_StillCountsThem()
    {
        var read = ChartComposition.From(null, [ChartLayerMarker.ForLayer(ChartLayerKind.Chart, [Cell])]);

        Assert.Single(read.Layers);
        Assert.Empty(read.Group.Inputs);
    }

    [Fact]
    public void Datasets_AreTopFirst()
    {
        var composition = ChartComposition.Empty.With("a", [Cell, Tide, Currents]);

        Assert.Equal([Currents, Tide, Cell], composition.Datasets);
    }
}
