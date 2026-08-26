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
using Launcher.Infrastructure;
using Launcher.UI.WPF.Services.Abstractions;
using Launcher.UI.WPF.ViewModels.Accounts;
using Launcher.UI.WPF.ViewModels.Game;
using Launcher.UI.WPF.ViewModels.Instances;
using Launcher.UI.WPF.ViewModels.Settings;
using Launcher.UI.WPF.Views;

namespace Launcher.UI.WPF;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; }

    protected override async void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            Console.WriteLine($"=== UNHANDLED EXCEPTION ===");
            Console.WriteLine($"Exception Type: {ex.ExceptionObject?.GetType().FullName}");
            Console.WriteLine($"Message: {ex.ExceptionObject}");
            Console.WriteLine($"Stack Trace: {((Exception)ex.ExceptionObject)?.StackTrace}");
        };
    
        this.DispatcherUnhandledException += (s, ex) =>
        {
            Console.WriteLine($"=== DISPATCHER EXCEPTION ===");
            Console.WriteLine($"Exception: {ex.Exception.Message}");
            Console.WriteLine($"Stack Trace:\n{ex.Exception.StackTrace}");
            if (ex.Exception.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.Exception.InnerException.Message}");
            }
            ex.Handled = true;
        };
        base.OnStartup(e);

        try
        {
            var services = new ServiceCollection();
            
            services.AddSingleton<HttpClient>(); 
            
            services.AddSingleton<ILocalizationService, LocalizationService>();
            services.AddSingleton<INotificationService, NotificationService>();
            
            //UI.WPF Services
            services.AddSingleton<IAssetExtractionService, AssetExtractionService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<ImportOrchestratorService>();
            services.AddSingleton<IOverlayService, OverlayService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IDispatcherService, WpfDispatcherService>();
            services.AddSingleton<IInputService, InputService>();
            services.AddSingleton<IFileDialogService, WpfFileDialogService>();
            services.AddSingleton<IPreviewGeneratorService, PreviewGeneratorService>();
            services.AddSingleton<IClipboardService, WpfClipboardService>();
            
            //Core Services
            services.AddAssetsServices();
            services.AddConfigServices();
            services.AddGameServices();
            services.AddIdentityServices();
            services.AddInstancesServices();
            services.AddIntegrationsServices();
            services.AddModsServices();
            services.AddSystemServices();
            
            //Infrastructure Services
            services.AddInfrastructureServices();
            
            //Stores
            services.AddSingleton<AppStore>();
            services.AddSingleton<LoginStore>();
            services.AddSingleton<SettingsStore>();
            services.AddSingleton<InstancesStore>();
            services.AddSingleton<SkinsStore>();

            
            //ViewModels
            services.AddTransient<PlayViewModel>();
            services.AddTransient<InstallationsViewModel>();
            services.AddTransient<SkinsViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<MainWindow>();
            
            Services = services.BuildServiceProvider();

            Services.GetRequiredService<IAssetExtractionService>().EnsureAllBaseAssetsExist();
            
            LocalizationService.Instance = (LocalizationService)Services.GetRequiredService<ILocalizationService>();
            
            var mainViewModel = Services.GetRequiredService<MainWindowViewModel>();
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