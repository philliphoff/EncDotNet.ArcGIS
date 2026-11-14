using System.Diagnostics;
using EncDotNet.SoundCharts.Library;
using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts.Tests.Library;

public class DatasetValidatorTests
{
    private const string Valid = """
        { "specification": "S-57", "edition": "0.0.0", "rulesAvailable": true, "rulesEvaluated": 13,
          "rulesWithFindings": 0, "valid": true, "suppressedCount": 0, "findings": [] }
        """;

    private const string WithFindings = """
        { "specification": "S-101", "rulesAvailable": true, "rulesEvaluated": 10, "valid": false, "findings": [
          { "ruleId": "S101-R-3.1", "severity": "Warning", "message": "Odd." },
          { "ruleId": "S101-R-1.2", "severity": "Error", "message": "Not bound." },
          { "ruleId": "S101-R-1.2", "severity": "Error", "message": "Not bound again." } ] }
        """;

    [Fact]
    public void Parse_Valid()
    {
        var result = DatasetValidator.Parse(Valid);

        Assert.NotNull(result);
        Assert.Equal(DatasetValidationState.Valid, result.State);
        Assert.Equal(13, result.RulesEvaluated);
        Assert.Empty(result.Findings!);
    }

    [Fact]
    public void Parse_Findings_CountsAndOrdersBySeverity()
    {
        var result = DatasetValidator.Parse(WithFindings);

        Assert.NotNull(result);
        Assert.Equal(DatasetValidationState.Findings, result.State);
        Assert.Equal(2, result.Errors);
        Assert.Equal(1, result.Warnings);
        Assert.Equal(["Error", "Error", "Warning"], result.Findings!.Select(f => f.Severity));
    }

    [Fact]
    public void Parse_NoRules()
    {
        var result = DatasetValidator.Parse("""{ "rulesAvailable": false, "rulesEvaluated": 0, "valid": true, "findings": [] }""");

        Assert.Equal(DatasetValidationState.NoRules, result?.State);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("""{ "kind": "exchange-set" }""")]
    [InlineData("[]")]
    public void Parse_NotADatasetReport_IsNull(string? json)
    {
        Assert.Null(DatasetValidator.Parse(json));
    }

    [Fact]
    public async Task Validate_RunsS100ValidateAsJson()
    {
        ProcessStartInfo? started = null;
        var validator = new DatasetValidator(@"C:\s100\s100.exe", (start, _) =>
        {
            started = start;
            return Task.FromResult(new ValidationProcessResult(DatasetValidator.FindingsExitCode, WithFindings, string.Empty));
        });

        var result = await validator.ValidateAsync(@"C:\charts\101AA00DS0003.000", TestContext.Current.CancellationToken);

        Assert.Equal(DatasetValidationState.Findings, result.State);
        Assert.NotNull(started);
        Assert.Equal(@"C:\s100\s100.exe", started.FileName);
        Assert.Equal(["validate", @"C:\charts\101AA00DS0003.000", "--format", "json"], started.ArgumentList);
        Assert.Equal("1", started.Environment["S100_NO_UPDATE_CHECK"]);
    }

    [Fact]
    public async Task Validate_FailedProcess_SaysWhy()
    {
        var validator = new DatasetValidator(
            "s100.exe",
            (_, _) => Task.FromResult(new ValidationProcessResult(127, string.Empty, "\nError: A dataset or exchange set path is required.\n")));

        var result = await validator.ValidateAsync("x.000", TestContext.Current.CancellationToken);

        Assert.Equal(DatasetValidationState.Failed, result.State);
        Assert.Equal("Error: A dataset or exchange set path is required.", result.Message);
    }

    [Fact]
    public async Task Validate_ProcessThatWontStart_Fails()
    {
        var validator = new DatasetValidator("s100.exe", (_, _) => throw new System.ComponentModel.Win32Exception("not found"));

        var result = await validator.ValidateAsync("x.000", TestContext.Current.CancellationToken);

        Assert.Equal(DatasetValidationState.Failed, result.State);
        Assert.Equal("not found", result.Message);
    }

    [Fact]
    public async Task Validate_TooSlow_TimesOut()
    {
        var validator = new DatasetValidator(
            "s100.exe",
            async (_, token) =>
            {
                await Task.Delay(Timeout.Infinite, token);
                return new ValidationProcessResult(0, Valid, string.Empty);
            },
            TimeSpan.FromMilliseconds(50));

        var result = await validator.ValidateAsync("x.000", TestContext.Current.CancellationToken);

        Assert.Equal(DatasetValidationState.Failed, result.State);
        Assert.Contains("longer than", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Validate_WithTheBundledS100()
    {
        var executable = ServerExecutable.Locate(AppContext.BaseDirectory, System.Runtime.InteropServices.RuntimeInformation.OSArchitecture, overridePath: null);
        var cell = Fixtures.Upstream(Path.Combine("S57", "US5MA1BO", "US5MA1BO.000"));
        Assert.SkipWhen(executable is null, "Run tools/build-server.ps1 to build the bundled s100.");
        Assert.SkipWhen(cell is null, $"Set {Fixtures.UpstreamDatasetsVariable} to EncDotNet.S100's tests/datasets folder.");

        var result = await new DatasetValidator(executable.Path).ValidateAsync(cell, TestContext.Current.CancellationToken);

        Assert.Equal(DatasetValidationState.Valid, result.State);
        Assert.True(result.RulesEvaluated > 0);
    }
}
