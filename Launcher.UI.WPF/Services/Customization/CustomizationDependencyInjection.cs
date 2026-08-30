using Launcher.UI.WPF.Services.Customization.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.UI.WPF.Services.Customization;

public static class CustomizationDependencyInjection
{
    public static IServiceCollection AddUiCustomizationServices(this IServiceCollection services)
    {
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IThemeService, ThemeService>();
        
        return services;
    }
}
