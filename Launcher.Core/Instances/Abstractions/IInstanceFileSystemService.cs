using Launcher.Core.Instances.Models;

namespace Launcher.Core.Instances.Abstractions;

public interface IInstanceFileSystemService
{
    Task InitializeOnCreation(MinecraftInstance instance);
    
    string PrepareForLaunch(MinecraftInstance instance);
    
    string GetGlobalMinecraftPath();
    void DeleteInstance(MinecraftInstance instance);
    
    void OpenInstanceFolder(MinecraftInstance? instance);
    void OpenInstanceModsFolder(MinecraftInstance? instance);
    void OpenRootMinecraftFolder();
    
    Task ImportModAsync(MinecraftInstance instance, string sourceFilePath);
    Task ImportResourcePackAsync(MinecraftInstance instance, string sourceFilePath);
    Task ImportShaderPackAsync(MinecraftInstance instance, string sourceFilePath);
    Task ImportSaveAsync(MinecraftInstance instance, string sourceZipPath);
}