using Launcher.Infrastructure.Customization.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.Infrastructure.Customization;

public static class CustomizationDependencyInjection
{
    public static IServiceCollection AddCustomizationServices(this IServiceCollection services)
    {
        services.AddSingleton<IThemeProvider, ThemeProvider>();
        return services;
    }
}