using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;
using Launcher.Core.Enums;
using Launcher.Core.Models;

namespace Launcher.Core.Services.IO
{
    public interface IDragDropParserService
    {
        Task<ParsedFileType> ParseFileAsync(string filePath);
    }

    public class DragDropParserService : IDragDropParserService
    {
        public async Task<ParsedFileType> ParseFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return ParsedFileType.Unknown;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            return extension switch
            {
                // 4. Моды (обычно это .jar файлы)
                ".jar" => ParsedFileType.MinecraftMod,
                
                // Задел на будущее сборки Modrinth
                ".mrpack" => ParsedFileType.ModrinthPack,
                
                // 2. Файлы локализаций
                ".json" => await ParseJsonSignatureAsync(filePath),
                
                // 1, 3, 5, 6. Все остальное упаковано в ZIP
                ".zip" => ParseZipSignature(filePath),
                
                _ => ParsedFileType.Unknown
            };
        }

        private async Task<ParsedFileType> ParseJsonSignatureAsync(string filePath)
        {
            try
            {
                // Читаем JSON асинхронно
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
                // Открываем ZIP только для чтения оглавления
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
}