using System.Globalization;
using EncDotNet.S100.Collections;
using EncDotNet.S100.Collections.Library;
using EncDotNet.S100.Core;
using EncDotNet.S100.Datasets.Pipelines;
using EncDotNet.SoundCharts.Resources;
using EncDotNet.SoundCharts.Themes;

namespace EncDotNet.SoundCharts.Library;

/// <summary>
/// Builds <see cref="LibraryDatasetDetails"/>: EncDotNet.S100's own detail
/// groups (<see cref="LibraryItemText.Details"/>, the viewer's words), with
/// the add-in's compatibility chip, the time a dataset covers and its time
/// steps, and the validation status chip added.
/// </summary>
public static class LibraryDatasetDetailsBuilder
{
    /// <summary>The products whose datasets change with time, which have time steps.</summary>
    private static readonly HashSet<string> TimeVaryingProducts = new(StringComparer.Ordinal) { "S-104", "S-111" };

    /// <summary>Gets whether datasets of <paramref name="product"/> have time steps to read.</summary>
    /// <param name="product">The product, such as <c>S-104</c>.</param>
    public static bool HasTimeSteps(string product) => TimeVaryingProducts.Contains(product);

    /// <summary>Builds the details.</summary>
    /// <param name="state">The dataset.</param>
    /// <param name="collectionName">The collection it's in.</param>
    /// <param name="validation">Its validation status.</param>
    /// <param name="timeSteps">Its time steps when read, empty when it has none, <see langword="null"/> while reading or not read.</param>
    /// <param name="timeZone">The zone for times; UTC when <see langword="null"/>.</param>
    public static LibraryDatasetDetails Build(
        LibraryItemState state,
        string? collectionName,
        DatasetValidation validation,
        IReadOnlyList<DateTime>? timeSteps = null,
        TimeZoneInfo? timeZone = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(validation);
        var item = state.Item;
        var input = new LibraryItemTextInput(item, state.Source, state.Availability, state.EffectiveItem)
        {
            CollectionName = collectionName,
            TimeFormat = timeZone is null ? LibraryTimeFormat.Utc : LibraryTimeFormat.Local,
            TimeZone = timeZone ?? TimeZoneInfo.Utc,
        };

        // LibraryItemText.Details names its groups in EncDotNet.S100's words; the Product group has the Spec field.
        var specLabel = LibraryText.Find("Library_Field_Spec");
        var groups = new List<LibraryDetailSection>();
        foreach (var group in LibraryItemText.Details(input))
        {
            var section = LibraryDetailSection.From(group);
            if (group.Fields.Any(f => f.Label == specLabel) && Compatibility(item) is { } compatibility)
            {
                section = section with { Fields = [.. section.Fields, compatibility] };
            }

            groups.Add(section);
        }

        if (Time(state, timeSteps, input) is { } time)
        {
            // After Product: when the data is matters as much as where.
            groups.Insert(Math.Min(1, groups.Count), LibraryDetailSection.From(time));
        }

        groups.Add(LibraryDetailSection.Create(Strings.Library_Group_Validation, ValidationFields(validation)));
        return new LibraryDatasetDetails(item.Name, LibraryTextFormat.Product(item), groups, state.NotForNavigation);
    }

    /// <summary>
    /// Says how the edition the dataset declares relates to the editions
    /// SoundCharts reads (EncDotNet.S100's <see cref="SupportedSpecEditions"/>),
    /// as a toned chip: <b>Supported</b> (success), <b>Partly supported</b>
    /// (warning) or <b>Not supported</b> (danger), with a note on the edition
    /// read. An edition the dataset doesn't state is a plain note.
    /// </summary>
    /// <param name="item">The dataset.</param>
    /// <returns>The <b>Compatibility</b> row, or <see langword="null"/> for a product EncDotNet.S100 doesn't assess, such as S-57.</returns>
    public static LibraryDetailRow? Compatibility(CollectionItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var edition = SpecVersion.TryParse(item.ProductSpecVersion, out var parsed) ? parsed : default;
        SpecVersionAssessment? assessment;
        try
        {
            assessment = SupportedSpecEditions.Assess(new SpecRef(item.ProductSpec, edition));
        }
        catch (ArgumentException)
        {
            // A product code EncDotNet.S100 can't name.
            return null;
        }

        if (assessment is null)
        {
            return null;
        }

        var c = CultureInfo.CurrentCulture;
        var implemented = assessment.Implemented.ToString();
        var label = Strings.Library_Field_Compatibility;
        return assessment.Kind switch
        {
            SpecMatchKind.Exact => new(label, null, Strings.Library_Compatibility_Supported, SoundChartsTone.Success),
            SpecMatchKind.CatalogueNewerCompatible => new(
                label,
                string.Format(c, Strings.Library_Compatibility_NewerFormat, implemented),
                Strings.Library_Compatibility_Supported,
                SoundChartsTone.Success),
            SpecMatchKind.CatalogueOlder => new(
                label,
                string.Format(c, Strings.Library_Compatibility_OlderFormat, implemented),
                Strings.Library_Compatibility_PartlySupported,
                SoundChartsTone.Warning),
            SpecMatchKind.MajorDivergence => new(
                label,
                string.Format(c, Strings.Library_Compatibility_UnsupportedFormat, implemented),
                Strings.Library_Compatibility_NotSupported,
                SoundChartsTone.Danger),
            _ => new(label, string.Format(c, Strings.Library_Compatibility_UnknownFormat, implemented)),
        };
    }

    /// <summary>
    /// The validation status as a toned chip (<b>Valid</b> success, errors
    /// danger, warnings only warning, <b>Validating...</b> with a spinner),
    /// then the first findings.
    /// </summary>
    /// <param name="validation">The status.</param>
    public static IReadOnlyList<LibraryDetailRow> ValidationFields(DatasetValidation validation)
    {
        ArgumentNullException.ThrowIfNull(validation);
        var c = CultureInfo.CurrentCulture;
        var label = Strings.Library_Field_Status;
        var status = validation.State switch
        {
            DatasetValidationState.Running => new LibraryDetailRow(label, null, Strings.Library_Validation_Running, IsWorking: true),
            DatasetValidationState.Valid => new LibraryDetailRow(
                label,
                validation.RulesEvaluated > 0 ? string.Format(c, Strings.Library_Validation_ValidRulesFormat, validation.RulesEvaluated) : null,
                Strings.Library_Validation_Valid,
                SoundChartsTone.Success),
            DatasetValidationState.Findings => new LibraryDetailRow(
                label,
                null,
                FindingsText(validation),
                validation.Errors > 0 || validation.Warnings == 0 ? SoundChartsTone.Danger : SoundChartsTone.Warning),
            DatasetValidationState.NoRules => new LibraryDetailRow(label, Strings.Library_Validation_NoRules),
            DatasetValidationState.Failed => new LibraryDetailRow(label, validation.Message, Strings.Library_Validation_Failed, SoundChartsTone.Warning),
            _ => new LibraryDetailRow(label, null, Strings.Library_Validation_NotValidated),
        };

        var fields = new List<LibraryDetailRow> { status };
        foreach (var finding in validation.Findings ?? [])
        {
            var text = string.Format(c, Strings.Library_Validation_FindingFormat, finding.Severity, finding.Message);
            fields.Add(new LibraryDetailRow(finding.RuleId, text));
        }

        return fields;
    }

    /// <summary>"6 steps · hourly", "180 steps · every 10 min", "1 step".</summary>
    /// <param name="steps">The steps, in order.</param>
    public static string TimeStepsText(IReadOnlyList<DateTime> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        var c = CultureInfo.CurrentCulture;
        if (steps.Count <= 1)
        {
            return steps.Count == 0 ? Strings.Library_TimeSteps_None : Strings.Library_TimeSteps_One;
        }

        var count = string.Format(c, Strings.Library_TimeSteps_CountFormat, steps.Count);
        var interval = steps.Zip(steps.Skip(1), (a, b) => b - a)
            .Where(d => d > TimeSpan.Zero)
            .GroupBy(d => d)
            .OrderByDescending(g => g.Count())
            .Select(g => (TimeSpan?)g.Key)
            .FirstOrDefault();
        return interval is { } every ? $"{count} · {Every(every, c)}" : count;
    }

    private static string Every(TimeSpan interval, CultureInfo c) => interval switch
    {
        { TotalHours: 1 } => Strings.Library_TimeSteps_Hourly,
        { TotalDays: 1 } => Strings.Library_TimeSteps_Daily,
        { TotalHours: >= 1 } when interval.TotalHours % 1 == 0 => string.Format(c, Strings.Library_TimeSteps_EveryHoursFormat, interval.TotalHours),
        _ => string.Format(c, Strings.Library_TimeSteps_EveryMinutesFormat, Math.Round(interval.TotalMinutes, 1)),
    };

    private static string FindingsText(DatasetValidation validation)
    {
        var c = CultureInfo.CurrentCulture;
        var errors = validation.Errors == 1 ? Strings.Library_Validation_OneError : string.Format(c, Strings.Library_Validation_ErrorsFormat, validation.Errors);
        var warnings = validation.Warnings == 1 ? Strings.Library_Validation_OneWarning : string.Format(c, Strings.Library_Validation_WarningsFormat, validation.Warnings);
        return (validation.Errors, validation.Warnings) switch
        {
            ( > 0, > 0) => $"{errors}, {warnings}",
            ( > 0, _) => errors,
            (_, > 0) => warnings,
            _ => Strings.Library_Validation_Invalid,
        };
    }

    private static LibraryDetailGroup? Time(LibraryItemState state, IReadOnlyList<DateTime>? steps, LibraryItemTextInput input)
    {
        var fields = new List<LibraryDetailField>();
        string Format(DateTime time) => LibraryTextFormat.Run(new DateTimeOffset(DateTime.SpecifyKind(time, DateTimeKind.Utc)), input.TimeFormat, input.TimeZone);

        var window = steps is { Count: > 0 } ? (steps[0], steps[^1]) : state.ValidWindow;
        if (window is { } range)
        {
            fields.Add(new LibraryDetailField(Strings.Library_Field_TimeRange, $"{Format(range.Item1)} → {Format(range.Item2)}", IsMono: true));
        }

        if (HasTimeSteps(state.Item.ProductSpec))
        {
            var text = steps is null ? Strings.Library_TimeSteps_Reading : TimeStepsText(steps);
            fields.Add(new LibraryDetailField(Strings.Library_Field_TimeSteps, text));
        }

        return fields.Count == 0 ? null : new LibraryDetailGroup(Strings.Library_Group_Time, fields);
    }
}
