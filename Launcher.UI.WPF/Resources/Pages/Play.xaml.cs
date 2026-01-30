using System.Windows;
using System.Windows.Controls;

namespace Launcher.UI.WPF.Resources.Pages;

public partial class Play : Page
{
    public Play()
    {
        InitializeComponent();
        // В ТУПУЮ БЕРЕМ КОНТЕКСТ ГЛАВНОГО ОКНА
        // Это костыль, но для теста кнопки - идеально.
        if (Application.Current.MainWindow != null)
        {
            this.DataContext = Application.Current.MainWindow.DataContext;
        }
    }
}