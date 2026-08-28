using System.Text.Json;
using Launcher.Core.Config.Abstractions;
using Launcher.Infrastructure.Assets.Abstractions;
using Launcher.Infrastructure.Assets.Models;

namespace Launcher.Infrastructure.Assets;

public class CharacterManagerService : ICharacterManagerService
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
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
            return [];
        }

        var json = await File.ReadAllTextAsync(_registryFilePath);
        var characters = JsonSerializer.Deserialize<List<CharacterModel>>(json) ?? [];

        var validCharacters = new List<CharacterModel>();
        var needsResave = false;
        var missingSkins = new List<string>();

        foreach (var character in characters)
        {
            if (string.IsNullOrEmpty(character.SkinFileName)) continue;

            var fullPath = GetFullSkinPath(character.SkinFileName);

            if (File.Exists(fullPath))
            {
                validCharacters.Add(character);
            }
            else
            {
                missingSkins.Add(character.Name);
                needsResave = true;
            }
        }

        if (needsResave)
        {
            await SaveCharactersAsync(validCharacters);
        }

        if (missingSkins.Count <= 0) return validCharacters;
        var missingNames = string.Join(", ", missingSkins);
        throw new FileNotFoundException($"The following character skins are missing from disk and were removed from the registry: {missingNames}");
    }

    public async Task<string> ImportSkinFileAsync(string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("The source skin file does not exist.", sourceFilePath);
        }
        
        var extension = Path.GetExtension(sourceFilePath);
        var newFileName = $"{Guid.NewGuid()}{extension}";
        var destinationPath = GetFullSkinPath(newFileName);

        await using Stream source = File.OpenRead(sourceFilePath);
        await using Stream destination = File.Create(destinationPath);
        await source.CopyToAsync(destination);

        return newFileName;
    }

    public string GetFullSkinPath(string skinFileName)
    {
        return string.IsNullOrEmpty(skinFileName) ? string.Empty : Path.Combine(_skinsDirectory, skinFileName);
    }
}