using Launcher.UI.WPF.Services.Customization;
using Launcher.UI.WPF.Services.Rendering;
using Launcher.UI.WPF.Services.Shell;
using Launcher.UI.WPF.Services.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.UI.WPF.Services;

public static class ServicesDependencyInjection
{
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        services.AddUiCustomizationServices();
        services.AddRenderingServices();
        services.AddShellServices();
        services.AddWindowsServices();
        
        return services;
    }
}
