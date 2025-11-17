using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Launcher.UI.WPF.Resources.Controls
{
    public partial class CustomTitleBar : UserControl
    {
        public CustomTitleBar()
        {
            InitializeComponent();
        }

        // Вспомогательный метод, чтобы найти родительское Окно
        private Window GetParentWindow()
        {
            return Window.GetWindow(this);
        }

        // Перетаскивание окна
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                GetParentWindow()?.DragMove();
            }
        }

        // Кнопка "Свернуть"
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            var window = GetParentWindow();
            if (window != null)
            {
                window.WindowState = WindowState.Minimized;
            }
        }

        // Кнопка "Развернуть/Восстановить"
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

        // Кнопка "Закрыть"
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            GetParentWindow()?.Close();
        }
    }
}