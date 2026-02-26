using Microsoft.Extensions.DependencyInjection;
using UtilityAppV2.ViewModels;

namespace UtilityAppV2.Views.FixFileNamesView;

public partial class FixFileNamesView
{
    public FixFileNamesView()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<FixFileNamesViewModel>();
    }
}
