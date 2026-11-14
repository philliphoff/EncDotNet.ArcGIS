using EncDotNet.S100.Collections;
using EncDotNet.S100.Collections.Library;
using EncDotNet.SoundCharts.Library;
using static EncDotNet.SoundCharts.Tests.Library.LibraryTestItems;

namespace EncDotNet.SoundCharts.Tests.Library;

public class LibraryDatasetDetailsBuilderTests
{
    [Theory]
    [InlineData("2.0.0", "Supported")]
    [InlineData("1.0.0", "Supported: read with the newer, compatible edition 1.2.0")]
    [InlineData("2.1.0", "Partly supported: SoundCharts reads edition 2.0.0, so some features may not draw")]
    [InlineData("3.0.0", "Not supported: SoundCharts reads edition 2.0.0, so the chart may be incomplete or wrong")]
    [InlineData(null, "Edition not stated; SoundCharts reads edition 2.0.0")]
    public void Compatibility_ComparesTheEditionWithWhatSoundChartsReads(string? version, string expected)
    {
        var field = LibraryDatasetDetailsBuilder.Compatibility(Item("101AA00DS0003", version: version));

        Assert.NotNull(field);
        Assert.Equal("Compatibility", field.Label);
        Assert.Equal(expected, field.Value);
    }

    [Fact]
    public void Compatibility_NotAssessedForS57()
    {
        Assert.Null(LibraryDatasetDetailsBuilder.Compatibility(Item("US5MA1BO", product: "S-57", version: "3.1")));
    }

    [Fact]
    public void Build_HasProductCoverageSourceAndValidation()
    {
        var item = Item("101AA00DS0003", edition: 3, update: 1, bounds: new GeoBounds(-33, 61, -32, 62));
        var state = new LibraryItemState(item, Source(@"C:\charts", item));

        var details = LibraryDatasetDetailsBuilder.Build(state, "Charts", DatasetValidation.NotValidated);

        Assert.Equal("101AA00DS0003", details.Name);
        Assert.StartsWith("S-101", details.Product, StringComparison.Ordinal);
        var product = details.Groups[0];
        Assert.Contains(product.Fields, f => f.Label == "Compatibility" && f.Value == "Supported");
        Assert.Contains(details.Groups, g => g.Fields.Any(f => f.Value.Contains("Charts", StringComparison.Ordinal)));
        var validation = details.Groups[^1];
        Assert.Equal("Validation", validation.Title);
        Assert.Equal("Not validated", Assert.Single(validation.Fields).Value);
    }

    [Fact]
    public void Build_TimeVaryingDataset_ShowsItsRangeAndSteps()
    {
        var item = Item("104US004SC1BO", product: "S-104", properties: new Dictionary<string, string>
        {
            ["timeStart"] = "2025-12-17T13:00:00Z",
            ["timeEnd"] = "2025-12-17T15:00:00Z",
        });
        var state = new LibraryItemState(item, Source(@"C:\charts", item));
        DateTime[] steps =
        [
            new(2025, 12, 17, 13, 0, 0, DateTimeKind.Utc),
            new(2025, 12, 17, 14, 0, 0, DateTimeKind.Utc),
            new(2025, 12, 17, 15, 0, 0, DateTimeKind.Utc),
        ];

        var reading = LibraryDatasetDetailsBuilder.Build(state, null, DatasetValidation.NotValidated, timeSteps: null);
        var read = LibraryDatasetDetailsBuilder.Build(state, null, DatasetValidation.NotValidated, steps);

        var time = reading.Groups[1];
        Assert.Equal("Time", time.Title);
        Assert.Contains(time.Fields, f => f.Label == "Time range" && f.Value.Contains('→'));
        Assert.Contains(time.Fields, f => f.Label == "Time steps" && f.Value == "Reading…");
        Assert.Contains(read.Groups[1].Fields, f => f.Label == "Time steps" && f.Value == "3 steps · hourly");
    }

    [Fact]
    public void Build_StaticDataset_HasNoTimeGroup()
    {
        var item = Item("101AA00DS0003");
        var details = LibraryDatasetDetailsBuilder.Build(new LibraryItemState(item, Source(@"C:\charts", item)), null, DatasetValidation.NotValidated);

        Assert.DoesNotContain(details.Groups, g => g.Title == "Time");
    }

    [Theory]
    [InlineData(0, "None")]
    [InlineData(1, "1 step")]
    public void TimeStepsText_FewSteps(int count, string expected)
    {
        var steps = Enumerable.Range(0, count).Select(i => new DateTime(2026, 1, 1, i, 0, 0, DateTimeKind.Utc)).ToArray();

        Assert.Equal(expected, LibraryDatasetDetailsBuilder.TimeStepsText(steps));
    }

    [Fact]
    public void TimeStepsText_NamesTheCommonInterval()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime[] steps = [start, start.AddMinutes(10), start.AddMinutes(20), start.AddMinutes(40)];

        Assert.Equal("4 steps · every 10 min", LibraryDatasetDetailsBuilder.TimeStepsText(steps));
    }

    [Fact]
    public void ValidationFields_SayTheOutcome()
    {
        var findings = new DatasetValidation(
            DatasetValidationState.Findings,
            Errors: 2,
            Warnings: 1,
            Findings: [new DatasetValidationFinding("S101-R-1.2", "Error", "Not bound.")]);

        var fields = LibraryDatasetDetailsBuilder.ValidationFields(findings);

        Assert.Equal("2 errors, 1 warning", fields[0].Value);
        Assert.Equal("S101-R-1.2", fields[1].Label);
        Assert.Equal("Error: Not bound.", fields[1].Value);
        Assert.Equal("Valid · 13 rules checked", LibraryDatasetDetailsBuilder.ValidationFields(new DatasetValidation(DatasetValidationState.Valid, RulesEvaluated: 13))[0].Value);
        Assert.Equal("Couldn't validate: boom", LibraryDatasetDetailsBuilder.ValidationFields(DatasetValidation.Failed("boom"))[0].Value);
        Assert.Equal("Validating…", LibraryDatasetDetailsBuilder.ValidationFields(DatasetValidation.Running)[0].Value);
    }
}
