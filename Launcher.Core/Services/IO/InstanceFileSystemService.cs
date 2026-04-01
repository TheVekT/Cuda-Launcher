using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Launcher.Core.Enums;
using Launcher.Core.Helpers;
using Launcher.Core.Models;
using Launcher.Core.Services.System;

namespace Launcher.Core.Services.IO;

public interface IInstanceFileSystemService
{
    // Вызывается 1 раз при создании (ViewModel). Может запросить права Админа.
    Task InitializeOnCreation(MinecraftInstance instance);
    
    // Вызывается перед каждым запуском (LaunchService).
    string PrepareForLaunch(MinecraftInstance instance);
    
    string GetGlobalMinecraftPath();
    void DeleteInstance(MinecraftInstance instance);
    
    void OpenInstanceFolder(MinecraftInstance instance);
    void OpenInstanceModsFolder(MinecraftInstance instance);
    void OpenRootMinecraftFolder();

    // --- НОВЫЕ АСИНХРОННЫЕ МЕТОДЫ ИМПОРТА (DRAG & DROP) ---
    Task ImportModAsync(MinecraftInstance instance, string sourceFilePath);
    Task ImportResourcePackAsync(MinecraftInstance instance, string sourceFilePath);
    Task ImportShaderPackAsync(MinecraftInstance instance, string sourceFilePath);
    Task ImportSaveAsync(MinecraftInstance instance, string sourceZipPath);
}

public class InstanceFileSystemService : IInstanceFileSystemService
{
    private readonly string _instancesBasePath;
    private readonly string _portableGlobalPath;
    private readonly ILauncherPathsService _pathsService;
    private readonly ISymlinkService _symlinkService;

    public InstanceFileSystemService(ILauncherPathsService pathsService, ISymlinkService symlinkService)
    {
        _pathsService = pathsService;
        _symlinkService = symlinkService;
        
        _instancesBasePath = _pathsService.InstancesDirectory;
        _portableGlobalPath = Path.Combine(_pathsService.DataDirectory, "Global");
    }

    public string GetGlobalMinecraftPath()
    {
        if (!Directory.Exists(_portableGlobalPath)) Directory.CreateDirectory(_portableGlobalPath);
        return _portableGlobalPath;
    }

    private string GetExternalMinecraftPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
    }

    // Вспомогательный метод для получения корня инстанса
    private string GetInstanceRootPath(MinecraftInstance instance)
    {
        if (instance.IsolationType == IsolationType.Global)
            return GetExternalMinecraftPath();
            
        return Path.Combine(_instancesBasePath, instance.Id);
    }

    // --- МЕТОДЫ ИМПОРТА ---

    public async Task ImportModAsync(MinecraftInstance instance, string sourceFilePath)
    {
        await CopyFileToInstanceFolderAsync(instance, "mods", sourceFilePath);
    }

    public async Task ImportResourcePackAsync(MinecraftInstance instance, string sourceFilePath)
    {
        await CopyFileToInstanceFolderAsync(instance, "resourcepacks", sourceFilePath);
    }

    public async Task ImportShaderPackAsync(MinecraftInstance instance, string sourceFilePath)
    {
        await CopyFileToInstanceFolderAsync(instance, "shaderpacks", sourceFilePath);
    }

    public async Task ImportSaveAsync(MinecraftInstance instance, string sourceZipPath)
    {
        var targetDir = Path.Combine(GetInstanceRootPath(instance), "saves");
        CreateDir(targetDir);

        string saveName = Path.GetFileNameWithoutExtension(sourceZipPath);
        string extractPath = Path.Combine(targetDir, saveName);

        // Асинхронная распаковка архива
        await Task.Run(() => 
        {
            // Распаковываем во временную папку с именем архива
            if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
            ZipFile.ExtractToDirectory(sourceZipPath, extractPath);

            // Умная проверка: если внутри архива была всего одна папка (и больше никаких файлов в корне), 
            // значит архив был запакован вместе с корневой папкой мира.
            var extractedDirs = Directory.GetDirectories(extractPath);
            var extractedFiles = Directory.GetFiles(extractPath);

            if (extractedDirs.Length == 1 && extractedFiles.Length == 0)
            {
                // Ситуация: saves/MyWorld/MyWorld/level.dat
                // Нужно вытащить внутреннюю папку наружу
                string innerWorldFolder = extractedDirs[0];
                string tempMovePath = Path.Combine(targetDir, Guid.NewGuid().ToString());

                // Перемещаем внутреннюю папку во временное место
                Directory.Move(innerWorldFolder, tempMovePath);
                
                // Удаляем теперь уже пустую папку-обертку
                Directory.Delete(extractPath);
                
                // Переименовываем временную папку в оригинальное имя (или имя из архива)
                string finalFolderName = new DirectoryInfo(innerWorldFolder).Name;
                string finalPath = Path.Combine(targetDir, finalFolderName);
                
                if (Directory.Exists(finalPath)) Directory.Delete(finalPath, true);
                Directory.Move(tempMovePath, finalPath);
            }
            // Иначе ситуация: saves/MyWorld/level.dat - всё идеально, ничего двигать не нужно
        });
    }

    private async Task CopyFileToInstanceFolderAsync(MinecraftInstance instance, string targetFolderName, string sourceFilePath)
    {
        var targetDir = Path.Combine(GetInstanceRootPath(instance), targetFolderName);
        CreateDir(targetDir);

        var targetFile = Path.Combine(targetDir, Path.GetFileName(sourceFilePath));

        // Используем Task.Run для предотвращения зависания UI при копировании больших файлов
        await Task.Run(() => 
        {
            File.Copy(sourceFilePath, targetFile, overwrite: true);
        });
    }

    // --- ЭТАП 1: ИНИЦИАЛИЗАЦИЯ (UI) ---
    public async Task InitializeOnCreation(MinecraftInstance instance)
    {
        var instancePath = Path.Combine(_instancesBasePath, instance.Id);

        switch (instance.IsolationType)
        {
            case IsolationType.Partial:
                await PreparePartial(instancePath); 
                break;

            case IsolationType.Full:
                if (!Directory.Exists(instancePath)) Directory.CreateDirectory(instancePath);
                break;

            case IsolationType.Global:
                break;
        }
    }

    // --- ЭТАП 2: ПОДГОТОВКА К ЗАПУСКУ (GAME) ---
    public string PrepareForLaunch(MinecraftInstance instance)
    {
        var instancePath = Path.Combine(_instancesBasePath, instance.Id);

        switch (instance.IsolationType)
        {
            case IsolationType.Global:
                return GetExternalMinecraftPath(); 

            case IsolationType.Partial:
                if (!Directory.Exists(instancePath))
                {
                    throw new DirectoryNotFoundException("Instance folder not found. Please recreate the installation.");
                }
                return instancePath;

            case IsolationType.Full:
                if (!IsFullInstanceReady(instancePath))
                {
                    PrepareFullLazy(instancePath);
                }
                return instancePath;

            default:
                return GetGlobalMinecraftPath();
        }
    }

    public void DeleteInstance(MinecraftInstance instance)
    {
        if (instance.IsolationType == IsolationType.Global) return;
        if (string.IsNullOrWhiteSpace(instance.Id)) return;

        var instancePath = Path.Combine(_instancesBasePath, instance.Id);
        if (Directory.Exists(instancePath))
        {
            try { Directory.Delete(instancePath, true); }
            catch (Exception ex) { Debug.WriteLine($"Error deleting instance: {ex.Message}"); }
        }
    }

    // --- ВНУТРЕННИЕ МЕТОДЫ ---

    private async Task PreparePartial(string instancePath)
    {
        var sourcePath = GetExternalMinecraftPath();

        CreateDir(instancePath);
        CreateDir(Path.Combine(instancePath, "mods")); 

        CreateDir(sourcePath);
        
        var whitelistFolders = new[] { "config", "resourcepacks", "saves", "schematics", "screenshots", "shaderpacks" };
        var whitelistFiles = new[] { "options.txt", "optionsof.txt" };

        foreach (var folder in whitelistFolders)
        {
            CreateDir(Path.Combine(sourcePath, folder));
        }

        foreach (var file in whitelistFiles)
        {
            var filePath = Path.Combine(sourcePath, file);
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, ""); 
            }
        }

        var inclusionList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in whitelistFolders) inclusionList.Add(item);
        foreach (var item in whitelistFiles) inclusionList.Add(item);

        try 
        {
            await Task.Run(() => 
            {
                _symlinkService.CreateSymlinksElevated(sourcePath, instancePath, inclusionList);
            });
        }
        catch (Exception ex)
        {
            if (Directory.Exists(instancePath)) Directory.Delete(instancePath, true);
            throw new Exception("Administrator rights are required to create Partial Isolation links!", ex);
        }
    }

    private bool IsFullInstanceReady(string instancePath)
    {
        return File.Exists(Path.Combine(instancePath, "options.txt"));
    }

    private void PrepareFullLazy(string instancePath)
    {
        CreateDir(instancePath);
        string[] basicFolders = { "mods", "config", "saves", "screenshots", "resourcepacks" };
        foreach (var folder in basicFolders) CreateDir(Path.Combine(instancePath, folder));
    }

    private void CreateDir(string path)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
    }

    public void OpenRootMinecraftFolder()
    {
        var path = GetExternalMinecraftPath();
        CreateDir(path); 
        OpenFolderInExplorer(path);
    }

    public void OpenInstanceFolder(MinecraftInstance instance)
    {
        if (instance == null) return;

        if (instance.IsolationType == IsolationType.Global)
        {
            OpenRootMinecraftFolder();
            return;
        }

        var instancePath = Path.Combine(_instancesBasePath, instance.Id);
        CreateDir(instancePath);
        OpenFolderInExplorer(instancePath);
    }

    public void OpenInstanceModsFolder(MinecraftInstance instance)
    {
        if (instance == null) return;

        string modsPath;

        if (instance.IsolationType == IsolationType.Global)
        {
            modsPath = Path.Combine(GetExternalMinecraftPath(), "mods");
        }
        else
        {
            var instancePath = Path.Combine(_instancesBasePath, instance.Id);
            modsPath = Path.Combine(instancePath, "mods");
        }

        CreateDir(modsPath);
        OpenFolderInExplorer(modsPath);
    }

    private void OpenFolderInExplorer(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open folder {path}: {ex.Message}");
        }
    }
}
