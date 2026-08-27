using Launcher.Core.Common.Enums;

namespace Launcher.Core.Game.Abstractions;

public interface IGameVersionService
{ 
    Task<IEnumerable<string>> GetVanillaVersionsAsync(bool showSnapshots = false); 
    Task<IEnumerable<string>> GetLoaderVersionsAsync(GameLoaderType type, string gameVersion);
    Task<string> GetRecommendedLoaderVersionAsync(GameLoaderType type, string gameVersion);
    Task<IEnumerable<string>> GetGameVersionsByTypeAsync(GameLoaderType type, bool showSnapshots = false); 
}