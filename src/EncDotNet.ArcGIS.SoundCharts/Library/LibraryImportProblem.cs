using System.Globalization;
using EncDotNet.SoundCharts.Resources;

namespace EncDotNet.SoundCharts.Library;

/// <summary>A path <see cref="SoundChartsLibrary.ImportAsync"/> didn't add.</summary>
/// <param name="Path">The path.</param>
/// <param name="Kind">Why.</param>
/// <param name="Detail">More, such as why a collection manifest couldn't be read.</param>
public sealed record LibraryImportProblem(string Path, LibraryImportProblemKind Kind, string? Detail = null)
{
    /// <summary>Gets the line that says why the path wasn't added, such as <c>C:\charts: already in the Library</c>.</summary>
    public string Describe()
    {
        var c = CultureInfo.CurrentCulture;
        return Kind switch
        {
            LibraryImportProblemKind.NotFound => string.Format(c, Strings.Library_Problem_NotFoundFormat, Path),
            LibraryImportProblemKind.AlreadyInLibrary => string.Format(c, Strings.Library_Problem_AlreadyInLibraryFormat, Path),
            _ => string.Format(c, Strings.Library_Problem_UnreadableFormat, Path, Detail ?? string.Empty).TrimEnd(),
        };
    }
}
