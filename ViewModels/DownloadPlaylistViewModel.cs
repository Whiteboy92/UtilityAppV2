using System.IO;
using System.Windows;
using System.Windows.Input;
using UtilityAppV2.Commands;
using UtilityAppV2.Core;
using UtilityAppV2.Models;
using UtilityAppV2.Services.Interfaces;
using UtilityAppV2.Settings;

namespace UtilityAppV2.ViewModels
{
    public class DownloadPlaylistViewModel : BaseViewModel
    {
        private readonly IDownloadService downloadService;
        private readonly IDialogService dialogService;
        private readonly ISettingsService settingsService;

        private CancellationTokenSource? cancellationTokenSource;
        private UserSettings settings = new();

        public DownloadPlaylistViewModel(IDownloadService downloadService,
                                         IDialogService dialogService,
                                         ISettingsService settingsService)
        {
            this.downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            DownloadCommand = new RelayCommand(_ => RunDownloadPlaylistAsync(), _ => !IsDownloading);
            CancelCommand = new RelayCommand(_ => CancelDownload(), _ => IsDownloading);

            LoadUserSettingsAsync();
        }

        private async void LoadUserSettingsAsync()
        {
            settings = await settingsService.LoadSettingsAsync();
            PlaylistUrl = settings.PlaylistLink;
            FirstDownloadedSong = settings.FirstDownloadedSong;
        }

        public string? PlaylistUrl
        {
            get;
            set
            {
                field = value;
                OnPropertyChanged();
            }
        } = string.Empty;

        public string? FirstDownloadedSong
        {
            get;
            set
            {
                field = value;
                OnPropertyChanged();
            }
        } = string.Empty;

        public double Progress
        {
            get;
            set
            {
                field = value;
                OnPropertyChanged();
            }
        }

        public bool IsDownloading
        {
            get;
            set
            {
                field = value;
                OnPropertyChanged();
                ((RelayCommand)DownloadCommand).RaiseCanExecuteChanged();
                ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
            }
        }

        public ICommand DownloadCommand { get; }
        public ICommand CancelCommand { get; }

        private async void RunDownloadPlaylistAsync()
        {
            try
            {
                await DownloadPlaylistAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DownloadPlaylistAsync()
        {
            var dialogResult = await dialogService.ShowDownloadPlaylistDialogAsync();
            if (!dialogResult.Result) return;

            string playlistUrlInput = dialogResult.PlaylistUrl;
            int numberOfSongs = dialogResult.NumberOfSongs;

            string downloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Playlist");
            Directory.CreateDirectory(downloadFolder);

            IsDownloading = true;
            Progress = 0;
            cancellationTokenSource = new CancellationTokenSource();

            try
            {
                // Progress reporter marshaled to UI thread automatically
                var progressReporter = new Progress<(int downloaded, int total, string currentSong)>(report =>
                {
                    Progress = Math.Min(100, (report.downloaded * 100.0) / report.total);
                });

                UserSettings userSettings = await settingsService.LoadSettingsAsync();

                // Run download on background thread
                DownloadResult result = await Task.Run(() =>
                    downloadService.DownloadPlaylistAsync(
                        playlistUrlInput,
                        numberOfSongs,
                        downloadFolder,
                        progressReporter,
                        cancellationTokenSource.Token,
                        userSettings));

                // Update settings and UI
                settings.FirstDownloadedSong = result.FirstSongName;
                settings.PlaylistLink = playlistUrlInput;
                await settingsService.SaveSettingsAsync(settings);

                PlaylistUrl = settings.PlaylistLink;
                FirstDownloadedSong = settings.FirstDownloadedSong;

                MessageBox.Show($"Playlist downloaded successfully!\nFirst song: {result.FirstSongName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Download was canceled.", "Canceled", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                IsDownloading = false;
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
                Progress = 0;
            }
        }

        private void CancelDownload()
        {
            cancellationTokenSource?.Cancel();
        }
    }
}
