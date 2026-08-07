using System.Windows.Input;
using UtilityAppV2.Commands;
using UtilityAppV2.Core;
using UtilityAppV2.Models;
using UtilityAppV2.Services.Interfaces;

namespace UtilityAppV2.ViewModels;

public class ConvertToPngViewModel : BaseViewModel
{
    private readonly IFileConversionService fileConversionService;
    private readonly IDialogService dialogService;
    private CancellationTokenSource? cancellationTokenSource;

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
            ((RelayCommand)ConvertCommand).RaiseCanExecuteChanged();
            ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand ConvertCommand { get; }
    public ICommand CancelCommand { get; }

    public ConvertToPngViewModel(IFileConversionService fileConversionService, IDialogService dialogService)
    {
        this.fileConversionService = fileConversionService ?? throw new ArgumentNullException(nameof(fileConversionService));
        this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        ConvertCommand = new RelayCommand(_ => _ = ConvertFilesAsync(), _ => !IsProcessing);
        CancelCommand = new RelayCommand(_ => CancelConversion(), _ => IsProcessing);
    }

    private async Task ConvertFilesAsync()
    {
        string? folder = dialogService.PickFolder("Select folder to convert");
        if (folder is null)
            return;

        IsProcessing = true;
        Progress = 0;
        cancellationTokenSource = new CancellationTokenSource();

        try
        {
            var progressReporter = new Progress<double>(p => Progress = p);

            ConversionResult result = await fileConversionService.ConvertFilesToPngAsync(
                folder,
                progressReporter,
                cancellationTokenSource.Token);

            dialogService.ShowInfo(
                "Conversion completed successfully!\n\n" +
                $"Total files: {result.TotalFiles}\n" +
                $"Converted: {result.ConvertedFiles}\n" +
                $"Duration: {result.Duration.TotalSeconds:F2} seconds",
                "Success");

            if (result.FailedFiles.Count > 0)
            {
                dialogService.ShowWarning(
                    $"{result.FailedFiles.Count} file(s) could not be converted:\n\n" +
                    string.Join(Environment.NewLine, result.FailedFiles.Take(10)),
                    "Conversion Warning");
            }
        }
        catch (OperationCanceledException)
        {
            dialogService.ShowWarning("Conversion was canceled.", "Canceled");
        }
        catch (Exception ex)
        {
            dialogService.ShowError($"Error during conversion:\n{ex.Message}");
        }
        finally
        {
            IsProcessing = false;
            Progress = 0;
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }
    }

    private void CancelConversion()
    {
        cancellationTokenSource?.Cancel();
    }
}
