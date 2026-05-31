using System.Windows.Input;
using UtilityAppV2.Commands;
using UtilityAppV2.Core;
using UtilityAppV2.Models;
using UtilityAppV2.Services.Interfaces;

namespace UtilityAppV2.ViewModels;

public class FixFileNamesViewModel : BaseViewModel
{
    private readonly IFileFixerService fileFixerService;
    private readonly IDialogService dialogService;
    private CancellationTokenSource? cancellationTokenSource;

    public FixFileNamesViewModel(IFileFixerService fileFixerService, IDialogService dialogService)
    {
        this.fileFixerService = fileFixerService ?? throw new ArgumentNullException(nameof(fileFixerService));
        this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        FixCommand = new RelayCommand(_ => _ = RunAsyncSafe(), _ => !IsProcessing);
        CancelCommand = new RelayCommand(_ => Cancel(), _ => IsProcessing);
    }

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
            ((RelayCommand)FixCommand).RaiseCanExecuteChanged();
            ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand FixCommand { get; }
    public ICommand CancelCommand { get; }

    private async Task RunAsync()
    {
        IsProcessing = true;
        Progress = 0;
        cancellationTokenSource = new CancellationTokenSource();

        try
        {
            var progressReporter = new Progress<double>(p => Progress = p);

            FileFixResult result = await fileFixerService.FixFilesAsync(progressReporter, cancellationTokenSource.Token);

            dialogService.ShowInfo(
                $"Files processed: {result.ProcessedFiles}/{result.TotalFiles}\n" +
                $"Renamed: {result.RenamedFiles}\n" +
                $"Duration: {result.Duration.TotalSeconds:F1}s",
                "File fixer");
        }
        catch (OperationCanceledException)
        {
            dialogService.ShowWarning("Operation canceled.", "Canceled");
        }
        catch (Exception ex)
        {
            dialogService.ShowError($"Error while fixing files: {ex.Message}");
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
