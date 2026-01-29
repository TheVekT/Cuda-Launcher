using System.Configuration;
using System.Data;
using System.Windows;
using Launcher.UI.WPF.Resources.Overlay;
using Launcher.UI.WPF.ViewModels;
using Launcher.UI.WPF.Services;
using Launcher.Core.Services.IO;
using Launcher.Core.Services.Auth;
using Launcher.Core.Services.Game;

namespace Launcher.UI.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var themeService = new ThemeService();
        var authService = new AuthService();
        var accountStorageService = new AccountStorageService();
        var versionService = new GameVersionService();
        var instanceService = new InstanceService();

        var mainViewModel = new MainViewModel(themeService, authService, accountStorageService, versionService, instanceService);
        await mainViewModel.InitializeAsync();
        
        var mainWindow = new MainWindow();
        
        mainWindow.DataContext = mainViewModel;
        
        mainWindow.Show();
    }
}