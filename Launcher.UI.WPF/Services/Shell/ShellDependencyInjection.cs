using Launcher.UI.WPF.Services.Shell.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.UI.WPF.Services.Shell;

public static class ShellDependencyInjection
{
    public static IServiceCollection AddShellServices(this IServiceCollection services)
    {
        services.AddSingleton<IImportOrchestratorService, ImportOrchestratorService>();
        services.AddSingleton<IOverlayService, OverlayService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<INotificationService, NotificationService>();
        
        return services;
    }
}
