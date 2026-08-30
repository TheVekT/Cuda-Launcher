using Launcher.Infrastructure.Assets.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.Infrastructure.Assets;

public static class AssetsDependencyInjection
{
    public static IServiceCollection AddAssetsServices(this IServiceCollection services)
    {
        services.AddSingleton<ICharacterManagerService, CharacterManagerService>();
        services.AddSingleton<IIconsService, IconsService>();
        services.AddSingleton<IMojangAssetCacheService, MojangAssetCacheService>();
        services.AddSingleton<IAssetsExtractionService, AssetsesExtractionService>();
        
        return services;
    }
}