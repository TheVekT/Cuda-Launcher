using System.Diagnostics;
using System.IO.Compression;
using Launcher.Core.Common.Enums;
using Launcher.Core.Config.Abstractions;
using Launcher.Core.Instances.Abstractions;
using Launcher.Core.Instances.Models;
using Launcher.Core.System.Abstractions;

namespace Launcher.Core.Instances;

public class InstanceFileSystemService : IInstanceFileSystemService
{
    private readonly string _instancesBasePath;
    private readonly string _portableGlobalPath;
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
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
    
    private string GetInstanceRootPath(MinecraftInstance instance)
    {
        if (instance.IsolationType == IsolationType.Global)
            return GetExternalMinecraftPath();
            
        return Path.Combine(_instancesBasePath, instance.Id);
    }

    public async Task ImportModAsync(MinecraftInstance? instance, string sourceFilePath)
    {
        if (instance == null) 
            throw new ArgumentNullException(nameof(instance));
        await CopyFileToInstanceFolderAsync(instance, "mods", sourceFilePath);
    }

    public async Task ImportResourcePackAsync(MinecraftInstance? instance, string sourceFilePath)
    {
        if (instance == null) 
            throw new ArgumentNullException(nameof(instance));
        await CopyFileToInstanceFolderAsync(instance, "resourcepacks", sourceFilePath);
    }

    public async Task ImportShaderPackAsync(MinecraftInstance? instance, string sourceFilePath)
    {
        if (instance == null) 
            throw new ArgumentNullException(nameof(instance));
        await CopyFileToInstanceFolderAsync(instance, "shaderpacks", sourceFilePath);
    }

    public async Task ImportSaveAsync(MinecraftInstance? instance, string sourceZipPath)
    {
        if (instance == null) 
            throw new ArgumentNullException(nameof(instance));
        var targetDir = Path.Combine(GetInstanceRootPath(instance), "saves");
        CreateDir(targetDir);

        string saveName = Path.GetFileNameWithoutExtension(sourceZipPath);
        string extractPath = Path.Combine(targetDir, saveName);
        
        await Task.Run(() => 
        {
            // unzip the save into a temporary folder first to check its structure
            if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
            ZipFile.ExtractToDirectory(sourceZipPath, extractPath);
            
            // Smart check: if there was only one folder inside the archive (and no other files in the root),
            // In this case, archive was packed with the world's root folder.
            var extractedDirs = Directory.GetDirectories(extractPath);
            var extractedFiles = Directory.GetFiles(extractPath);

            if (extractedDirs.Length == 1 && extractedFiles.Length == 0)
            {
                // Situation: saves/MyWorld/MyWorld/level.dat
                // We need to move the inner folder up one level and delete the now-empty outer folder.
                string innerWorldFolder = extractedDirs[0];
                string tempMovePath = Path.Combine(targetDir, Guid.NewGuid().ToString());
                
                Directory.Move(innerWorldFolder, tempMovePath);
                
                Directory.Delete(extractPath);
                
                // Now we rename the inner folder to the original save name (or keep its original name if you prefer)
                string finalFolderName = new DirectoryInfo(innerWorldFolder).Name;
                string finalPath = Path.Combine(targetDir, finalFolderName);
                
                if (Directory.Exists(finalPath)) Directory.Delete(finalPath, true);
                Directory.Move(tempMovePath, finalPath);
            }
            // Otherwise, situation: saves/MyWorld/level.dat - everything is perfect, no need to move anything
        });
    }

    private async Task CopyFileToInstanceFolderAsync(MinecraftInstance instance, string targetFolderName, string sourceFilePath)
    {
        var targetDir = Path.Combine(GetInstanceRootPath(instance), targetFolderName);
        CreateDir(targetDir);

        var targetFile = Path.Combine(targetDir, Path.GetFileName(sourceFilePath));
        
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
                await File.WriteAllTextAsync(filePath, ""); 
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
        string[] basicFolders = ["mods", "config", "saves", "screenshots", "resourcepacks"];
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

    public void OpenInstanceFolder(MinecraftInstance? instance)
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

    public void OpenInstanceModsFolder(MinecraftInstance? instance)
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
