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
    public static ServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure services
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();

        // Show the main window
        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
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