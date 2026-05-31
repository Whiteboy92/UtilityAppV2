using UtilityAppV2.Views.ConvertToPngView;
using UtilityAppV2.Views.DownloadPlaylistView;
using UtilityAppV2.Views.FixFileNamesView;
using UtilityAppV2.Views.MoveFilesView;
using UtilityAppV2.Views.OptimizeAudioView;

namespace UtilityAppV2;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow(
        ConvertToPngView convertView,
        DownloadPlaylistView downloadView,
        FixFileNamesView fixView,
        MoveFilesView moveView,
        OptimizeAudioView optimizeView)
    {
        InitializeComponent();

        ConvertHost.Content = convertView;
        DownloadHost.Content = downloadView;
        FixHost.Content = fixView;
        MoveHost.Content = moveView;
        OptimizeHost.Content = optimizeView;
    }
}
