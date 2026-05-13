using System.Net.Http;
using System.Windows;
using Launcher.Core.Assets;
using Launcher.Core.Config;
using Launcher.Core.Game;
using Launcher.Core.Identity;
using Launcher.Core.Instances;
using Launcher.Core.Integrations;
using Launcher.Core.Mods;
using Microsoft.Extensions.DependencyInjection;
using Launcher.UI.WPF.ViewModels;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Stores;
using Launcher.Core.System;
using Launcher.Core.System.Abstractions;
using Launcher.Core.UI.Abstractions;
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

        try
        {
            var services = new ServiceCollection();
            
            services.AddSingleton<HttpClient>(); 
            
            services.AddSingleton<LocalizationService>();
            services.AddSingleton<ILocalizationService>(sp => sp.GetRequiredService<LocalizationService>());
            services.AddSingleton<NotificationService>();
            services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<NotificationService>());
            
            //UI.WPF Services
            services.AddSingleton<IAssetExtractionService, AssetExtractionService>();
            services.AddSingleton<ThemeService>();
            services.AddSingleton<ImportOrchestratorService>();
            services.AddSingleton<IOverlayService, OverlayService>();
            services.AddSingleton<NavigationService>();
            services.AddSingleton<IDispatcherService, WpfDispatcherService>();
            services.AddSingleton<IInputService, InputService>();
            services.AddSingleton<IFileDialogService, WpfFileDialogService>();
            services.AddSingleton<PreviewGeneratorService>();
            
            //Core Services
            services.AddAssetsServices();
            services.AddConfigServices();
            services.AddGameServices();
            services.AddIdentityServices();
            services.AddInstancesServices();
            services.AddIntegrationsServices();
            services.AddModsServices();
            services.AddSystemServices();
            
            services.AddSingleton<AppStore>();
            services.AddSingleton<LoginStore>();
            services.AddSingleton<SettingsStore>();
            services.AddSingleton<InstancesStore>();
            services.AddSingleton<SkinsStore>();

            
            
            services.AddTransient<PlayViewModel>();
            services.AddTransient<InstallationsViewModel>();
            services.AddTransient<SkinsViewModel>();
            services.AddTransient<LoginVM>();
            services.AddTransient<SettingsVM>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<MainWindow>();
            
            Services = services.BuildServiceProvider();

            Services.GetRequiredService<IAssetExtractionService>().EnsureAllBaseAssetsExist();
            
            LocalizationService.Instance = (LocalizationService)Services.GetRequiredService<ILocalizationService>();
            NotificationService.Instance = (NotificationService)Services.GetRequiredService<INotificationService>();
            
            var mainViewModel = Services.GetRequiredService<MainViewModel>();
            var mainWindow = Services.GetRequiredService<MainWindow>();
            
            await mainViewModel.InitializeAsync();
            await Services.GetRequiredService<SkinsStore>().InitializeAsync();
            
            mainWindow.DataContext = mainViewModel;
            
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Critical error:\n{ex.Message}", 
                "Launcher Error", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
            
            Current.Shutdown();
        }
    }
}