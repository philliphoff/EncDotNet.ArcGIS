namespace EncDotNet.SoundCharts.Library;

/// <summary>Where a dataset's validation stands.</summary>
public enum DatasetValidationState
{
    /// <summary>Not validated in this session.</summary>
    NotValidated,

    /// <summary><c>s100 validate</c> is running.</summary>
    Running,

    /// <summary>Every rule passed, or only informational findings.</summary>
    Valid,

    /// <summary>Some rules found errors or warnings.</summary>
    Findings,

    /// <summary>EncDotNet.S100 has no validation rules for the product.</summary>
    NoRules,

    /// <summary>Validation couldn't run, or its output couldn't be read.</summary>
    Failed,
}
