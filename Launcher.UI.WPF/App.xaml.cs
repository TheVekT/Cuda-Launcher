using System.Net.Http;
using System.Windows;
using Launcher.Core.Config;
using Launcher.Core.Game;
using Launcher.Core.Identity;
using Launcher.Core.Instances;
using Launcher.Core.Mods;
using Launcher.Core.System;
using Launcher.Infrastructure.Assets;
using System.Reflection;
using Launcher.Infrastructure.Assets.Abstractions;
using Launcher.Infrastructure.Config;
using Launcher.Infrastructure.Customization;
using Launcher.Infrastructure.Integrations;
using Launcher.Infrastructure.Localization;
using Launcher.Infrastructure.Updates;
using Launcher.Infrastructure.Updates.Abstractions;
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
            // Add infrastructure update checker service
            services.AddUpdatesServices(repositoryOwner: "TheVekT", repositoryName: "Cuda-Launcher");
            
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
            
            // Stores
            services.AddSingleton<AppStore>();
            services.AddSingleton<IdentityStore>();
            services.AddSingleton<SettingsStore>();
            services.AddSingleton<InstancesStore>();
            services.AddSingleton<SkinsStore>();
            
            // ViewModels & Views
            services.AddTransient<PlayViewModel>();
            services.AddTransient<InstallationsViewModel>();
            services.AddTransient<SkinsViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<MainWindow>();
            
            Services = services.BuildServiceProvider();

            // Check and handle updates before initializing main UI
            var updateLauncherService = Services.GetRequiredService<IUpdateLauncherService>();
            updateLauncherService.CleanupTempDirectory();

            try 
            {
                var updateChecker = Services.GetRequiredService<IUpdateCheckerService>();
                var currentVersion = Assembly.GetEntryAssembly()?
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                    .InformationalVersion
                    .Split('+')[0];

                if (currentVersion != null)
                {
                    var updateResult = await updateChecker.CheckForUpdatesAsync(currentVersion, true);
                
                    if (updateResult.IsSuccess && updateResult.Value.IsUpdateAvailable)
                    {
                        Console.WriteLine("Update available, launching updater...");
                        var launchResult = updateLauncherService.LaunchUpdater(updateResult.Value);
                        if (launchResult.IsSuccess)
                        {
                            // Shutdown current launcher so updater can update files without locks
                            Shutdown();
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to check for updates: {ex.Message}");
            }

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