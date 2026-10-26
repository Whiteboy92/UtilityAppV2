using System.Diagnostics;
using System.IO;
using System.Net.Http;
using UtilityAppV2.Services.Interfaces;

namespace UtilityAppV2.Services.Implementation
{
    public sealed class YtDlpProvider : IYtDlpProvider
    {
        // GitHub's "latest" alias always 302-redirects to the newest release asset,
        // so this URL needs no version bumping.
        private const string LatestReleaseUrl =
            "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";

        // One HttpClient for the process. Redirects are followed by default, which
        // the "latest" alias relies on.
        private static readonly HttpClient Http = new()
        {
            Timeout = TimeSpan.FromMinutes(5),
        };

        // Serializes concurrent EnsureAsync calls so two downloads can't race onto
        // the same file (e.g. a startup self-update overlapping the first download).
        private readonly SemaphoreSlim gate = new(1, 1);

        public string YtDlpPath { get; }

        private readonly string toolsDir;

        public YtDlpProvider()
        {
            toolsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "UtilityAppV2", "Tools");
            YtDlpPath = Path.Combine(toolsDir, "yt-dlp.exe");
        }

        public async Task<string> EnsureAsync(CancellationToken cancellationToken = default)
        {
            if (File.Exists(YtDlpPath))
                return YtDlpPath;

            await gate.WaitAsync(cancellationToken);
            try
            {
                // Re-check under the lock: another caller may have downloaded it
                // while we were waiting.
                if (!File.Exists(YtDlpPath))
                    await DownloadLatestAsync(cancellationToken);
            }
            finally
            {
                gate.Release();
            }

            return YtDlpPath;
        }

        public async Task EnsureAndUpdateAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var existedBefore = File.Exists(YtDlpPath);
                await EnsureAsync(cancellationToken);

                // A binary we just downloaded is already the latest release; only
                // spend an update run when it was already present from a prior session.
                if (existedBefore)
                    await RunSelfUpdateAsync(cancellationToken);
            }
            catch
            {
                // Best-effort: keep whatever binary we have. Offline or GitHub hiccups
                // must not stop the app from starting.
            }
        }

        private async Task DownloadLatestAsync(CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(toolsDir);

            // Download to a temp file, then atomically move into place so a partial
            // download can never be mistaken for a usable executable.
            var tempPath = YtDlpPath + ".download";

            using (var response = await Http.GetAsync(
                       LatestReleaseUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                await using var httpStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var fileStream = new FileStream(
                    tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await httpStream.CopyToAsync(fileStream, cancellationToken);
            }

            File.Move(tempPath, YtDlpPath, overwrite: true);
        }

        private async Task RunSelfUpdateAsync(CancellationToken cancellationToken)
        {
            var psi = new ProcessStartInfo
            {
                FileName = YtDlpPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            psi.ArgumentList.Add("-U");

            using var process = Process.Start(psi);
            if (process is null)
                return;

            await process.WaitForExitAsync(cancellationToken);
        }
    }
}
