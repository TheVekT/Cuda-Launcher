using System.Diagnostics;
using System.IO.Compression;
using FluentResults;
using Launcher.Core.Common.Enums;
using Launcher.Core.Config.Abstractions;
using Launcher.Core.Instances.Abstractions;
using Launcher.Core.Instances.Models;

namespace Launcher.Core.Instances;

public class InstanceBackupService(
    ILauncherPathsService pathsService,
    IInstanceFileSystemService fileSystemService)
    : IInstanceBackupService
{
    public bool IsBackupDue(MinecraftInstance instance, GlobalBackupSettings globalSettings)
    {
        var policy = instance.BackupSettings.SavesBackupSettings;
        if (policy == BackupPolicy.ForceOff)
            return false;

        var lastBackup = instance.BackupSettings.LastBackupDate;
        if (lastBackup == null)
            return true;

        var frequency = policy switch
        {
            BackupPolicy.ForceOn => instance.BackupSettings.SavesBackupFrequency ?? globalSettings.SavesBackupFrequency,
            _ => globalSettings.SavesBackupFrequency
        };

        var threshold = frequency switch
        {
            BackupFrequency.Daily => TimeSpan.FromDays(1),
            BackupFrequency.Weekly => TimeSpan.FromDays(7),
            BackupFrequency.Biweekly => TimeSpan.FromDays(14),
            BackupFrequency.Monthly => TimeSpan.FromDays(30),
            _ => TimeSpan.FromDays(7)
        };

        return DateTime.Now - lastBackup.Value >= threshold;
    }

    public bool HasSavesToBackup(MinecraftInstance targetInstance)
    {
        string sourceSavesPath = GetSavesPath(targetInstance);
        return Directory.Exists(sourceSavesPath) && Directory.EnumerateDirectories(sourceSavesPath).Any();
    }

    private string GetSavesPath(MinecraftInstance targetInstance)
    {
        bool isGlobalSaves = targetInstance.IsolationType == IsolationType.Global || 
                            (targetInstance.IsolationType == IsolationType.Partial && !targetInstance.PartialSettings.IsSavesUnique);

        return isGlobalSaves
            ? Path.Combine(fileSystemService.GetExternalMinecraftPath(), "saves")
            : Path.Combine(pathsService.InstancesDirectory, targetInstance.Id, "saves");
    }

    public async Task<Result<bool>> CreateBackupAsync(
        MinecraftInstance targetInstance, 
        IReadOnlyList<MinecraftInstance> allInstances, 
        GlobalBackupSettings globalSettings, 
        IProgress<InstanceBackupProgress>? progress = null,
        bool force = false)
    {
        if (!force && !IsBackupDue(targetInstance, globalSettings))
            return Result.Ok(false);

        try
        {
            bool isGlobalSaves = targetInstance.IsolationType == IsolationType.Global || 
                                (targetInstance.IsolationType == IsolationType.Partial && !targetInstance.PartialSettings.IsSavesUnique);

            string sourceSavesPath = GetSavesPath(targetInstance);

            if (!Directory.Exists(sourceSavesPath))
            {
                Debug.WriteLine($"[BackupService] Saves directory not found at: {sourceSavesPath}");
                return Result.Ok(false);
            }

            var worldDirectories = Directory.GetDirectories(sourceSavesPath);
            if (worldDirectories.Length == 0)
            {
                Debug.WriteLine($"[BackupService] No world folders found to backup at: {sourceSavesPath}");
                return Result.Ok(false);
            }

            string backupContainerPath = isGlobalSaves
                ? Path.Combine(pathsService.DataDirectory, "Backups", ".minecraft")
                : Path.Combine(pathsService.DataDirectory, "Backups", targetInstance.Id);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string targetSnapshotDirectory = Path.Combine(backupContainerPath, timestamp);
            Directory.CreateDirectory(targetSnapshotDirectory);

            progress?.Report(new InstanceBackupProgress(0, "Making backups..."));

            var worldsData = new List<(string WorldPath, string WorldName, string[] Files, string[] Dirs)>();
            long totalBytes = 0;

            foreach (var worldDir in worldDirectories)
            {
                var files = Directory.GetFiles(worldDir, "*", SearchOption.AllDirectories);
                var dirs = Directory.GetDirectories(worldDir, "*", SearchOption.AllDirectories);
                string worldName = new DirectoryInfo(worldDir).Name;

                worldsData.Add((worldDir, worldName, files, dirs));

                foreach (var f in files)
                {
                    try
                    {
                        totalBytes += new FileInfo(f).Length;
                    }
                    catch { /* ignore */ }
                }
            }

            long processedBytes = 0;

            await Task.Run(async () =>
            {
                byte[] buffer = new byte[81920];

                foreach (var world in worldsData)
                {
                    string targetZipFilePath = Path.Combine(targetSnapshotDirectory, $"{world.WorldName}.zip");

                    using var zipToOpen = new FileStream(
                        targetZipFilePath, 
                        FileMode.Create, 
                        FileAccess.Write, 
                        FileShare.None, 
                        65536, 
                        useAsync: true);
                        
                    using var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create);

                    foreach (var dir in world.Dirs)
                    {
                        string relDir = Path.GetRelativePath(world.WorldPath, dir).Replace('\\', '/');
                        if (!relDir.EndsWith('/')) relDir += '/';
                        archive.CreateEntry(relDir);
                    }

                    for (int i = 0; i < world.Files.Length; i++)
                    {
                        string file = world.Files[i];
                        string relPath = Path.GetRelativePath(world.WorldPath, file).Replace('\\', '/');
                        var entry = archive.CreateEntry(relPath, CompressionLevel.Fastest);

                        using var srcStream = new FileStream(
                            file, 
                            FileMode.Open, 
                            FileAccess.Read, 
                            FileShare.ReadWrite, 
                            65536, 
                            useAsync: true);
                            
                        using var dstStream = entry.Open();

                        int bytesRead;
                        while ((bytesRead = await srcStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await dstStream.WriteAsync(buffer, 0, bytesRead);
                            processedBytes += bytesRead;

                            if (totalBytes > 0)
                            {
                                double percent = Math.Min(99.0, (double)processedBytes / totalBytes * 100.0);
                                progress?.Report(new InstanceBackupProgress(percent, "Making backups..."));
                            }
                        }
                    }
                }
            });

            progress?.Report(new InstanceBackupProgress(100, "Making backups..."));

            int maxBackups = targetInstance.BackupSettings.SavesBackupSettings switch
            {
                BackupPolicy.ForceOn => targetInstance.BackupSettings.SavesMaxBackups ?? globalSettings.SavesMaxBackups,
                _ => globalSettings.SavesMaxBackups
            };

            CleanupOldBackups(backupContainerPath, maxBackups);

            DateTime now = DateTime.Now;
            if (isGlobalSaves)
            {
                var sharedInstances = allInstances.Where(i => 
                    i.IsolationType == IsolationType.Global || 
                    (i.IsolationType == IsolationType.Partial && !i.PartialSettings.IsSavesUnique));

                foreach (var instance in sharedInstances)
                {
                    instance.BackupSettings.LastBackupDate = now;
                }
            }
            else
            {
                targetInstance.BackupSettings.LastBackupDate = now;
            }

            Debug.WriteLine($"[BackupService] Snapshot backup created successfully at: {targetSnapshotDirectory}");
            return Result.Ok(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BackupService Error] Backup failed: {ex.Message}");
            return Result.Fail<bool>(new Error($"Backup archive creation failed: {ex.Message}").CausedBy(ex));
        }
    }

    private static void CleanupOldBackups(string containerDirectory, int maxBackups)
    {
        if (!Directory.Exists(containerDirectory) || maxBackups <= 0) return;

        try
        {
            var backupDirectories = new DirectoryInfo(containerDirectory)
                .GetDirectories()
                .OrderByDescending(d => d.CreationTimeUtc)
                .ToList();

            if (backupDirectories.Count > maxBackups)
            {
                var directoriesToDelete = backupDirectories.Skip(maxBackups);
                foreach (var directory in directoriesToDelete)
                {
                    directory.Delete(recursive: true);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BackupService Warn] Failed to clean up old backups: {ex.Message}");
        }
    }
}