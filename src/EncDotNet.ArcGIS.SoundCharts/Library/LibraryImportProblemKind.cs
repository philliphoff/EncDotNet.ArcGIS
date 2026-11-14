namespace EncDotNet.SoundCharts.Library;

/// <summary>Why a path wasn't added to the library.</summary>
public enum LibraryImportProblemKind
{
    /// <summary>The path doesn't exist.</summary>
    NotFound,

    /// <summary>The path is already in the library, or under a folder that is.</summary>
    AlreadyInLibrary,

    /// <summary>The path couldn't be read, such as a collection manifest that isn't valid.</summary>
    Unreadable,
}
