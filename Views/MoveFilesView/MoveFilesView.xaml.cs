using UtilityAppV2.ViewModels;

namespace UtilityAppV2.Views.MoveFilesView;

public partial class MoveFilesView
{
    public MoveFilesView(MoveFilesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
