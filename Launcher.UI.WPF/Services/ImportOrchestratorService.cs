using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Launcher.Core.Enums;
using Launcher.Core.Models;
using Launcher.Core.Services.IO;
using Launcher.UI.WPF.Messages; // Для отправки сообщений
using CommunityToolkit.Mvvm.Messaging;

namespace Launcher.UI.WPF.Services;

public class ImportOrchestratorService
{
    private readonly IDragDropParserService _dragDropParserService;
    private readonly IInstanceFileSystemService _instanceFileSystemService;
    private readonly ThemeService _themeService;

    public ImportOrchestratorService(
        IDragDropParserService dragDropParserService,
        IInstanceFileSystemService instanceFileSystemService,
        ThemeService themeService)
    {
        _dragDropParserService = dragDropParserService;
        _instanceFileSystemService = instanceFileSystemService;
        _themeService = themeService;
    }
    
    public async Task<int> ProcessDroppedFilesAsync(string[] files, MinecraftInstance currentInstance, IProgress<(double Percent, string FileName)> progress, CancellationToken token)
    {
        int totalFiles = files.Length;
        int processedFiles = 0;
        int successCount = 0;

        bool themesChanged = false;
        bool langsChanged = false;

        foreach (var file in files)
        {
            token.ThrowIfCancellationRequested();

            string fileName = Path.GetFileName(file);
            fileName = fileName.Length > 32 ? fileName.Remove(32) : fileName;
            
            double currentPercent = ((double)processedFiles / totalFiles) * 100;
            progress?.Report((currentPercent, fileName));

            try
            {
                var fileType = await _dragDropParserService.ParseFileAsync(file);
                
                switch (fileType)
                {
                    case ParsedFileType.MinecraftMod:
                        if (currentInstance == null) throw new InvalidOperationException("Select an instance to install the mod.");
                        await _instanceFileSystemService.ImportModAsync(currentInstance, file);
                        break;
                    case ParsedFileType.MinecraftResourcepack:
                        if (currentInstance == null) throw new InvalidOperationException("Select an instance to install the resource pack.");
                        await _instanceFileSystemService.ImportResourcePackAsync(currentInstance, file);
                        break;
                    case ParsedFileType.MinecraftShaderpack:
                        if (currentInstance == null) throw new InvalidOperationException("Select an instance to install the shader pack.");
                        await _instanceFileSystemService.ImportShaderPackAsync(currentInstance, file);
                        break;
                    case ParsedFileType.MinecraftWorldSave:
                        if (currentInstance == null) throw new InvalidOperationException("Select an instance to import the world save.");
                        await _instanceFileSystemService.ImportSaveAsync(currentInstance, file);
                        break;
                    case ParsedFileType.LauncherTheme:
                        await _themeService.ImportTheme(file);
                        themesChanged = true;
                        break;
                    case ParsedFileType.LauncherLocalization:
                        await LocalizationService.Instance.ImportLocalization(file);
                        langsChanged = true;
                        break;
                    case ParsedFileType.Unknown:
                        default:
                        Debug.WriteLine($"Unknown file type for '{file}'. Skipping.");
                        continue;
                }
                successCount++;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { Debug.WriteLine($"Error importing file '{file}': {ex}"); }
            finally
            {
                processedFiles++;
                double newPercent = ((double)processedFiles / totalFiles) * 100;
                
                progress?.Report((newPercent, fileName));
            }
        }
        
        if (themesChanged) WeakReferenceMessenger.Default.Send(new ThemeImportedMessage());
        if (langsChanged) WeakReferenceMessenger.Default.Send(new LanguageImportedMessage());

        return successCount;
    }
}