using UtilityAppV2.ViewModels;

namespace UtilityAppV2.Views.OptimizeAudioView;

public partial class OptimizeAudioView
{
    public OptimizeAudioView(OptimizeAudioViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
