using EncDotNet.S100.Collections;
using EncDotNet.S100.Collections.Library;
using EncDotNet.SoundCharts.Library;
using EncDotNet.SoundCharts.Themes;
using static EncDotNet.SoundCharts.Tests.Library.LibraryTestItems;

namespace EncDotNet.SoundCharts.Tests.Library;

public class LibraryDatasetDetailsBuilderTests
{
    [Theory]
    [InlineData("2.0.0", "Supported", SoundChartsTone.Success, null)]
    [InlineData("1.0.0", "Supported", SoundChartsTone.Success, "Read with the newer, compatible edition 1.2.0")]
    [InlineData("2.1.0", "Partly supported", SoundChartsTone.Warning, "SoundCharts reads edition 2.0.0, so some features may not draw")]
    [InlineData("3.0.0", "Not supported", SoundChartsTone.Danger, "SoundCharts reads edition 2.0.0, so the chart may be incomplete or wrong")]
    public void Compatibility_IsAToneChipForTheEdition(string version, string chip, SoundChartsTone tone, string? note)
    {
        var field = LibraryDatasetDetailsBuilder.Compatibility(Item("101AA00DS0003", version: version));

        Assert.NotNull(field);
        Assert.Equal("Compatibility", field.Label);
        Assert.Equal(chip, field.Chip);
        Assert.Equal(tone, field.Tone);
        Assert.Equal(note, field.Value);
        Assert.False(field.IsWorking);
    }

    [Fact]
    public void Compatibility_WithoutAnEdition_IsAPlainNote()
    {
        var field = LibraryDatasetDetailsBuilder.Compatibility(Item("101AA00DS0003", version: null));

        Assert.NotNull(field);
        Assert.Null(field.Chip);
        Assert.Equal(SoundChartsTone.Neutral, field.Tone);
        Assert.Equal("Edition not stated; SoundCharts reads edition 2.0.0", field.Value);
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
        Assert.Contains(product.Fields, f => f.Label == "Compatibility" && f.Chip == "Supported" && f.Tone == SoundChartsTone.Success);
        Assert.Contains(details.Groups, g => g.Fields.Any(f => f.Value?.Contains("Charts", StringComparison.Ordinal) == true));
        var validation = details.Groups[^1];
        Assert.Equal("Validation", validation.Title);
        Assert.False(string.IsNullOrEmpty(validation.Header));
        var status = Assert.Single(validation.Fields);
        Assert.Equal("Not validated", status.Chip);
        Assert.Equal(SoundChartsTone.Neutral, status.Tone);
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
        Assert.Contains(time.Fields, f => f.Label == "Time range" && f.Value?.Contains('→') == true);
        Assert.Contains(time.Fields, f => f.Label == "Time steps" && f.Value == "Reading...");
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

        Assert.Equal("2 errors, 1 warning", fields[0].Chip);
        Assert.Equal(SoundChartsTone.Danger, fields[0].Tone);
        Assert.Equal("S101-R-1.2", fields[1].Label);
        Assert.Equal("Error: Not bound.", fields[1].Value);
        Assert.Null(fields[1].Chip);
    }

    [Theory]
    [InlineData(DatasetValidationState.NotValidated, 0, 0, 0, null, "Not validated", SoundChartsTone.Neutral, null, false)]
    [InlineData(DatasetValidationState.Running, 0, 0, 0, null, "Validating...", SoundChartsTone.Neutral, null, true)]
    [InlineData(DatasetValidationState.Valid, 0, 0, 0, null, "Valid", SoundChartsTone.Success, null, false)]
    [InlineData(DatasetValidationState.Valid, 0, 0, 13, null, "Valid", SoundChartsTone.Success, "13 rules checked", false)]
    [InlineData(DatasetValidationState.Findings, 0, 3, 0, null, "3 warnings", SoundChartsTone.Warning, null, false)]
    [InlineData(DatasetValidationState.Findings, 1, 0, 0, null, "1 error", SoundChartsTone.Danger, null, false)]
    [InlineData(DatasetValidationState.Findings, 0, 0, 0, null, "Not valid", SoundChartsTone.Danger, null, false)]
    [InlineData(DatasetValidationState.NoRules, 0, 0, 0, null, null, SoundChartsTone.Neutral, "No validation rules for this product", false)]
    [InlineData(DatasetValidationState.Failed, 0, 0, 0, "boom", "Couldn't validate", SoundChartsTone.Warning, "boom", false)]
    public void ValidationFields_StatusIsAToneChip(
        DatasetValidationState state,
        int errors,
        int warnings,
        int rules,
        string? message,
        string? chip,
        SoundChartsTone tone,
        string? note,
        bool working)
    {
        var validation = new DatasetValidation(state, errors, warnings, rules, Message: message);

        var status = LibraryDatasetDetailsBuilder.ValidationFields(validation)[0];

        Assert.Equal("Status", status.Label);
        Assert.Equal(chip, status.Chip);
        Assert.Equal(tone, status.Tone);
        Assert.Equal(note, status.Value);
        Assert.Equal(working, status.IsWorking);
    }
}
