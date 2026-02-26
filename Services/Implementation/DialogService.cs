using System.Windows;
using UtilityAppV2.Services.Interfaces;
using UtilityAppV2.UI_Components.Download;
using UtilityAppV2.ViewModels;

namespace UtilityAppV2.Services.Implementation;

public class DialogService(ISettingsService settingsService) : IDialogService
{
    public async Task<(bool Result, string PlaylistUrl, int NumberOfSongs)> ShowDownloadPlaylistDialogAsync()
    {
        var settings = await settingsService.LoadSettingsAsync();
        var vm = new DownloadPlaylistInputDialogViewModel(settings);

        var dialog = new DownloadPlaylistInputDialog(vm)
        {
            Owner = Application.Current.MainWindow,
        };

        bool? result = dialog.ShowDialog();
        if (result != true)
        {
            return (false, "", 0);
        }

        return (true, vm.PlaylistUrl, vm.NumberOfSongs)!;
    }
}