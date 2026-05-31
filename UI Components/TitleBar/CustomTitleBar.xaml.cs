using System.Windows;
using System.Windows.Input;

namespace UtilityAppV2.UI_Components.TitleBar
{
    public partial class CustomTitleBar
    {
        public static readonly DependencyProperty WindowTitleProperty =
            DependencyProperty.Register(nameof(WindowTitle), typeof(string), typeof(CustomTitleBar), new PropertyMetadata(string.Empty));

        public string WindowTitle
        {
            get => (string)GetValue(WindowTitleProperty);
            set => SetValue(WindowTitleProperty, value);
        }

        public CustomTitleBar()
        {
            InitializeComponent();
            MouseLeftButtonDown += OnTitleBarMouseLeftButtonDown;
        }

        private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window is null) return;

            if (e.ClickCount == 2)
            {
                ToggleMaximize(window);
                e.Handled = true;
                return;
            }

            // DragMove must only be called while the primary button is pressed,
            // otherwise it throws — guard against the stray double-click case.
            if (e.ButtonState == MouseButtonState.Pressed)
                window.DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e) =>
            Window.GetWindow(this)?.Close();

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window is not null)
                window.WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window is not null)
                ToggleMaximize(window);
        }

        private static void ToggleMaximize(Window window) =>
            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
    }
}
