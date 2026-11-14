namespace EncDotNet.SoundCharts.Library;

/// <summary>One finding of <c>s100 validate</c>.</summary>
/// <param name="RuleId">The rule, such as <c>S101-R-1.2</c>.</param>
/// <param name="Severity">The severity as <c>s100</c> writes it: <c>Error</c>, <c>Warning</c> or <c>Info</c>.</param>
/// <param name="Message">What the rule found.</param>
public sealed record DatasetValidationFinding(string RuleId, string Severity, string Message);
