using Launcher.Core.Assets.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.Core.Assets;

public static class AssetsDependencyInjection
{
    public static IServiceCollection AddAssetsServices(this IServiceCollection services)
    {
        services.AddSingleton<ICharacterManagerService, CharacterManagerService>();
        services.AddSingleton<IIconsService, IconsService>();
        services.AddSingleton<IMojangAssetCacheService, MojangAssetCacheService>();
        
        return services;
    }
}