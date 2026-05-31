using System.Diagnostics;
using System.IO;
using UtilityAppV2.Core;
using UtilityAppV2.Models;
using UtilityAppV2.Services.Interfaces;

namespace UtilityAppV2.Services.Implementation;

public class AudioOptimizerService : IAudioOptimizerService
{
    private readonly string ffmpegPath;

    public AudioOptimizerService()
    {
        ffmpegPath = Path.Combine(AppContext.BaseDirectory, FilePathConstants.FfmpegPath);
    }

    public async Task<AudioOptimizeResult> OptimizeFolderAsync(
        string folder,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(folder))
            throw new DirectoryNotFoundException($"Folder not found: {folder}");

        if (!File.Exists(ffmpegPath))
            throw new FileNotFoundException("ffmpeg executable not found.", ffmpegPath);

        var sw = Stopwatch.StartNew();

        var mp3Files = Directory
            .GetFiles(folder, "*.mp3", SearchOption.TopDirectoryOnly);

        int total = mp3Files.Length;

        if (total == 0)
        {
            sw.Stop();
            progress?.Report(100);
            return new AudioOptimizeResult { TotalFiles = 0, OptimizedFiles = 0, BytesSaved = 0, Duration = sw.Elapsed };
        }

        int optimized = 0;
        long bytesSaved = 0;
        int attempted = 0;

        await Task.Run(() =>
        {
            foreach (var file in mp3Files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var (saved, didShrink) = OptimizeFile(file);
                    if (didShrink)
                    {
                        optimized++;
                        bytesSaved += saved;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error optimizing {file}: {ex.Message}");
                }
                finally
                {
                    attempted++;
                    progress?.Report(attempted * 100.0 / total);
                }
            }
        }, cancellationToken);

        progress?.Report(100);
        sw.Stop();

        return new AudioOptimizeResult
        {
            TotalFiles = total,
            OptimizedFiles = optimized,
            BytesSaved = bytesSaved,
            Duration = sw.Elapsed,
        };
    }

    // Strips embedded cover art / padding by copying only the audio stream
    // (no re-encode). The result replaces the original only when it's actually
    // smaller, so files without art are left untouched.
    private (long BytesSaved, bool DidShrink) OptimizeFile(string file)
    {
        long originalSize = new FileInfo(file).Length;
        string tempPath = file + ".opt.tmp.mp3";

        var psi = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        psi.ArgumentList.Add("-y");
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(file);
        // Audio only: drops the attached cover-art video stream.
        psi.ArgumentList.Add("-map");
        psi.ArgumentList.Add("0:a");
        // Bit-for-bit copy — no re-encode, no quality loss.
        psi.ArgumentList.Add("-c:a");
        psi.ArgumentList.Add("copy");
        // Preserve ID3 tags.
        psi.ArgumentList.Add("-map_metadata");
        psi.ArgumentList.Add("0");
        psi.ArgumentList.Add("-id3v2_version");
        psi.ArgumentList.Add("3");
        psi.ArgumentList.Add(tempPath);

        try
        {
            using var process = Process.Start(psi)
                                ?? throw new InvalidOperationException("Failed to start ffmpeg process.");

            // Drain pipes so ffmpeg never blocks on a full stderr buffer.
            process.OutputDataReceived += (_, _) => { };
            process.ErrorDataReceived += (_, _) => { };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            if (process.ExitCode != 0 || !File.Exists(tempPath))
                return (0, false);

            long newSize = new FileInfo(tempPath).Length;

            // Only adopt the optimized file when it actually saved space.
            if (newSize > 0 && newSize < originalSize)
            {
                File.Delete(file);
                File.Move(tempPath, file);
                return (originalSize - newSize, true);
            }

            return (0, false);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { /* best-effort cleanup */ }
            }
        }
    }
}
