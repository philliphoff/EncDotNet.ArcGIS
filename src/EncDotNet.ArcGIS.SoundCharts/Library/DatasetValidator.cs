using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using EncDotNet.SoundCharts.Resources;
using EncDotNet.SoundCharts.Server;

namespace EncDotNet.SoundCharts.Library;

/// <summary>
/// Validates one dataset on demand by running
/// <c>s100 validate &lt;dataset&gt; --format json</c>, the bundled chart
/// server's program, out of Pro's process (issue #14).
/// </summary>
/// <remarks>
/// Validation reads the whole dataset and runs its product's rule pack, so
/// it runs out of process like the chart server: a crash or a large dataset
/// can't take Pro down. Exit code 0 is valid, 6 has findings; both print the
/// JSON report. Exchange-set integrity (signatures, Part 15) isn't checked:
/// permits are deferred.
/// </remarks>
public sealed class DatasetValidator
{
    /// <summary>The exit code of <c>s100 validate</c> when it has findings.</summary>
    public const int FindingsExitCode = 6;

    /// <summary>The most findings kept for the details pane.</summary>
    public const int MaxFindings = 20;

    private readonly string _executable;
    private readonly Func<ProcessStartInfo, CancellationToken, Task<ValidationProcessResult>> _run;
    private readonly TimeSpan _timeout;

    /// <summary>Creates a validator that runs <paramref name="executable"/>.</summary>
    /// <param name="executable">The full path of <c>s100.exe</c>.</param>
    /// <param name="run">Runs the process; <see langword="null"/> for a real process. Tests pass a fake.</param>
    /// <param name="timeout">How long one validation may take; five minutes by default.</param>
    public DatasetValidator(
        string executable,
        Func<ProcessStartInfo, CancellationToken, Task<ValidationProcessResult>>? run = null,
        TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        _executable = executable;
        _run = run ?? RunProcessAsync;
        _timeout = timeout ?? TimeSpan.FromMinutes(5);
    }

    /// <summary>Validates the dataset at <paramref name="datasetPath"/>. Never throws but for cancellation.</summary>
    /// <param name="datasetPath">The dataset file; S-57 and S-101 updates beside a base cell are applied.</param>
    /// <param name="cancellationToken">Stops the validation and its process.</param>
    /// <returns>The outcome; <see cref="DatasetValidationState.Failed"/> when it couldn't run.</returns>
    public async Task<DatasetValidation> ValidateAsync(string datasetPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetPath);

        var start = new ProcessStartInfo(_executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(datasetPath) ?? string.Empty,
        };
        start.ArgumentList.Add("validate");
        start.ArgumentList.Add(datasetPath);
        start.ArgumentList.Add("--format");
        start.ArgumentList.Add("json");
        start.Environment["S100_NO_UPDATE_CHECK"] = "1";

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);
        ValidationProcessResult result;
        try
        {
            result = await _run(start, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DatasetValidation.Failed(string.Format(CultureInfo.CurrentCulture, Strings.Library_ValidationTimedOut, _timeout.TotalMinutes));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return DatasetValidation.Failed(ex.Message);
        }

        if (result.ExitCode is 0 or FindingsExitCode && Parse(result.StandardOutput) is { } validation)
        {
            return validation;
        }

        var reason = FirstLine(result.StandardError) ?? FirstLine(result.StandardOutput)
            ?? string.Format(CultureInfo.CurrentCulture, Strings.Library_ValidationExitCode, result.ExitCode);
        return DatasetValidation.Failed(reason);
    }

    /// <summary>
    /// Reads the JSON report of <c>s100 validate --format json</c> for one
    /// dataset (<c>valid</c>, <c>rulesAvailable</c>, <c>rulesEvaluated</c>,
    /// <c>findings</c>).
    /// </summary>
    /// <param name="json">The report.</param>
    /// <returns>The outcome, or <see langword="null"/> when it isn't a dataset report.</returns>
    public static DatasetValidation? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("valid", out var valid))
            {
                return null;
            }

            var findings = new List<DatasetValidationFinding>();
            if (root.TryGetProperty("findings", out var list) && list.ValueKind == JsonValueKind.Array)
            {
                foreach (var finding in list.EnumerateArray())
                {
                    findings.Add(new DatasetValidationFinding(
                        Text(finding, "ruleId") ?? string.Empty,
                        Text(finding, "severity") ?? string.Empty,
                        Text(finding, "message") ?? string.Empty));
                }
            }

            var errors = findings.Count(f => f.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase));
            var warnings = findings.Count(f => f.Severity.Equals("Warning", StringComparison.OrdinalIgnoreCase));
            var rulesAvailable = !root.TryGetProperty("rulesAvailable", out var available) || available.ValueKind != JsonValueKind.False;
            var rules = root.TryGetProperty("rulesEvaluated", out var evaluated) && evaluated.TryGetInt32(out var count) ? count : 0;
            var state = (rulesAvailable, errors + warnings, valid.ValueKind) switch
            {
                (false, 0, _) => DatasetValidationState.NoRules,
                (_, > 0, _) => DatasetValidationState.Findings,
                (_, _, JsonValueKind.False) => DatasetValidationState.Findings,
                _ => DatasetValidationState.Valid,
            };

            var shown = findings
                .OrderBy(f => SeverityRank(f.Severity))
                .Take(MaxFindings)
                .ToArray();
            return new DatasetValidation(state, errors, warnings, rules, shown);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static int SeverityRank(string severity) => severity.ToUpperInvariant() switch
    {
        "ERROR" => 0,
        "WARNING" => 1,
        _ => 2,
    };

    private static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string? FirstLine(string? text) =>
        text?.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();

    private static async Task<ValidationProcessResult> RunProcessAsync(ProcessStartInfo start, CancellationToken cancellationToken)
    {
        using var process = Process.Start(start)
            ?? throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Strings.Library_ValidationNotStarted, start.FileName));

        // Ends with Pro if Pro exits first.
        KillOnCloseJob.TryAssign(process);
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // Already gone.
            }

            throw;
        }

        return new ValidationProcessResult(process.ExitCode, await output.ConfigureAwait(false), await error.ConfigureAwait(false));
    }
}
