using UtilityAppV2.Core;
using UtilityAppV2.Settings;

namespace UtilityAppV2.ViewModels;

public class DownloadPlaylistInputDialogViewModel : BaseViewModel
{
    public DownloadPlaylistInputDialogViewModel(UserSettings? settings)
    {
        PlaylistUrl = settings?.PlaylistLink ?? string.Empty;
        FirstDownloadedSong = settings?.FirstDownloadedSong ?? string.Empty;
    }

    public string? PlaylistUrl
    {
        get;
        set => SetProperty(ref field, value);
    }

    public int NumberOfSongs
    {
        get;
        set => SetProperty(ref field, value);
    } = 1;

    public string? FirstDownloadedSong
    {
        get;
        set => SetProperty(ref field, value);
    }
}