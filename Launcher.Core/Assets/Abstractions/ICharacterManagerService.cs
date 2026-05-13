using Launcher.Core.Common.Models;

namespace Launcher.Core.Assets.Abstractions;

public interface ICharacterManagerService
{
    Task SaveCharactersAsync(IEnumerable<CharacterModel> characters);
    
    Task<List<CharacterModel>> LoadCharactersAsync();

    Task<string> ImportSkinFileAsync(string sourceFilePath);
    
    string GetFullSkinPath(string skinFileName);
}