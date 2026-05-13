using System.Text.Json;
using Launcher.Core.Assets.Abstractions;
using Launcher.Core.Common.Models;
using Launcher.Core.Config.Abstractions;

namespace Launcher.Core.Assets;

public class CharacterManagerService : ICharacterManagerService
{
    private readonly ILauncherPathsService _pathsService;
    private readonly string _skinsDirectory;
    private readonly string _registryFilePath;

    public CharacterManagerService(ILauncherPathsService pathsService)
    {
        _pathsService = pathsService;
        
        _skinsDirectory = Path.Combine(_pathsService.UserDataDirectory, "Skins");
        _registryFilePath = Path.Combine(_skinsDirectory, "skins.json");
        
        if (!Directory.Exists(_skinsDirectory))
        {
            Directory.CreateDirectory(_skinsDirectory);
        }
    }

    public async Task SaveCharactersAsync(IEnumerable<CharacterModel> characters)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(characters, options);
        
        await File.WriteAllTextAsync(_registryFilePath, json);
    }

    public async Task<List<CharacterModel>> LoadCharactersAsync()
    {
        if (!File.Exists(_registryFilePath))
        {
            return new List<CharacterModel>();
        }

        var json = await File.ReadAllTextAsync(_registryFilePath);
        var characters = JsonSerializer.Deserialize<List<CharacterModel>>(json) ?? new List<CharacterModel>();

        var validCharacters = new List<CharacterModel>();
        bool needsResave = false;
        var missingSkins = new List<string>();

        foreach (var character in characters)
        {
            if (string.IsNullOrEmpty(character.SkinFileName)) continue;

            string fullPath = GetFullSkinPath(character.SkinFileName);

            if (File.Exists(fullPath))
            {
                validCharacters.Add(character);
            }
            else
            {
                missingSkins.Add(character.Name ?? character.Id);
                needsResave = true;
            }
        }

        if (needsResave)
        {
            await SaveCharactersAsync(validCharacters);
        }

        if (missingSkins.Count > 0)
        {
            string missingNames = string.Join(", ", missingSkins);
            throw new FileNotFoundException($"The following character skins are missing from disk and were removed from the registry: {missingNames}");
        }

        return validCharacters;
    }

    public async Task<string> ImportSkinFileAsync(string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("The source skin file does not exist.", sourceFilePath);
        }
        
        string extension = Path.GetExtension(sourceFilePath);
        string newFileName = $"{Guid.NewGuid()}{extension}";
        string destinationPath = GetFullSkinPath(newFileName);
        
        using (Stream source = File.OpenRead(sourceFilePath))
        using (Stream destination = File.Create(destinationPath))
        {
            await source.CopyToAsync(destination);
        }

        return newFileName;
    }

    public string GetFullSkinPath(string skinFileName)
    {
        if (string.IsNullOrEmpty(skinFileName)) return string.Empty;
        return Path.Combine(_skinsDirectory, skinFileName);
    }
}