using System.Windows;
using System.Windows.Input;

namespace Launcher.Updater.Resources.Components;

public partial class CustomTitleBar
{
    public CustomTitleBar()
    {
        InitializeComponent();
    }

    private Window? GetParentWindow() => Window.GetWindow(this);

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            GetParentWindow()?.DragMove();
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

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        GetParentWindow()?.Close();
    }
}
