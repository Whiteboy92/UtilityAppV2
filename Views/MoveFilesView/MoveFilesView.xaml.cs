using Microsoft.Extensions.DependencyInjection;
using UtilityAppV2.ViewModels;

namespace UtilityAppV2.Views.MoveFilesView;

public partial class MoveFilesView
{
    public MoveFilesView()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<MoveFilesViewModel>();
    }
}