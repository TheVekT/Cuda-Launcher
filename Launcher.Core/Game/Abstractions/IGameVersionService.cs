using Launcher.Core.Common.Enums;

namespace Launcher.Core.Game.Abstractions;

public interface IGameVersionService
{ 
    Task<IEnumerable<string>> GetVanillaVersionsAsync(); 
    Task<IEnumerable<string>> GetLoaderVersionsAsync(GameLoaderType type, string gameVersion);
    Task<string> GetRecommendedLoaderVersionAsync(GameLoaderType type, string gameVersion);
    Task<IEnumerable<string>> GetGameVersionsByTypeAsync(GameLoaderType type); 
}