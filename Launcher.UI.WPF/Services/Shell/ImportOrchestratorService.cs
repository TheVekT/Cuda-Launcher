using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Common.Enums;
using Launcher.Core.Instances.Abstractions;
using Launcher.Core.Instances.Models;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Services.Customization;
using Launcher.UI.WPF.Services.Customization.Abstractions;
using Launcher.UI.WPF.Services.Shell.Abstractions;

namespace Launcher.UI.WPF.Services.Shell;

public class ImportOrchestratorService(
    IFileTypeDetector fileTypeDetector,
    IInstanceFileSystemService instanceFileSystemService,
    IThemeService themeService)
    : IImportOrchestratorService
{
    public async Task<int> ProcessDroppedFilesAsync(string[] files, MinecraftInstance? currentInstance, IProgress<(double Percent, string FileName)> progress, CancellationToken token)
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
                var fileType = await fileTypeDetector.ParseFileAsync(file);
                
                switch (fileType)
                {
                    case ParsedFileType.MinecraftMod:
                        if (currentInstance == null) throw new InvalidOperationException("Select an instance to install the mod.");
                        await instanceFileSystemService.ImportModAsync(currentInstance, file);
                        break;
                    case ParsedFileType.MinecraftResourcepack:
                        if (currentInstance == null) throw new InvalidOperationException("Select an instance to install the resource pack.");
                        await instanceFileSystemService.ImportResourcePackAsync(currentInstance, file);
                        break;
                    case ParsedFileType.MinecraftShaderpack:
                        if (currentInstance == null) throw new InvalidOperationException("Select an instance to install the shader pack.");
                        await instanceFileSystemService.ImportShaderPackAsync(currentInstance, file);
                        break;
                    case ParsedFileType.MinecraftWorldSave:
                        if (currentInstance == null) throw new InvalidOperationException("Select an instance to import the world save.");
                        await instanceFileSystemService.ImportSaveAsync(currentInstance, file);
                        break;
                    case ParsedFileType.LauncherTheme:
                        await themeService.ImportTheme(file);
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