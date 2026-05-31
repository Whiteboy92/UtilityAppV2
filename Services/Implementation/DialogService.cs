using System.Windows;
using Ookii.Dialogs.Wpf;
using UtilityAppV2.Services.Interfaces;
using UtilityAppV2.UI_Components.Download;
using UtilityAppV2.ViewModels;

namespace UtilityAppV2.Services.Implementation;

public class DialogService(ISettingsService settingsService) : IDialogService
{
    public async Task<(bool Result, string PlaylistUrl, int NumberOfSongs)> ShowDownloadPlaylistDialogAsync()
    {
        var settings = await settingsService.LoadSettingsAsync();

        return Invoke(() =>
        {
            var vm = new DownloadPlaylistInputDialogViewModel(settings);
            var dialog = new DownloadPlaylistInputDialog(vm)
            {
                Owner = Application.Current.MainWindow,
            };

            bool? result = dialog.ShowDialog();
            return result != true
                ? (false, string.Empty, 0)
                : (true, vm.PlaylistUrl ?? string.Empty, vm.NumberOfSongs);
        });
    }

    public void ShowError(string message, string title = "Error") =>
        Show(message, title, MessageBoxImage.Error);

    public void ShowWarning(string message, string title = "Warning") =>
        Show(message, title, MessageBoxImage.Warning);

    public void ShowInfo(string message, string title = "Information") =>
        Show(message, title, MessageBoxImage.Information);

    public string? PickFolder(string? title = null)
    {
        return Invoke(() =>
        {
            var dialog = new VistaFolderBrowserDialog();
            if (!string.IsNullOrWhiteSpace(title))
                dialog.Description = title;

            return dialog.ShowDialog() == true ? dialog.SelectedPath : null;
        });
    }

    private static void Show(string message, string title, MessageBoxImage image) =>
        Invoke(() => MessageBox.Show(message, title, MessageBoxButton.OK, image));

    /// <summary>Marshals UI work onto the dispatcher so services can be called from any thread.</summary>
    private static T Invoke<T>(Func<T> func)
    {
        var dispatcher = Application.Current?.Dispatcher;
        return dispatcher is null || dispatcher.CheckAccess()
            ? func()
            : dispatcher.Invoke(func);
    }

    private static void Invoke(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
            action();
        else
            dispatcher.Invoke(action);
    }
}
