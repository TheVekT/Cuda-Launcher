using Launcher.UI.WPF.Services.Windows.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.UI.WPF.Services.Windows;

public static class WindowsDependencyInjection
{
    public static IServiceCollection AddWindowsServices(this IServiceCollection services)
    {
        services.AddSingleton<IDispatcherService, WpfDispatcherService>();
        services.AddSingleton<IInputService, InputService>();
        services.AddSingleton<IFileDialogService, WpfFileDialogService>();
        services.AddSingleton<IClipboardService, WpfClipboardService>();
        services.AddSingleton<IBrowserService, SystemBrowserService>();
        
        return services;
    }
}
