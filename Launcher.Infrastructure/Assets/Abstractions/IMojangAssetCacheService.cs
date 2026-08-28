namespace Launcher.Infrastructure.Assets.Abstractions;

public interface IMojangAssetCacheService
{
    Task<string?> GetOrDownloadAssetAsync(string url, string assetId);
}