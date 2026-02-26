// DownloadPlaylistInputDialog.xaml.cs
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using UtilityAppV2.ViewModels;

namespace UtilityAppV2.UI_Components.Download
{
    public partial class DownloadPlaylistInputDialog
    {
        public DownloadPlaylistInputDialogViewModel ViewModel { get; }

        public DownloadPlaylistInputDialog(DownloadPlaylistInputDialogViewModel vm)
        {
            InitializeComponent();

            ViewModel = vm ?? throw new ArgumentNullException(nameof(vm));
            DataContext = ViewModel;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            BindingExpression? be1 = NumberOfSongsTb.GetBindingExpression(TextBox.TextProperty);
            be1?.UpdateSource();

            BindingExpression? be2 = PlayListUrlTb.GetBindingExpression(TextBox.TextProperty);
            be2?.UpdateSource();

            if (string.IsNullOrWhiteSpace(ViewModel.PlaylistUrl))
            {
                MessageBox.Show("Please enter a playlist URL.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (ViewModel.NumberOfSongs <= 0)
            {
                MessageBox.Show("Please enter a valid number of songs.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}