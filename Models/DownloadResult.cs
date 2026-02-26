namespace UtilityAppV2.Models
{
    public class DownloadResult
    {
        public int TotalSongs { get; set; }
        public int DownloadedSongs { get; set; }
        public string FirstSongName { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
    }
}