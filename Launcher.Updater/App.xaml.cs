using System.Net.Http;
using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using Launcher.Updater.Helpers;
using Launcher.Updater.Services;
using Launcher.Updater.Services.Abstractions;
using Launcher.Updater.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.Updater;

public partial class App
{
    public static IServiceProvider? Services { get; private set; }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        // Http Client
        services.AddSingleton(new HttpClient());

        // Services
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IUpdateExecutionService, UpdateExecutionService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();

        // Views
        services.AddSingleton<MainWindow>();

        Services = services.BuildServiceProvider();
        Ioc.Default.ConfigureServices(Services);

        // 1. Initialize Theme
        var themeService = Services.GetRequiredService<IThemeService>();
        themeService.InitializeTheme();

        // 2. Show UI
        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        // 3. Parse command-line arguments and run the update
        var parsedArgs = UpdaterArgumentsParser.ParseCommandLineArgs(e.Args);
        var viewModel = Services.GetRequiredService<MainWindowViewModel>();
        var updateExecutionService = Services.GetRequiredService<IUpdateExecutionService>();

        bool success = await updateExecutionService.ApplyUpdateAsync(parsedArgs, viewModel.ProgressReporter);

        // 4. Close the updater after completion (managed here in App, not in ViewModel)
        if (success)
        {
            await Task.Delay(1000);
            Shutdown();
        }
        else
        {
            // In case of error, keep visible briefly so user can see what failed
            await Task.Delay(3000);
            Shutdown();
        }
    }
}