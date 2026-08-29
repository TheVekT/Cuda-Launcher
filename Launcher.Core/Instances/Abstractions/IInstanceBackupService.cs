using FluentResults;
using Launcher.Core.Instances.Models;

namespace Launcher.Core.Instances.Abstractions;

public interface IInstanceBackupService
{
    bool IsBackupDue(MinecraftInstance instance, GlobalBackupSettings globalSettings);

    bool HasSavesToBackup(MinecraftInstance targetInstance);
    
    Task<Result<bool>> CreateBackupAsync(
        MinecraftInstance targetInstance, 
        IReadOnlyList<MinecraftInstance> allInstances, 
        GlobalBackupSettings globalSettings, 
        IProgress<InstanceBackupProgress>? progress = null,
        bool force = false);
}