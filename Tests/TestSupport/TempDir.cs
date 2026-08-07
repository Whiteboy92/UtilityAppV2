using System.IO;

namespace UtilityAppV2.Tests.TestSupport;

/// <summary>
/// Disposable scratch directory under the OS temp folder. Created on construction,
/// recursively deleted on Dispose. Use with `using` so tests never leak folders.
/// </summary>
public sealed class TempDir : IDisposable
{
    public string Path { get; }

    public TempDir()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "UtilityAppV2Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>Creates an empty file with the given name inside this dir; returns its full path.</summary>
    public string Touch(string fileName)
    {
        string full = System.IO.Path.Combine(Path, fileName);
        File.WriteAllText(full, string.Empty);
        return full;
    }

    public string Combine(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
        catch
        {
            // Best-effort cleanup — a locked file must not fail the test run.
        }
    }
}
