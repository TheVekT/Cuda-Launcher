using Launcher.Core.Game.Abstractions;
using Launcher.Core.Game.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.Core.Game;

public static class GameDependencyInjection
{
    public static IServiceCollection AddGameServices(this IServiceCollection services)
    {
        services.AddSingleton<IJavaPathResolver, JavaPathResolver>();
        services.AddSingleton<JvmArgumentsValidator>();
        services.AddSingleton<IGameVersionService, GameVersionService>();
        services.AddSingleton<ILaunchService, LaunchService>();
        
        return services;
    }
}