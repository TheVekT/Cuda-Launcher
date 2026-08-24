using Launcher.Infrastructure.Localization;
using Launcher.Infrastructure.Localization.Abstractions;
using Launcher.Infrastructure.Themes;
using Launcher.Infrastructure.Themes.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<ILocalizationProvider, JsonLocalizationProvider>();
        services.AddSingleton<IThemeProvider, ThemeProvider>();
        
        return services;
    }
}
