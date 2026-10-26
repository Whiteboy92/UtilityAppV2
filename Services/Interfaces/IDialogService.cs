namespace UtilityAppV2.Services.Interfaces;

public interface IDialogService
{
    Task<(bool Result, string PlaylistUrl, int NumberOfSongs)> ShowDownloadPlaylistDialogAsync();

    void ShowError(string message, string title = "Error");

    void ShowWarning(string message, string title = "Warning");

    void ShowInfo(string message, string title = "Information");

    /// <summary>Shows a Yes/No prompt; returns true when the user chooses Yes.</summary>
    bool Confirm(string message, string title = "Confirm");

    /// <summary>Shows a folder picker; returns the selected path or null if cancelled.</summary>
    string? PickFolder(string? title = null);
}
