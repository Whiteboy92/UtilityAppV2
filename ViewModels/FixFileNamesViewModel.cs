using System.Windows;
using System.Windows.Input;
using UtilityAppV2.Commands;
using UtilityAppV2.Core;
using UtilityAppV2.Models;
using UtilityAppV2.Services.Interfaces;

namespace UtilityAppV2.ViewModels;

public class FixFileNamesViewModel : BaseViewModel
{
    private readonly IFileFixerService fileFixerService;
    private CancellationTokenSource? cancellationTokenSource;

    public FixFileNamesViewModel(IFileFixerService fileFixerService)
    {
        this.fileFixerService = fileFixerService ?? throw new ArgumentNullException(nameof(fileFixerService));

        FixCommand = new RelayCommand(runAsyncSafe =>
        {
            _ = RunAsyncSafe();
        }, _ => !IsProcessing);

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
            MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public double Progress
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public bool IsProcessing
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            ((RelayCommand)FixCommand).RaiseCanExecuteChanged();
            ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand FixCommand { get; }
    public ICommand CancelCommand { get; }

    private async Task RunAsync()
    {
        try
        {
            IsProcessing = true;
            Progress = 0;
            cancellationTokenSource = new CancellationTokenSource();
            var progressReporter = new Progress<double>(p => Progress = p);

            FileFixResult result = await fileFixerService.FixFilesAsync(progressReporter, cancellationTokenSource.Token);

            MessageBox.Show(
                $"Files processed: {result.ProcessedFiles}/{result.TotalFiles}\nRenamed: {result.RenamedFiles}\nDuration: {result.Duration.TotalSeconds:F1}s",
                "File fixer",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show("Operation canceled.", "Canceled", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error while fixing files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsProcessing = false;
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
            Progress = 0;
        }
    }

    private void Cancel()
    {
        cancellationTokenSource?.Cancel();
    }
}
