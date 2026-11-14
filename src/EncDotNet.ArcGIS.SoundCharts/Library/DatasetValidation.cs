namespace EncDotNet.SoundCharts.Library;

/// <summary>
/// A dataset's validation status in the Library: the outcome of
/// <c>s100 validate</c> against its product specification's rules, which
/// runs only when the user asks (issue #14).
/// </summary>
/// <param name="State">Where the validation stands.</param>
/// <param name="Errors">The number of error findings.</param>
/// <param name="Warnings">The number of warning findings.</param>
/// <param name="RulesEvaluated">The number of rules checked.</param>
/// <param name="Findings">The first findings, most severe first.</param>
/// <param name="Message">Why validation couldn't run, for <see cref="DatasetValidationState.Failed"/>.</param>
public sealed record DatasetValidation(
    DatasetValidationState State,
    int Errors = 0,
    int Warnings = 0,
    int RulesEvaluated = 0,
    IReadOnlyList<DatasetValidationFinding>? Findings = null,
    string? Message = null)
{
    /// <summary>Gets the status of a dataset not validated yet.</summary>
    public static DatasetValidation NotValidated { get; } = new(DatasetValidationState.NotValidated);

    /// <summary>Gets the status of a validation that's running.</summary>
    public static DatasetValidation Running { get; } = new(DatasetValidationState.Running);

    /// <summary>Gets the status of a validation that couldn't run.</summary>
    /// <param name="message">Why.</param>
    public static DatasetValidation Failed(string message) => new(DatasetValidationState.Failed, Message: message);
}
