using System.Diagnostics;
using System.IO;
using UtilityAppV2.Core;
using UtilityAppV2.Models;
using UtilityAppV2.Services.Interfaces;

namespace UtilityAppV2.Services.Implementation;

public class AudioOptimizerService : IAudioOptimizerService
{
    // One ffmpeg encode per logical core — saturate the machine.
    private static readonly int MaxParallelism = Environment.ProcessorCount;

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

        await Parallel.ForEachAsync(mp3Files, new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxParallelism,
            CancellationToken = cancellationToken,
        },
        async (file, token) =>
        {
            try
            {
                var (saved, didShrink) = await OptimizeFileAsync(file, token);
                if (didShrink)
                {
                    Interlocked.Increment(ref optimized);
                    Interlocked.Add(ref bytesSaved, saved);
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
                int done = Interlocked.Increment(ref attempted);
                progress?.Report(done * 100.0 / total);
            }
        });

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

    // Re-encodes to LAME V0 (~245 kbps VBR), which is transparent — audibly
    // identical to the source for human ears. yt-dlp writes bloated 320 kbps CBR
    // even though YouTube's real source is ~128-160 kbps, so V0 reclaims that
    // wasted headroom with no perceptible quality loss. Cover art is dropped and
    // ID3 tags preserved. The result replaces the original only when it's
    // actually smaller, so already-lean files are left untouched.
    private async Task<(long BytesSaved, bool DidShrink)> OptimizeFileAsync(string file, CancellationToken cancellationToken)
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
        // LAME V0 VBR — transparent quality, smaller than 320k CBR.
        psi.ArgumentList.Add("-c:a");
        psi.ArgumentList.Add("libmp3lame");
        psi.ArgumentList.Add("-q:a");
        psi.ArgumentList.Add("0");
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

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Kill the encode so it stops writing and releases the temp file
                // before the finally block tries to delete it.
                TryKill(process);
                throw;
            }

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

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Process already exited or is inaccessible — nothing to clean up.
        }
    }
}
