using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using UtilityAppV2.Services.Implementation;
using UtilityAppV2.Services.Interfaces;
using UtilityAppV2.ViewModels;
using UtilityAppV2.Views.ConvertToPngView;
using UtilityAppV2.Views.DownloadPlaylistView;
using UtilityAppV2.Views.FixFileNamesView;
using UtilityAppV2.Views.MoveFilesView;

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
        services.AddSingleton<IDownloadService, DownloadService>();
        services.AddSingleton<IFileFixerService, FileFixerService>();
        services.AddSingleton<IRegexFileNameCleaner, RegexFileNameCleaner>();
        services.AddSingleton<IMoveFilesService, MoveFilesService>();
        services.AddSingleton<IArtistRepository, ArtistRepository>();

        // ViewModels
        services.AddTransient<ConvertToPngViewModel>();
        services.AddTransient<DownloadPlaylistViewModel>();
        services.AddTransient<MoveFilesViewModel>();
        services.AddTransient<FixFileNamesViewModel>();

        // Views
        services.AddTransient<MainWindow>();
        services.AddTransient<ConvertToPngView>();
        services.AddTransient<DownloadPlaylistView>();
        services.AddTransient<FixFileNamesView>();
        services.AddTransient<MoveFilesView>();
    }
}
