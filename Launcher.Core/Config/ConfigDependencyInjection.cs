using Launcher.Core.Config.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.Core.Config;

public static class ConfigDependencyInjection
{
    public static IServiceCollection AddConfigServices(this IServiceCollection services)
    {
        services.AddSingleton<ILauncherPathsService, LauncherPathsService>();
        
        return services;
    }
}