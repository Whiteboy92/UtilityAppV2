using System.Windows;
using System.Windows.Input;
using UtilityAppV2.Commands;
using UtilityAppV2.Core;
using UtilityAppV2.Models;
using UtilityAppV2.Services.Interfaces;

namespace UtilityAppV2.ViewModels
{
    public class MoveFilesViewModel : BaseViewModel
    {
        private readonly IMoveFilesService moveFilesService;

        private CancellationTokenSource? cts;

        public MoveFilesViewModel(IMoveFilesService moveFilesService)
        {
            this.moveFilesService = moveFilesService ?? throw new ArgumentNullException(nameof(moveFilesService));

            MoveCommand = new RelayCommand(_ => RunMoveFilesAsync(), _ => !IsMoving);
            CancelCommand = new RelayCommand(_ => CancelMove(), _ => IsMoving);
        }

        // ====================
        // Properties
        // ====================

        public double Progress
        {
            get { return field; }
            set
            {
                field = value;
                OnPropertyChanged();
            }
        }

        public bool IsMoving
        {
            get { return field; }
            set
            {
                field = value;
                OnPropertyChanged();
                ((RelayCommand)MoveCommand).RaiseCanExecuteChanged();
                ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
            }
        }

        // ====================
        // Commands
        // ====================

        public ICommand MoveCommand { get; }
        public ICommand CancelCommand { get; }

        // ====================
        // Methods
        // ====================

        private async void RunMoveFilesAsync()
        {
            try
            {
                await MoveFilesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An error occurred while moving files:\n" + ex.Message,
                    "Move Files Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task MoveFilesAsync()
        {
            IsMoving = true;
            Progress = 0;
            cts = new CancellationTokenSource();

            try
            {
                var progressReporter = new Progress<double>(value =>
                {
                    Progress = Math.Max(0, Math.Min(100, value));
                });

                FileMoveResult result = await moveFilesService.MoveFilesAsync(
                    progressReporter,
                    cts.Token);

                if (result.MovedFiles > 0)
                {
                    MessageBox.Show(
                        "Files moved successfully!\n" +
                        "From: " + FilePathConstants.PlaylistFolder + "\n" +
                        "To: " + FilePathConstants.MusicDbFolder + "\n\n" +
                        "Total files: " + result.TotalFiles + "\n" +
                        "Moved files: " + result.MovedFiles,
                        "Move Files",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show(
                    "Move operation was canceled.",
                    "Canceled",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                IsMoving = false;
                Progress = 0;

                if (cts != null)
                {
                    cts.Dispose();
                    cts = null;
                }
            }
        }

        private void CancelMove()
        {
            if (cts is { IsCancellationRequested: false })
            {
                cts.Cancel();
            }
        }
    }
}
