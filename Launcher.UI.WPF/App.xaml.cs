using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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
using Launcher.Infrastructure.Config.Abstractions;
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
    private static Mutex? _instanceMutex;

    public static IServiceProvider? Services { get; private set; }

    protected override async void OnStartup(StartupEventArgs e)
    {
        var mutexName = GetApplicationMutexName();
        _instanceMutex = new Mutex(true, mutexName, out bool isFirstInstance);

        if (!isFirstInstance)
        {
            MessageBox.Show(
                "An instance of Cuda Launcher is already running from this directory.\nLaunching a duplicate instance is prevented to avoid file corruption.",
                "Cuda Launcher",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            Shutdown();
            return;
        }

        RegisterGlobalExceptionHandlers();

        base.OnStartup(e);

        try
        {
            Services = ConfigureServices();

            if (await TryCheckAndLaunchUpdateAsync())
            {
                return;
            }

            await InitializeAndShowMainWindowAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Critical error:\n{ex.Message}", 
                "Launcher Error", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
            Console.WriteLine($"Critical error: {ex.Message}");
            Current.Shutdown();
        }
    }

    private static string GetApplicationMutexName()
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToLowerInvariant();
        var pathBytes = Encoding.UTF8.GetBytes(basePath);
        var hashBytes = SHA256.HashData(pathBytes);
        var hash = Convert.ToHexString(hashBytes);

        return $@"Local\CudaLauncher_{hash}";
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        
        services.AddSingleton<HttpClient>(); 
        
        // Updates Infrastructure
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

        return services.BuildServiceProvider();
    }

    private void RegisterGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
        {
            Console.WriteLine("=== UNHANDLED EXCEPTION ===");
            Console.WriteLine($"Exception Type: {ex.ExceptionObject.GetType().FullName}");
            Console.WriteLine($"Message: {ex.ExceptionObject}");
            Console.WriteLine($"Stack Trace: {((Exception)ex.ExceptionObject).StackTrace}");
        };

        DispatcherUnhandledException += (_, ex) =>
        {
            Console.WriteLine("=== DISPATCHER EXCEPTION ===");
            Console.WriteLine($"Exception: {ex.Exception.Message}");
            Console.WriteLine($"Stack Trace:\n{ex.Exception.StackTrace}");
            if (ex.Exception.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.Exception.InnerException.Message}");
            }
            ex.Handled = true;
        };
    }

    private async Task<bool> TryCheckAndLaunchUpdateAsync()
    {
        if (Services == null)
        {
            return false;
        }

        var updateLauncherService = Services.GetRequiredService<IUpdateLauncherService>();
        updateLauncherService.CleanupTempDirectory();

        var isEnableAutoUpdates = Services.GetRequiredService<ISettingsService>()
            .GetPropertyValue<SettingsStore, bool>(nameof(SettingsStore.IsEnableAutoUpdates), defaultValue: true);

        if (!isEnableAutoUpdates)
        {
            return false;
        }

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
                        Shutdown();
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to check for updates: {ex.Message}");
        }

        return false;
    }

    private async Task InitializeAndShowMainWindowAsync()
    {
        if (Services == null)
        {
            return;
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

    protected override void OnExit(ExitEventArgs e)
    {
        if (_instanceMutex != null)
        {
            try
            {
                _instanceMutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Ignored if thread terminated without owning the mutex
            }
            finally
            {
                _instanceMutex.Dispose();
                _instanceMutex = null;
            }
        }
        base.OnExit(e);
    }
}