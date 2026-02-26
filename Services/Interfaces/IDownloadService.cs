using UtilityAppV2.Models;
using UtilityAppV2.Settings;

namespace UtilityAppV2.Services.Interfaces
{
    public interface IDownloadService
    {
        Task<DownloadResult> DownloadPlaylistAsync(
            string playlistUrl,
            int numberOfSongs,
            string downloadFolder,
            IProgress<(int downloaded, int total, string currentSong)> progress,
            CancellationToken cancellationToken,
            UserSettings userSettings);
    }
}