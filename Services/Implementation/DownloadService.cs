using System.IO;
using System.Text;
using System.Windows;
using UtilityAppV2.Core;
using UtilityAppV2.Models;
using UtilityAppV2.Services.Interfaces;
using UtilityAppV2.Settings;

namespace UtilityAppV2.Services.Implementation
{
    public class DownloadService : IDownloadService
    {
        private readonly string ytDlpPath;
        private readonly string toolsDir;

        public DownloadService()
        {
            var baseDir = AppContext.BaseDirectory;

            ytDlpPath = Path.Combine(baseDir, FilePathConstants.YtDlpPath);
            toolsDir = Path.Combine(baseDir, "Resources", "Tools");

            if (!File.Exists(ytDlpPath))
                throw new FileNotFoundException("yt-dlp executable not found.", ytDlpPath);

            if (!File.Exists(Path.Combine(toolsDir, "ffmpeg.exe")))
                throw new FileNotFoundException("ffmpeg executable not found.", Path.Combine(toolsDir, "ffmpeg.exe"));

            if (!File.Exists(Path.Combine(toolsDir, "ffprobe.exe")))
                throw new FileNotFoundException("ffprobe executable not found.", Path.Combine(toolsDir, "ffprobe.exe"));
        }

        public async Task<DownloadResult> DownloadPlaylistAsync(
            string playlistUrl,
            int numberOfSongs,
            string downloadFolder,
            IProgress<(int downloaded, int total, string currentSong)>? progress,
            CancellationToken cancellationToken,
            UserSettings? userSettings = null)
        {
            if (string.IsNullOrWhiteSpace(playlistUrl))
                throw new ArgumentException("Playlist URL cannot be null or empty.", nameof(playlistUrl));

            if (numberOfSongs <= 0)
                throw new ArgumentOutOfRangeException(nameof(numberOfSongs), "Number of songs must be greater than zero.");

            Directory.CreateDirectory(downloadFolder);

            var result = new DownloadResult
            {
                TotalSongs = numberOfSongs,
            };

            var startTime = DateTime.UtcNow;
            var outputTemplate = Path.Combine(downloadFolder, "%(title)s.%(ext)s");

            // Point yt-dlp to Deno explicitly so it works regardless of system PATH
            var denoPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "deno", "deno.exe");
            
            var jsRuntimeArg = File.Exists(denoPath) ? $"--js-runtimes \"{denoPath}\" " : "";

            // toolsDir is the folder — yt-dlp will find ffmpeg.exe and ffprobe.exe there automatically
            var args = $"--ffmpeg-location \"{toolsDir}\" " +
                       jsRuntimeArg +
                       "--extract-audio --audio-format mp3 --audio-quality 0 " +
                       "--no-keep-video " +
                       $"--playlist-items 1-{numberOfSongs} --ignore-errors " +
                       $"-o \"{outputTemplate}\" \"{playlistUrl}\"";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            using var process = System.Diagnostics.Process.Start(psi)
                             ?? throw new InvalidOperationException("Failed to start yt-dlp process.");

            // Read stderr and stdout asynchronously to prevent buffer deadlocks
            var stderrBuilder = new StringBuilder();
            var stdoutBuilder = new StringBuilder();

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    stderrBuilder.AppendLine(e.Data);
            };

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    stdoutBuilder.AppendLine(e.Data);
            };

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            var existingFiles = Directory.GetFiles(downloadFolder, "*.mp3").ToHashSet();
            int downloadedCount = 0;

            while (!process.HasExited)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var mp3Files = Directory.GetFiles(downloadFolder, "*.mp3")
                    .Where(f => !existingFiles.Contains(f))
                    .OrderBy(f => f)
                    .ToList();

                foreach (var file in mp3Files)
                {
                    existingFiles.Add(file);
                    downloadedCount++;
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
                    progress?.Report((downloadedCount, numberOfSongs, fileNameWithoutExt));
                }

                await Task.Delay(100, cancellationToken);
            }

            await process.WaitForExitAsync(cancellationToken);

            // Catch any files that finished right as the process exited
            var finalFiles = Directory.GetFiles(downloadFolder, "*.mp3")
                .Where(f => !existingFiles.Contains(f))
                .OrderBy(f => f)
                .ToList();

            foreach (var file in finalFiles)
            {
                existingFiles.Add(file);
                downloadedCount++;
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
                progress?.Report((downloadedCount, numberOfSongs, fileNameWithoutExt));
            }

            // First downloaded song (by creation time)
            string? firstSongName = Directory.GetFiles(downloadFolder, "*.mp3")
                .OrderBy(File.GetCreationTime)
                .Select(Path.GetFileNameWithoutExtension)
                .FirstOrDefault();

            if (userSettings != null && firstSongName != null)
                userSettings.FirstDownloadedSong = firstSongName;

            result.DownloadedSongs = downloadedCount;
            result.FirstSongName = firstSongName ?? string.Empty;
            result.Duration = DateTime.UtcNow - startTime;

            if (process.ExitCode != 0)
            {
                var stderrOutput = stderrBuilder.ToString();
                var errorDetail = string.IsNullOrWhiteSpace(stderrOutput)
                    ? "No additional error output captured."
                    : stderrOutput.Length > 500
                        ? stderrOutput[..500] + "\n...(truncated)"
                        : stderrOutput;

                string msg = $"Some videos failed to download.\n\n" +
                             $"Total songs requested: {numberOfSongs}\n" +
                             $"Successfully downloaded: {downloadedCount}\n" +
                             (!string.IsNullOrEmpty(firstSongName) ? $"First downloaded song: {firstSongName}\n" : "") +
                             $"\nyt-dlp error output:\n{errorDetail}";

                MessageBox.Show(msg, "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return result;
        }
    }
}
