using UtilityAppV2.ViewModels;

namespace UtilityAppV2.Views.ConvertToPngView;

public partial class ConvertToPngView
{
    public ConvertToPngView(ConvertToPngViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
