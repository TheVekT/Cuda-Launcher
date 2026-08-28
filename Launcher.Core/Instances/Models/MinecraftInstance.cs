using Launcher.Core.Common.Enums;

namespace Launcher.Core.Instances.Models;

public class MinecraftInstance
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    public required string Name { get; set; }
    public required string IconPath { get; set; }
    
    public required string GameVersion { get; set; }   // 1.20.1
    public string? LoaderVersion { get; set; } // 47.1.0 (for Vanilla it will be a null)
    
    public GameLoaderType LoaderType { get; set; }
    
    public IsolationType IsolationType { get; init; }
    
    public DateTime? LastPlayedDate { get; set; }
    
    public GameSettings GameSettings { get; init; } = new();
    
    public PartialIsolationSettings PartialSettings { get; set; } = new();
    
    public BackupSettings BackupSettings { get; init; } = new();
    
    public bool RequestPerformanceMods { get; set; }
}

public class PartialIsolationSettings
{
    public bool IsModsUnique { get; set; } = true; 
    public bool IsConfigUnique { get; set; } = false; 
    public bool IsSavesUnique { get; set; } = false;
    public bool IsResourcePacksUnique { get; set; } = false;
}

public class BackupSettings
{
    public BackupPolicy SavesBackupSettings { get; set; } = BackupPolicy.Inherit;
    public BackupFrequency? SavesBackupFrequency { get; set; }
    public int? SavesMaxBackups { get; set; }
    public DateTime? LastBackupDate { get; set; }
}

public class GameSettings
{
    public int? AllocatedMemory { get; set; }
    public string? JvmArgs { get; set; }
    public bool? Fullscreen { get; set; }
    public string? GameResolution { get; set; } // e.g. "1920x1080"
}
