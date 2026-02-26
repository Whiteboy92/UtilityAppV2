using System.Windows;
using System.Windows.Input;
using Ookii.Dialogs.Wpf;
using UtilityAppV2.Commands;
using UtilityAppV2.Core;
using UtilityAppV2.Models;
using UtilityAppV2.Services.Interfaces;

namespace UtilityAppV2.ViewModels;

public class ConvertToPngViewModel : BaseViewModel
{
    private readonly IFileConversionService fileConversionService;
    private CancellationTokenSource? cancellationTokenSource;

    public double Progress
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    private bool IsProcessing
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            ((RelayCommand)ConvertCommand).RaiseCanExecuteChanged();
            ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand ConvertCommand { get; }
    public ICommand CancelCommand { get; }

    public ConvertToPngViewModel(IFileConversionService fileConversionService)
    {
        this.fileConversionService = fileConversionService ?? throw new ArgumentNullException(nameof(fileConversionService));

        ConvertCommand = new RelayCommand(async void (_) =>
        {
            try
            {
                await ConvertFilesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An unexpected error occurred:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }, _ => !IsProcessing);

        CancelCommand = new RelayCommand(_ => CancelConversion(), _ => IsProcessing);
    }

    private async Task ConvertFilesAsync()
    {
        try
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog() != true)
                return;

            string folder = dialog.SelectedPath;

            IsProcessing = true;
            Progress = 0;

            cancellationTokenSource = new CancellationTokenSource();
            var progressReporter = new Progress<double>(p => Progress = p);

            ConversionResult result = await fileConversionService.ConvertFilesToPngAsync(
                folder,
                progressReporter,
                cancellationTokenSource.Token
            );

            MessageBox.Show(
                $"Conversion completed successfully!\n\n" +
                $"Total files: {result.TotalFiles}\n" +
                $"Converted: {result.ConvertedFiles}\n" +
                $"Duration: {result.Duration.TotalSeconds:F2} seconds",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show("Conversion was canceled.", "Canceled", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during conversion:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsProcessing = false;
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }
    }

    private void CancelConversion()
    {
        cancellationTokenSource?.Cancel();
    }
}
