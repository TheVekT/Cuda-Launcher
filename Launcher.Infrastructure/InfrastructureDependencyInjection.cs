using Launcher.Infrastructure.Localization;
using Launcher.Infrastructure.Localization.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<ILocalizationProvider, JsonLocalizationProvider>();
        
        return services;
    }
}
