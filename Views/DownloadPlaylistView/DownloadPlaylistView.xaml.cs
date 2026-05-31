using UtilityAppV2.ViewModels;

namespace UtilityAppV2.Views.DownloadPlaylistView;

public partial class DownloadPlaylistView
{
    public DownloadPlaylistView(DownloadPlaylistViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += async (_, _) => await viewModel.InitializeAsync();
    }
}
