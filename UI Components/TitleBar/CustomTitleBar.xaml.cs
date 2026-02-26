using System.Windows;

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

            MouseLeftButtonDown += (_, e) =>
            {
                var window = Window.GetWindow(this);
                if (window == null) return;

                if (e.ClickCount == 2)
                    window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

                window.DragMove();
                e.Handled = true;
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            window?.WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);

            window?.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
    }
}