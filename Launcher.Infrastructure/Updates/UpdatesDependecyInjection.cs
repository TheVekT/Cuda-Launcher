using Launcher.Core.Config.Abstractions;
using Launcher.Infrastructure.Updates.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.Infrastructure.Updates;

public static class UpdatesDependencyInjection
{
    public static IServiceCollection AddUpdatesServices(
        this IServiceCollection services,
        string repositoryOwner,
        string repositoryName)
    {
        services.AddSingleton<IUpdateCheckerService>(sp => 
            new GitHubUpdateCheckerService(
                sp.GetRequiredService<HttpClient>(),
                repositoryOwner,
                repositoryName));
        services.AddSingleton<IUpdateLauncherService, UpdateLauncherService>();

        return services;
    }
}