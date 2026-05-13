using Launcher.Core.Game.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.Core.Game;

public static class GameDependencyInjection
{
    public static IServiceCollection AddGameServices(this IServiceCollection services)
    {
        services.AddSingleton<IGameVersionService, GameVersionService>();
        services.AddSingleton<ILaunchService, LaunchService>();
        
        return services;
    }
}