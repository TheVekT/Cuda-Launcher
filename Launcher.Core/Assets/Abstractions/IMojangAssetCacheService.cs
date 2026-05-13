namespace Launcher.Core.Assets.Abstractions;

public interface IMojangAssetCacheService
{
    Task<string> GetOrDownloadAssetAsync(string url, string assetId);
}