using System.IO;
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

            DownloadCommand = new RelayCommand(_ => _ = RunDownloadPlaylistAsync(), _ => !IsDownloading);
            CancelCommand = new RelayCommand(_ => CancelDownload(), _ => IsDownloading);
        }

        /// <summary>Loads persisted settings. Invoked from the view's Loaded event (not the ctor).</summary>
        public async Task InitializeAsync()
        {
            try
            {
                settings = await settingsService.LoadSettingsAsync();
                PlaylistUrl = settings.PlaylistLink;
                FirstDownloadedSong = settings.FirstDownloadedSong;
            }
            catch (Exception ex)
            {
                dialogService.ShowError($"Failed to load settings:\n{ex.Message}");
            }
        }

        public string? PlaylistUrl
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        public string? FirstDownloadedSong
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        public double Progress
        {
            get;
            set => SetProperty(ref field, value);
        }

        public bool IsDownloading
        {
            get;
            set
            {
                if (!SetProperty(ref field, value)) return;
                ((RelayCommand)DownloadCommand).RaiseCanExecuteChanged();
                ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
            }
        }

        public ICommand DownloadCommand { get; }
        public ICommand CancelCommand { get; }

        private async Task RunDownloadPlaylistAsync()
        {
            try
            {
                await DownloadPlaylistAsync();
            }
            catch (Exception ex)
            {
                dialogService.ShowError($"An error occurred:\n{ex.Message}");
            }
        }

        private async Task DownloadPlaylistAsync()
        {
            var dialogResult = await dialogService.ShowDownloadPlaylistDialogAsync();
            if (!dialogResult.Result) return;

            string playlistUrlInput = dialogResult.PlaylistUrl;
            int numberOfSongs = dialogResult.NumberOfSongs;

            string downloadFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Playlist");
            Directory.CreateDirectory(downloadFolder);

            IsDownloading = true;
            Progress = 0;
            cancellationTokenSource = new CancellationTokenSource();

            try
            {
                // Progress reporter is marshaled to the UI thread automatically.
                var progressReporter = new Progress<(int downloaded, int total, string currentSong)>(report =>
                {
                    if (report.total > 0)
                        Progress = Math.Min(100, (report.downloaded * 100.0) / report.total);
                });

                DownloadResult result = await downloadService.DownloadPlaylistAsync(
                    playlistUrlInput,
                    numberOfSongs,
                    downloadFolder,
                    progressReporter,
                    cancellationTokenSource.Token);

                settings.FirstDownloadedSong = result.FirstSongName;
                settings.PlaylistLink = playlistUrlInput;
                await settingsService.SaveSettingsAsync(settings);

                PlaylistUrl = settings.PlaylistLink;
                FirstDownloadedSong = settings.FirstDownloadedSong;

                if (result.HadErrors)
                {
                    dialogService.ShowError(
                        "Some videos failed to download.\n\n" +
                        $"Requested: {result.TotalSongs}\n" +
                        $"Downloaded: {result.DownloadedSongs}\n" +
                        (string.IsNullOrEmpty(result.FirstSongName) ? "" : $"First song: {result.FirstSongName}\n") +
                        $"\nyt-dlp error output:\n{result.ErrorDetail}",
                        "Download Error");
                }
                else
                {
                    dialogService.ShowInfo(
                        $"Playlist downloaded successfully!\nFirst song: {result.FirstSongName}",
                        "Success");
                }
            }
            catch (OperationCanceledException)
            {
                dialogService.ShowWarning("Download was canceled.", "Canceled");
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
