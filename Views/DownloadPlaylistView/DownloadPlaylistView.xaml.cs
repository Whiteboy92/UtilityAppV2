using Microsoft.Extensions.DependencyInjection;
using UtilityAppV2.ViewModels;

namespace UtilityAppV2.Views.DownloadPlaylistView;

public partial class DownloadPlaylistView
{
    public DownloadPlaylistView()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<DownloadPlaylistViewModel>();
    }
}