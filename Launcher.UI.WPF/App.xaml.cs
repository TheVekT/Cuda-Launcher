using System.Net.Http;
using System.Windows;
using Launcher.Core.Config;
using Launcher.Core.Game;
using Launcher.Core.Identity;
using Launcher.Core.Instances;
using Launcher.Core.Mods;
using Launcher.Core.System;
using Launcher.Infrastructure.Assets;
using Launcher.Infrastructure.Assets.Abstractions;
using Launcher.Infrastructure.Config;
using Launcher.Infrastructure.Customization;
using Launcher.Infrastructure.Integrations;
using Launcher.Infrastructure.Localization;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Services.Customization;
using Launcher.UI.WPF.Services.Customization.Abstractions;
using Launcher.UI.WPF.Stores;
using Launcher.UI.WPF.ViewModels;
using Launcher.UI.WPF.ViewModels.Config;
using Launcher.UI.WPF.ViewModels.Game;
using Launcher.UI.WPF.ViewModels.Identity;
using Launcher.UI.WPF.ViewModels.Instances;
using Launcher.UI.WPF.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.UI.WPF;

public partial class App
{
    public static IServiceProvider? Services { get; private set; }

    protected override async void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
        {
            Console.WriteLine($"=== UNHANDLED EXCEPTION ===");
            Console.WriteLine($"Exception Type: {ex.ExceptionObject.GetType().FullName}");
            Console.WriteLine($"Message: {ex.ExceptionObject}");
            Console.WriteLine($"Stack Trace: {((Exception)ex.ExceptionObject).StackTrace}");
        };
    
        DispatcherUnhandledException += (_, ex) =>
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
            
            // Core Services
            services.AddConfigServices();
            services.AddGameServices();
            services.AddIdentityServices();
            services.AddInstancesServices();
            services.AddModsServices();
            services.AddSystemServices();
            
            // Infrastructure Services
            services.AddInfrastructureConfigServices();
            services.AddCustomizationServices();
            services.AddIntegrationsServices();
            services.AddLocalizationServices();
            services.AddAssetsServices();
            
            // UI Services
            services.AddUiServices();
            
            //Stores
            services.AddSingleton<AppStore>();
            services.AddSingleton<IdentityStore>();
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

            Services.GetRequiredService<IAssetsExtractionService>().EnsureAllBaseAssetsExist();
            
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