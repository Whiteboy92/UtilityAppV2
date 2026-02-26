using System.Windows;
using System.Windows.Input;
using UtilityAppV2.Commands;

namespace UtilityAppV2.UI_Components
{
    public static class WindowCommands
    {
        private static Window? GetWindow(object? parameter)
        {
            if (parameter is Window window)
                return window;

            return Application.Current.MainWindow;
        }

        public static readonly ICommand MaximizeRestore = new RelayCommand(parameter =>
        {
            var window = GetWindow(parameter);
            window?.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }, parameter => GetWindow(parameter) != null);
    }
}