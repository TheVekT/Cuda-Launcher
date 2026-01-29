
using System.Collections.Generic;
using System.Threading.Tasks;
using Launcher.Core.Models;

namespace Launcher.Core.Services.Game
{
    public interface IGameVersionService
    {
        Task<IEnumerable<string>> GetVanillaVersionsAsync();
        Task<IEnumerable<string>> GetLoaderVersionsAsync(GameLoaderType type, string gameVersion);
        
        Task<IEnumerable<string>> GetGameVersionsByTypeAsync(GameLoaderType type);
    }
}