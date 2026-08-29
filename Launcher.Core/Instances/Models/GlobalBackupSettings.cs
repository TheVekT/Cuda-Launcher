namespace Launcher.Core.Instances.Models;

public class GlobalBackupSettings(int savesMaxBackups, BackupFrequency savesBackupFrequency)
{
    public BackupFrequency SavesBackupFrequency { get; set; } = savesBackupFrequency;
    public int SavesMaxBackups { get; set; } = savesMaxBackups;
}