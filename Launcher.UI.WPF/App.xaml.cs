using System.Configuration;
using System.Data;
using System.Windows;
using Launcher.Core.Services;
using Launcher.UI.WPF.Resources.Overlay;
using Launcher.UI.WPF.ViewModels;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Stores;
using Launcher.Core.Services.IO;
using Launcher.Core.Services.Auth;
using Launcher.Core.Services.Game;
using Launcher.Core.Services.System;

namespace Launcher.UI.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        
        
        //Services
        var themeService = new ThemeService();
        var authService = new AuthService();
        var accountStorageService = new AccountStorageService();
        var versionService = new GameVersionService();
        var instanceService = new InstanceService();
        var instanceFileSystemService = new InstanceFileSystemService();
        var modrinthService = new ModrinthService();
        var launchService = new LaunchService(instanceFileSystemService, modrinthService);
        var sysInfoService = new SysInfoService();
        var discordService = new DiscordService();
        //Stores
        var loginStore = new LoginStore(accountStorageService);
        var settingsStore = new SettingsStore(themeService, sysInfoService);
        var launchStore = new LaunchStore(launchService);
        var instancesStore = new InstancesStore(instanceFileSystemService, instanceService);
        var appStore = new AppStore();
        
        var availableThemes = themeService.ReloadThemes();

        string themeToLoad = settingsStore.CurrentThemePath;

        if (string.IsNullOrEmpty(themeToLoad) || !System.IO.File.Exists(themeToLoad))
        {
            // Если настройки нет, берем первую найденную
            if (availableThemes.Count > 0)
            {
                themeToLoad = availableThemes[0].ZipPath;
                appStore.CurrentBannerPath = availableThemes[0].BannerPath;
                settingsStore.CurrentThemePath = themeToLoad; 
            }
        }
        else
        {
            // Если настройка есть, просто применяем
            themeService.ChangeTheme(themeToLoad);
        }
        
        //MainViewModel
        var mainViewModel = new MainViewModel(
            themeService, 
            authService, 
            accountStorageService, 
            versionService, 
            instanceService, 
            instanceFileSystemService, 
            launchService,
            sysInfoService,
            discordService,
            loginStore,
            settingsStore,
            launchStore,
            instancesStore,
            appStore);
        await mainViewModel.InitializeAsync();
        
        var mainWindow = new MainWindow();
        
        mainWindow.DataContext = mainViewModel;
        
        mainWindow.Show();
    }
}