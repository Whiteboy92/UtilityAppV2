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
        private readonly IDialogService dialogService;

        private CancellationTokenSource? cts;

        public MoveFilesViewModel(IMoveFilesService moveFilesService, IDialogService dialogService)
        {
            this.moveFilesService = moveFilesService ?? throw new ArgumentNullException(nameof(moveFilesService));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            MoveCommand = new RelayCommand(_ => _ = RunMoveFilesAsync(), _ => !IsMoving);
            CancelCommand = new RelayCommand(_ => CancelMove(), _ => IsMoving);
        }

        public double Progress
        {
            get;
            set => SetProperty(ref field, value);
        }

        public bool IsMoving
        {
            get;
            set
            {
                if (!SetProperty(ref field, value)) return;
                ((RelayCommand)MoveCommand).RaiseCanExecuteChanged();
                ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
            }
        }

        public ICommand MoveCommand { get; }
        public ICommand CancelCommand { get; }

        private async Task RunMoveFilesAsync()
        {
            try
            {
                await MoveFilesAsync();
            }
            catch (Exception ex)
            {
                dialogService.ShowError(
                    "An error occurred while moving files:\n" + ex.Message,
                    "Move Files Error");
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

                FileMoveResult result = await moveFilesService.MoveFilesAsync(progressReporter, cts.Token);

                if (result.Aborted)
                {
                    dialogService.ShowError(result.ErrorMessage!, "Move Files");
                    return;
                }

                if (result.TagErrors.Count > 0)
                {
                    dialogService.ShowWarning(
                        "Some files could not be tagged:\n\n" +
                        string.Join(Environment.NewLine, result.TagErrors.Take(10)),
                        "Tag Update Warning");
                }

                if (result.MovedFiles > 0)
                {
                    dialogService.ShowInfo(
                        "Files moved successfully!\n" +
                        "From: " + FilePathConstants.PlaylistFolder + "\n" +
                        "To: " + FilePathConstants.MusicDbFolder + "\n\n" +
                        "Total files: " + result.TotalFiles + "\n" +
                        "Moved files: " + result.MovedFiles,
                        "Move Files");
                }
            }
            catch (OperationCanceledException)
            {
                dialogService.ShowWarning("Move operation was canceled.", "Canceled");
            }
            finally
            {
                IsMoving = false;
                Progress = 0;
                cts?.Dispose();
                cts = null;
            }
        }

        private void CancelMove()
        {
            if (cts is { IsCancellationRequested: false })
                cts.Cancel();
        }
    }
}
