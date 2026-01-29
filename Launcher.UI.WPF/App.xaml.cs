using System.Configuration;
using System.Data;
using System.Windows;
using Launcher.UI.WPF.Resources.Overlay;
using Launcher.UI.WPF.ViewModels;
using Launcher.UI.WPF.Services;
using Launcher.Core.Services.IO;
using Launcher.Core.Services.Auth;

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
        var authService = new AuthService();
        var accountStorageService = new AccountStorageService();

        var mainViewModel = new MainViewModel(themeService, authService, accountStorageService);
        
        var mainWindow = new MainWindow();
        
        mainWindow.DataContext = mainViewModel;
        
        mainWindow.Show();
    }
}