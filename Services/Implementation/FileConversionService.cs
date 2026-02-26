using System.IO;
using ImageMagick;
using UtilityAppV2.Models;
using UtilityAppV2.Services.Interfaces;

namespace UtilityAppV2.Services.Implementation;

public class FileConversionService : IFileConversionService
{
    private static readonly string[] HeicExt =
    [
        ".heic",
    ];

    public async Task<ConversionResult> ConvertFilesToPngAsync(
        string sourceFolder,
        IProgress<double> progress,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(sourceFolder))
            throw new DirectoryNotFoundException($"Folder not found: {sourceFolder}");

        var sw = System.Diagnostics.Stopwatch.StartNew();

        var allFiles = Directory.GetFiles(sourceFolder);

        var heicFiles = allFiles
            .Where(f => HeicExt.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToArray();

        int total = heicFiles.Length;
        int converted = 0;

        string backupFolder =
            Path.Combine(
                Directory.GetParent(sourceFolder)!.FullName,
                $"{Path.GetFileName(sourceFolder)}_Backup_{DateTime.Now:yyyyMMddHHmmss}"
            );

        await Task.Run(() =>
        {
            Directory.CreateDirectory(backupFolder);

            Parallel.ForEach(allFiles, file =>
            {
                File.Copy(file, Path.Combine(backupFolder, Path.GetFileName(file)), overwrite: true);
            });
        }, cancellationToken);

        if (total == 0)
        {
            sw.Stop();
            progress?.Report(100);

            return new ConversionResult
            {
                TotalFiles = 0,
                ConvertedFiles = 0,
                Duration = sw.Elapsed,
            };
        }

        await Parallel.ForEachAsync(heicFiles, new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken,
        },
        async (file, token) =>
        {
            try
            {
                using var img = new MagickImage(file);

                img.Format = MagickFormat.Png;
                img.Quality = 100;
                img.Depth = 8;
                img.Quality = 95;

                string newFile = Path.Combine(
                    sourceFolder,
                    Path.GetFileNameWithoutExtension(file) + ".png"
                );

                await img.WriteAsync(newFile, token);

                File.Delete(file);

                int count = Interlocked.Increment(ref converted);
                progress.Report(count * 100.0 / total);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting {file}: {ex.Message}");
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
