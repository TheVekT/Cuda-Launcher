using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Launcher.UI.WPF.Views.Common.Components
{
    public partial class CustomTitleBar : UserControl
    {
        public CustomTitleBar()
        {
            InitializeComponent();
        }

        // ��������������� �����, ����� ����� ������������ ����
        private Window GetParentWindow()
        {
            return Window.GetWindow(this);
        }

        // �������������� ����
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                GetParentWindow()?.DragMove();
            }
        }

        // ������ "��������"
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            var window = GetParentWindow();
            if (window != null)
            {
                window.WindowState = WindowState.Minimized;
            }
        }

        // ������ "����������/������������"
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

        // ������ "�������"
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            GetParentWindow()?.Close();
        }
    }
}