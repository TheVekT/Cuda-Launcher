using System.Configuration;
using System.Data;
using System.Windows;
using Launcher.UI.WPF.Resources.Overlay;
using Launcher.UI.WPF.ViewModels;
using Launcher.UI.WPF.Services;

namespace Launcher.UI.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var themeService = new ThemeService();
        var authService = new Launcher.Core.Services.Auth.AuthService();

        var mainViewModel = new MainViewModel(themeService, authService);
        
        var mainWindow = new MainWindow();
        
        mainWindow.DataContext = mainViewModel;
        
        mainWindow.Show();
    }
}