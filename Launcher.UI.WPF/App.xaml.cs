using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Launcher.Core.Services;
using Launcher.UI.WPF.Resources.Overlay;
using Launcher.UI.WPF.ViewModels;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Stores;
using Launcher.Core.Services.IO;
using Launcher.Core.Services.Auth;
using Launcher.Core.Services.Game;
using Launcher.Core.Services.Integrations;
using Launcher.Core.Services.System;
using Launcher.UI.WPF.ViewModels.Accounts;
using Launcher.UI.WPF.ViewModels.Game;
using Launcher.UI.WPF.ViewModels.Instances;
using Launcher.UI.WPF.ViewModels.Settings;

namespace Launcher.UI.WPF;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        
        services.AddSingleton<ILauncherPathsService, LauncherPathsService>();
        services.AddSingleton<LocalizationService>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IAccountStorageService, AccountStorageService>();
        services.AddSingleton<IGameVersionService, GameVersionService>();
        services.AddSingleton<IInstanceService, InstanceService>();
        services.AddSingleton<IInstanceFileSystemService, InstanceFileSystemService>();
        services.AddSingleton<IModrinthService, ModrinthService>();
        services.AddSingleton<ISysInfoService, SysInfoService>();
        services.AddSingleton<IDiscordService, DiscordService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDragDropParserService, DragDropParserService>();
        services.AddSingleton<ISymlinkService, SymlinkService>();
        services.AddSingleton<ImportOrchestratorService>();
        services.AddSingleton<IOverlayService, OverlayService>();
        services.AddSingleton<NavigationService>();
        
        services.AddSingleton<ILaunchService>(provider => new LaunchService(
            provider.GetRequiredService<IInstanceFileSystemService>(),
            provider.GetRequiredService<IModrinthService>(),
            NotificationService.Instance, 
            LocalizationService.Instance
        ));
        
        services.AddSingleton<AppStore>();
        services.AddSingleton<LoginStore>();
        services.AddSingleton<SettingsStore>();
        services.AddSingleton<LaunchStore>();
        services.AddSingleton<InstancesStore>();

        
        
        services.AddTransient<PlayViewModel>();
        services.AddTransient<InstallationsViewModel>();
        services.AddTransient<SkinsViewModel>();
        services.AddTransient<LoginVM>();
        services.AddTransient<SettingsVM>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();
        
        Services = services.BuildServiceProvider();
        
        LocalizationService.Instance = Services.GetRequiredService<LocalizationService>();
        NotificationService.Instance = Services.GetRequiredService<NotificationService>();
        
        var mainViewModel = Services.GetRequiredService<MainViewModel>();
        await mainViewModel.InitializeAsync();

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.DataContext = mainViewModel;
        
        mainWindow.Show();
    }
}