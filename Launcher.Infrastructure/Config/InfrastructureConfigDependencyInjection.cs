using Launcher.Infrastructure.Config.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.Infrastructure.Config;

public static class InfrastructureConfigDependencyInjection
{
    public static IServiceCollection AddInfrastructureConfigServices(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, SettingsService>();
        return services;
    }
}
