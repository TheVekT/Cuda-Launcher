using Launcher.Core.Mods.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.Core.Mods;

public static class ModsDependencyInjection
{
    public static IServiceCollection AddModsServices(this IServiceCollection services)
    {
        services.AddSingleton<IModrinthService, ModrinthService>();

        return services;
    }
}