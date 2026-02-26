using Microsoft.Extensions.DependencyInjection;
using UtilityAppV2.ViewModels;

namespace UtilityAppV2.Views.ConvertToPngView;

public partial class ConvertToPngView
{
    public ConvertToPngView()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<ConvertToPngViewModel>();
    }
}