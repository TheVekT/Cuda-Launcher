using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Launcher.Core.Common.Enums;
using Launcher.Core.Instances.Abstractions;

namespace Launcher.Core.Instances;

public class DragDropParserService : IDragDropParserService
{
    public async Task<ParsedFileType> ParseFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return ParsedFileType.Unknown;

        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".jar" => ParsedFileType.MinecraftMod,
            
            ".mrpack" => ParsedFileType.ModrinthPack,
            
            ".json" => await ParseJsonSignatureAsync(filePath),
            
            ".zip" => ParseZipSignature(filePath),
            
            _ => ParsedFileType.Unknown
        };
    }

    private async Task<ParsedFileType> ParseJsonSignatureAsync(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            using var doc = await JsonDocument.ParseAsync(stream);
            
            var root = doc.RootElement;

            // Ищем нашу сигнатуру локализации: корневой объект "Meta" с полем "LanguageCode"
            if (root.TryGetProperty("Meta", out var metaElement) && 
                metaElement.TryGetProperty("LanguageCode", out _))
            {
                return ParsedFileType.LauncherLocalization;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Drag&Drop] Ошибка парсинга JSON '{filePath}': {ex.Message}");
        }

        return ParsedFileType.Unknown;
    }

private ParsedFileType ParseZipSignature(string filePath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(filePath);

            bool isResourcepack = false;
            bool isShaderpack = false;

            foreach (var entry in archive.Entries)
            {
                var entryName = entry.FullName.ToLowerInvariant();
                
                if (entryName.EndsWith("theme.xaml")) 
                    return ParsedFileType.LauncherTheme;
                
                if (entryName.EndsWith("level.dat")) 
                    return ParsedFileType.MinecraftWorldSave;
                
                if (entryName.EndsWith("pack.mcmeta")) 
                    isResourcepack = true;
                
                if (entryName.StartsWith("shaders/") || entryName.Contains("/shaders/")) 
                    isShaderpack = true;
            }
            
            if (isResourcepack) return ParsedFileType.MinecraftResourcepack;
            if (isShaderpack) return ParsedFileType.MinecraftShaderpack;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Drag&Drop] Ошибка парсинга ZIP '{filePath}': {ex.Message}");
        }

        return ParsedFileType.Unknown;
    }
}
