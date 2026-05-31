using System.IO;
using ImageMagick;
using UtilityAppV2.Models;
using UtilityAppV2.Services.Interfaces;

namespace UtilityAppV2.Services.Implementation;

public class FileConversionService : IFileConversionService
{
    private static readonly string[] HeicExtensions = [".heic", ".heif"];

    // Cap parallelism so several large HEIC decodes can't exhaust memory.
    private static readonly int MaxParallelism = Math.Min(Environment.ProcessorCount, 4);

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

        // Back up the entire source folder before mutating it.
        string backupFolder = Path.Combine(
            Directory.GetParent(sourceFolder)!.FullName,
            $"{Path.GetFileName(sourceFolder)}_Backup_{DateTime.Now:yyyyMMddHHmmss}");

        await Task.Run(() =>
        {
            Directory.CreateDirectory(backupFolder);
            foreach (var file in allFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                File.Copy(file, Path.Combine(backupFolder, Path.GetFileName(file)), overwrite: true);
            }
        }, cancellationToken);

        int converted = 0;

        await Parallel.ForEachAsync(heicFiles, new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxParallelism,
            CancellationToken = cancellationToken,
        },
        async (file, token) =>
        {
            try
            {
                using var img = new MagickImage(file);

                img.Format = MagickFormat.Png;
                img.Depth = 8;
                img.Quality = 95;

                string newFile = Path.Combine(
                    sourceFolder,
                    Path.GetFileNameWithoutExtension(file) + ".png");

                await img.WriteAsync(newFile, token);

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
            }
        });

        sw.Stop();

        return new ConversionResult
        {
            TotalFiles = total,
            ConvertedFiles = converted,
            Duration = sw.Elapsed,
        };
    }
}
