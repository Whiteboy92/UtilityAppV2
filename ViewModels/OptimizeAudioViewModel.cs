using System.Windows.Input;
using UtilityAppV2.Commands;
using UtilityAppV2.Core;
using UtilityAppV2.Models;
using UtilityAppV2.Services.Interfaces;

namespace UtilityAppV2.ViewModels;

public class OptimizeAudioViewModel : BaseViewModel
{
    private readonly IAudioOptimizerService audioOptimizerService;
    private readonly IDialogService dialogService;
    private CancellationTokenSource? cancellationTokenSource;

    public OptimizeAudioViewModel(IAudioOptimizerService audioOptimizerService, IDialogService dialogService)
    {
        this.audioOptimizerService = audioOptimizerService ?? throw new ArgumentNullException(nameof(audioOptimizerService));
        this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        OptimizeCommand = new RelayCommand(_ => _ = RunAsyncSafe(), _ => !IsProcessing);
        CancelCommand = new RelayCommand(_ => Cancel(), _ => IsProcessing);
    }

    public double Progress
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsProcessing
    {
        get;
        set
        {
            if (!SetProperty(ref field, value)) return;
            ((RelayCommand)OptimizeCommand).RaiseCanExecuteChanged();
            ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand OptimizeCommand { get; }
    public ICommand CancelCommand { get; }

    private async Task RunAsyncSafe()
    {
        try
        {
            await RunAsync();
        }
        catch (Exception ex)
        {
            dialogService.ShowError($"Unexpected error: {ex.Message}");
        }
    }

    private async Task RunAsync()
    {
        IsProcessing = true;
        Progress = 0;
        cancellationTokenSource = new CancellationTokenSource();

        try
        {
            var progressReporter = new Progress<double>(p => Progress = p);

            AudioOptimizeResult result = await audioOptimizerService.OptimizeFolderAsync(
                FilePathConstants.PlaylistFolder, progressReporter, cancellationTokenSource.Token);

            double mbSaved = result.BytesSaved / (1024.0 * 1024.0);

            dialogService.ShowInfo(
                $"Files scanned: {result.TotalFiles}\n" +
                $"Optimized: {result.OptimizedFiles}\n" +
                $"Saved: {mbSaved:F1} MB\n" +
                $"Duration: {result.Duration.TotalSeconds:F1}s",
                "Audio optimizer");
        }
        catch (OperationCanceledException)
        {
            dialogService.ShowWarning("Operation canceled.", "Canceled");
        }
        catch (Exception ex)
        {
            dialogService.ShowError($"Error while optimizing audio: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
            Progress = 0;
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }
    }

    private void Cancel()
    {
        cancellationTokenSource?.Cancel();
    }
}
