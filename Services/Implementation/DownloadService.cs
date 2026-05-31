using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UtilityAppV2.Core;
using UtilityAppV2.Models;
using UtilityAppV2.Services.Interfaces;

namespace UtilityAppV2.Services.Implementation
{
    public partial class DownloadService : IDownloadService
    {
        private readonly string ytDlpPath;
        private readonly string toolsDir;
        private readonly string ffmpegPath;
        private readonly string ffprobePath;

        public DownloadService()
        {
            var baseDir = AppContext.BaseDirectory;

            ytDlpPath = Path.Combine(baseDir, FilePathConstants.YtDlpPath);
            toolsDir = Path.Combine(baseDir, "Resources", "Tools");
            ffmpegPath = Path.Combine(toolsDir, "ffmpeg.exe");
            ffprobePath = Path.Combine(toolsDir, "ffprobe.exe");
        }

        public async Task<DownloadResult> DownloadPlaylistAsync(
            string playlistUrl,
            int numberOfSongs,
            string downloadFolder,
            IProgress<(int downloaded, int total, string currentSong)>? progress,
            CancellationToken cancellationToken)
        {
            ValidateInputs(playlistUrl, numberOfSongs);
            EnsureToolsExist();

            Directory.CreateDirectory(downloadFolder);

            var result = new DownloadResult { TotalSongs = numberOfSongs };
            var startTime = DateTime.UtcNow;

            var psi = BuildProcessStartInfo(playlistUrl, numberOfSongs, downloadFolder);

            using var process = Process.Start(psi)
                                ?? throw new InvalidOperationException("Failed to start yt-dlp process.");

            var stderrBuilder = new StringBuilder();

            // Progress is derived from yt-dlp's own output (single-threaded per stream),
            // which is authoritative — no fragile directory polling.
            int total = numberOfSongs;
            int downloadedCount = 0;
            string? firstSongName = null;

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is null) return;

                var itemMatch = ItemProgressRegex().Match(e.Data);
                if (itemMatch.Success)
                {
                    total = int.Parse(itemMatch.Groups[2].Value);
                    return;
                }

                var destMatch = ExtractDestinationRegex().Match(e.Data);
                if (destMatch.Success)
                {
                    var song = Path.GetFileNameWithoutExtension(destMatch.Groups[1].Value.Trim());
                    firstSongName ??= song;
                    downloadedCount++;
                    progress?.Report((downloadedCount, Math.Max(total, downloadedCount), song));
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    stderrBuilder.AppendLine(e.Data);
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Cancellation must terminate yt-dlp (and its ffmpeg child); otherwise the
                // process keeps downloading and writing files after the user canceled.
                TryKill(process);
                throw;
            }

            result.DownloadedSongs = downloadedCount;
            result.FirstSongName = firstSongName ?? string.Empty;
            result.Duration = DateTime.UtcNow - startTime;

            if (process.ExitCode != 0)
            {
                result.HadErrors = true;
                result.ErrorDetail = BuildErrorDetail(stderrBuilder.ToString());
            }

            return result;
        }

        private static void ValidateInputs(string playlistUrl, int numberOfSongs)
        {
            if (string.IsNullOrWhiteSpace(playlistUrl))
                throw new ArgumentException("Playlist URL cannot be null or empty.", nameof(playlistUrl));

            if (!Uri.TryCreate(playlistUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("Playlist URL must be a valid http(s) URL.", nameof(playlistUrl));
            }

            if (numberOfSongs <= 0)
                throw new ArgumentOutOfRangeException(nameof(numberOfSongs), "Number of songs must be greater than zero.");
        }

        private void EnsureToolsExist()
        {
            if (!File.Exists(ytDlpPath))
                throw new FileNotFoundException("yt-dlp executable not found.", ytDlpPath);

            if (!File.Exists(ffmpegPath))
                throw new FileNotFoundException("ffmpeg executable not found.", ffmpegPath);

            if (!File.Exists(ffprobePath))
                throw new FileNotFoundException("ffprobe executable not found.", ffprobePath);
        }

        private ProcessStartInfo BuildProcessStartInfo(string playlistUrl, int numberOfSongs, string downloadFolder)
        {
            var outputTemplate = Path.Combine(downloadFolder, "%(title)s.%(ext)s");

            // Point yt-dlp to Deno explicitly so it works regardless of system PATH.
            var denoPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "deno", "deno.exe");

            var psi = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            // ArgumentList escapes each value individually — no manual quoting, no injection.
            psi.ArgumentList.Add("--ffmpeg-location");
            psi.ArgumentList.Add(toolsDir);

            if (File.Exists(denoPath))
            {
                psi.ArgumentList.Add("--js-runtimes");
                psi.ArgumentList.Add(denoPath);
            }

            psi.ArgumentList.Add("--extract-audio");
            psi.ArgumentList.Add("--audio-format");
            psi.ArgumentList.Add("mp3");
            psi.ArgumentList.Add("--audio-quality");
            psi.ArgumentList.Add("0");
            psi.ArgumentList.Add("--no-keep-video");
            psi.ArgumentList.Add("--ignore-errors");
            psi.ArgumentList.Add("--newline");
            psi.ArgumentList.Add("--playlist-items");
            psi.ArgumentList.Add($"1-{numberOfSongs}");
            psi.ArgumentList.Add("-o");
            psi.ArgumentList.Add(outputTemplate);
            psi.ArgumentList.Add(playlistUrl);

            return psi;
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

        private static string BuildErrorDetail(string stderrOutput)
        {
            if (string.IsNullOrWhiteSpace(stderrOutput))
                return "No additional error output captured.";

            return stderrOutput.Length > 500
                ? stderrOutput[..500] + "\n...(truncated)"
                : stderrOutput;
        }

        [GeneratedRegex(@"Downloading (?:item|video) (\d+) of (\d+)", RegexOptions.IgnoreCase)]
        private static partial Regex ItemProgressRegex();

        [GeneratedRegex(@"\[ExtractAudio\] Destination: (.+)", RegexOptions.IgnoreCase)]
        private static partial Regex ExtractDestinationRegex();
    }
}
