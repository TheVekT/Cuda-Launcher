using Launcher.Core.Integrations.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.Core.Integrations;

public static class IntegrationsDependencyInjection
{
    public static IServiceCollection AddIntegrationsServices(this IServiceCollection services)
    {
        services.AddSingleton<IDiscordService, DiscordService>();

        return services;
    }
}