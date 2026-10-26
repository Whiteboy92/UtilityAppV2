using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using UtilityAppV2.Services.Implementation;
using UtilityAppV2.Services.Interfaces;
using UtilityAppV2.ViewModels;
using UtilityAppV2.Views.ConvertToPngView;
using UtilityAppV2.Views.DownloadPlaylistView;
using UtilityAppV2.Views.FixFileNamesView;
using UtilityAppV2.Views.MoveFilesView;
using UtilityAppV2.Views.OptimizeAudioView;

namespace UtilityAppV2;

public partial class App
{
    private ServiceProvider? serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        serviceProvider = serviceCollection.BuildServiceProvider();

        var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        // Fetch/refresh yt-dlp in the background so the first playlist download
        // doesn't stall on a download and the binary stays current. Fire-and-forget:
        // EnsureAndUpdateAsync swallows offline/transient failures.
        var ytDlp = serviceProvider.GetRequiredService<IYtDlpProvider>();
        _ = Task.Run(() => ytDlp.EnsureAndUpdateAsync());
    }

    protected override void OnExit(ExitEventArgs e)
    {
        serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IFileConversionService, FileConversionService>();
        services.AddSingleton<IYtDlpProvider, YtDlpProvider>();
        services.AddSingleton<IDownloadService, DownloadService>();
        services.AddSingleton<IFileFixerService, FileFixerService>();
        services.AddSingleton<IRegexFileNameCleaner, RegexFileNameCleaner>();
        services.AddSingleton<IMoveFilesService, MoveFilesService>();
        services.AddSingleton<IArtistRepository, ArtistRepository>();
        services.AddSingleton<IAudioOptimizerService, AudioOptimizerService>();

        // ViewModels
        services.AddTransient<ConvertToPngViewModel>();
        services.AddTransient<DownloadPlaylistViewModel>();
        services.AddTransient<MoveFilesViewModel>();
        services.AddTransient<FixFileNamesViewModel>();
        services.AddTransient<OptimizeAudioViewModel>();

        // Views
        services.AddTransient<MainWindow>();
        services.AddTransient<ConvertToPngView>();
        services.AddTransient<DownloadPlaylistView>();
        services.AddTransient<FixFileNamesView>();
        services.AddTransient<MoveFilesView>();
        services.AddTransient<OptimizeAudioView>();
    }
}
