using System.IO;
using ImageMagick;
using UtilityAppV2.Models;
using UtilityAppV2.Services.Interfaces;

namespace UtilityAppV2.Services.Implementation;

public class FileConversionService : IFileConversionService
{
    private static readonly string[] HeicExtensions = [".heic", ".heif"];

    // Decode/encode is CPU-bound and per-image work is single-threaded (below),
    // so use every core — parallelism across files is the main speed lever.
    private static readonly int MaxParallelism = Environment.ProcessorCount;

    static FileConversionService()
    {
        // Give each image one internal thread: the file-level Parallel.ForEachAsync
        // already saturates the cores, so per-image threading would only
        // oversubscribe and add contention.
        ResourceLimits.Thread = 1;
    }

    public async Task<ConversionResult> ConvertFilesToPngAsync(
        string sourceFolder,
        IProgress<double>? progress,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(sourceFolder))
            throw new DirectoryNotFoundException($"Folder not found: {sourceFolder}");

        var sw = System.Diagnostics.Stopwatch.StartNew();

        var allFiles = Directory.GetFiles(sourceFolder);

        var heicFiles = allFiles
            .Where(f => HeicExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .ToArray();

        int total = heicFiles.Length;

        // Nothing to convert — bail out before any disk work (no backup needed).
        if (total == 0)
        {
            sw.Stop();
            progress?.Report(100);
            return new ConversionResult { TotalFiles = 0, ConvertedFiles = 0, Duration = sw.Elapsed };
        }

        // Back up before mutating. Only the HEIC files are deleted/replaced, so
        // only those need backing up — copying the whole folder would duplicate
        // gigabytes of unrelated files on every run.
        // GetParent is null for a drive root (e.g. "D:\"); fall back to the source
        // folder itself so we never dereference null.
        string parentFolder = Directory.GetParent(sourceFolder)?.FullName ?? sourceFolder;
        string sourceName = Path.GetFileName(sourceFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        string backupFolder = Path.Combine(
            parentFolder,
            $"{sourceName}_Backup_{DateTime.Now:yyyyMMddHHmmss}");

        await Task.Run(() =>
        {
            Directory.CreateDirectory(backupFolder);
            foreach (var file in heicFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                File.Copy(file, Path.Combine(backupFolder, Path.GetFileName(file)), overwrite: true);
            }
        }, cancellationToken);

        // Pick every output name up front, on one thread. Doing it inside the
        // parallel loop races: "IMG.heic" and "IMG.HEIF" would both see "IMG.png"
        // as free and one write would clobber the other.
        var outputPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in heicFiles)
        {
            // Never overwrite an existing file either — "foo (1).png" style, matching
            // FileSystemHelper.GetUniqueFilePath.
            string baseName = Path.GetFileNameWithoutExtension(file);
            string candidate = Path.Combine(sourceFolder, baseName + ".png");
            for (int i = 1; reserved.Contains(candidate) || File.Exists(candidate); i++)
                candidate = Path.Combine(sourceFolder, $"{baseName} ({i}).png");

            reserved.Add(candidate);
            outputPaths[file] = candidate;
        }

        int converted = 0;
        var failures = new System.Collections.Concurrent.ConcurrentBag<string>();

        await Parallel.ForEachAsync(heicFiles, new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxParallelism,
            CancellationToken = cancellationToken,
        },
        (file, token) =>
        {
            try
            {
                token.ThrowIfCancellationRequested();

                using var img = new MagickImage(file);

                img.Format = MagickFormat.Png;

                // PNG is lossless at every compression level — level only trades
                // file size for encode speed. Level 1 + no adaptive filtering is
                // far faster than the default max effort, with zero quality loss.
                img.Settings.SetDefine(MagickFormat.Png, "compression-level", "1");
                img.Settings.SetDefine(MagickFormat.Png, "compression-filter", "0");

                // Note: the app uses Magick.NET-Q8, so pixels are held at 8 bits per
                // channel — a 10-bit HEIC is reduced to 8-bit on decode. Fine for
                // typical phone photos; switch to the Q16 package to keep >8-bit depth.

                // Synchronous Write: WriteAsync encodes to a memory buffer first and
                // then copies it out, an extra pass with no benefit inside this
                // already-parallel loop.
                img.Write(outputPaths[file]);

                File.Delete(file);

                int count = Interlocked.Increment(ref converted);
                progress?.Report(count * 100.0 / total);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error converting {file}: {ex.Message}");
                failures.Add($"{Path.GetFileName(file)}: {ex.Message}");
            }

            return ValueTask.CompletedTask;
        });

        // Snap the bar to complete even if some files failed, so the UI never appears
        // stuck below 100% when converted < total.
        progress?.Report(100);

        sw.Stop();

        return new ConversionResult
        {
            TotalFiles = total,
            ConvertedFiles = converted,
            Duration = sw.Elapsed,
            FailedFiles = failures.ToArray(),
        };
    }
}
