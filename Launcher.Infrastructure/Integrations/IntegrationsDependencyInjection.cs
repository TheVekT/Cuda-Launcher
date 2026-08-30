using Launcher.Infrastructure.Integrations.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.Infrastructure.Integrations;

public static class IntegrationsDependencyInjection
{
    public static IServiceCollection AddIntegrationsServices(this IServiceCollection services)
    {
        services.AddSingleton<IDiscordService, DiscordService>();

        return services;
    }
}