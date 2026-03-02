using System;
using System.Text.Json.Serialization;
using Launcher.Core.Enums;
using Launcher.Core.Models;

namespace Launcher.Core.Models
{
    public class MinecraftInstance
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public string Name { get; set; }
        public string IconPath { get; set; }
        
        public string GameVersion { get; set; }   // 1.20.1
        public string? LoaderVersion { get; set; } // 47.1.0 (для Vanilla будет null или пусто)
        
        public GameLoaderType LoaderType { get; set; }
        
        public IsolationType IsolationType { get; set; }
        
        public DateTime? LastPlayedDate { get; set; }
        
        public GameSettings GameSettings { get; set; } = new();
        
        public PartialIsolationSettings PartialSettings { get; set; } = new();
        
        public BackupSettings BackupSettings { get; set; } = new();
        
        public bool RequestPerformanceMods { get; set; } = false;
        public MinecraftInstance() { }
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
        public BackupFrequency? SavesBackupFrequency { get; set; } = null;
        public int? SavesMaxBackups { get; set; } = null;
        public DateTime? LastBackupDate { get; set; }
    }
    public class GameSettings
    {
        public int? AllocatedMemory { get; set; } = null;
        public string? JvmArgs { get; set; } = null;
        public bool? Fullscreen { get; set; } = null;
        public string? GameResolution { get; set; } = null; // e.g. "1920x1080"
    }
}