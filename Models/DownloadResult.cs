namespace UtilityAppV2.Models
{
    public class DownloadResult
    {
        public int TotalSongs { get; set; }
        public int DownloadedSongs { get; set; }
        public string FirstSongName { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }

        /// <summary>True when yt-dlp reported a non-zero exit code.</summary>
        public bool HadErrors { get; set; }

        /// <summary>Captured yt-dlp error output (truncated); null when no error.</summary>
        public string? ErrorDetail { get; set; }
    }
}
