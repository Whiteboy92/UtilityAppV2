namespace UtilityAppV2.Services.Interfaces;

public interface IDialogService
{
    Task<(bool Result, string PlaylistUrl, int NumberOfSongs)> ShowDownloadPlaylistDialogAsync();
}
