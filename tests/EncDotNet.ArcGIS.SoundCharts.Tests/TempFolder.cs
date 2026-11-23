namespace EncDotNet.SoundCharts.Tests;

/// <summary>A new folder under the temporary folder, deleted on dispose.</summary>
internal sealed class TempFolder : IDisposable
{
    public TempFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SoundChartsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
        Path = LongPath.Expand(Path);
    }

    public string Path { get; }

    /// <summary>Creates an empty file at <paramref name="relativePath"/> and returns its full path.</summary>
    public string File(string relativePath, string content = "")
    {
        var path = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path) ?? Path);
        System.IO.File.WriteAllText(path, content);
        return path;
    }

    /// <summary>Creates a folder at <paramref name="relativePath"/> and returns its full path.</summary>
    public string Folder(string relativePath) => Directory.CreateDirectory(System.IO.Path.Combine(Path, relativePath)).FullName;

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // A server's file may still be closing; the temp folder is cleaned eventually.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
