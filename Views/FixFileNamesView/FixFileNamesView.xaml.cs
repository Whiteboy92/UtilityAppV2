using UtilityAppV2.ViewModels;

namespace UtilityAppV2.Views.FixFileNamesView;

public partial class FixFileNamesView
{
    public FixFileNamesView(FixFileNamesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
