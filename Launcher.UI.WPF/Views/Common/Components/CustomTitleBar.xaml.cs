using System.Windows;
using System.Windows.Input;

namespace Launcher.UI.WPF.Views.Common.Components
{
    public partial class CustomTitleBar
    {
        private Window? _parentWindow;
        
        public CustomTitleBar()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _parentWindow = GetParentWindow();
            if (_parentWindow != null)
            {
                _parentWindow.StateChanged += OnWindowStateChanged;
                UpdateMaximizeIcon();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_parentWindow != null)
            {
                _parentWindow.StateChanged -= OnWindowStateChanged;
                _parentWindow = null;
            }
        }

        private void OnWindowStateChanged(object? sender, EventArgs e)
        {
            UpdateMaximizeIcon();
        }

        private void UpdateMaximizeIcon()
        {
            if (MaximizeButton == null || _parentWindow == null) return;
            // E922 = ChromeMaximize (single square), E923 = ChromeRestore (overlapping squares)
            MaximizeButton.Content = _parentWindow.WindowState == WindowState.Maximized
                ? "\uE923"
                : "\uE922";
        }
        
        private Window? GetParentWindow()
        {
            return Window.GetWindow(this);
        }
        
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var window = GetParentWindow();
            if (window == null) return;
            
            if (e.ClickCount == 2)
            {
                window.WindowState = window.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
            else if (e.ButtonState == MouseButtonState.Pressed)
            {
                window.DragMove();
            }
        }
        
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            var window = GetParentWindow();
            if (window != null)
            {
                window.WindowState = WindowState.Minimized;
            }
        }
        
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            var window = GetParentWindow();
            if (window != null)
            {
                window.WindowState = window.WindowState == WindowState.Maximized 
                    ? WindowState.Normal 
                    : WindowState.Maximized;
            }
        }
        
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            GetParentWindow()?.Close();
        }
    }
}