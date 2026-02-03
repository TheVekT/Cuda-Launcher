using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Launcher.Core.Helpers;
using Launcher.Core.Models;

namespace Launcher.Core.Services.IO
{
    public interface IInstanceFileSystemService
    {
        string PrepareInstance(MinecraftInstance instance);
        string GetGlobalMinecraftPath();
        void DeleteInstance(MinecraftInstance instance);
    }

    public class InstanceFileSystemService : IInstanceFileSystemService
    {
        private readonly string _instancesBasePath;
        private readonly string _portableGlobalPath;

        public InstanceFileSystemService()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _instancesBasePath = Path.Combine(baseDir, "Data", "Instances");
            _portableGlobalPath = Path.Combine(baseDir, "Data", "Global");
        }

        public string GetGlobalMinecraftPath()
        {
            if (!Directory.Exists(_portableGlobalPath))
            {
                Directory.CreateDirectory(_portableGlobalPath);
            }
            return _portableGlobalPath;
        }

        private string GetExternalMinecraftPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
        }

        public string PrepareInstance(MinecraftInstance instance)
        {
            switch (instance.IsolationType)
            {
                case IsolationType.Global:
                    var externalPath = GetExternalMinecraftPath();
                    if (!Directory.Exists(externalPath)) Directory.CreateDirectory(externalPath);
                    return externalPath;

                case IsolationType.Full:
                    return PrepareFull(instance);

                case IsolationType.Partial:
                    return PreparePartial(instance);

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
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error deleting instance: {ex.Message}"); }
            }
        }

        private string PrepareFull(MinecraftInstance instance)
        {
            var instancePath = Path.Combine(_instancesBasePath, instance.Id);
            CreateDir(instancePath);
            // Для полной изоляции папки создаются пустыми
            string[] basicFolders = { "mods", "config", "saves", "screenshots", "resourcepacks" };
            foreach (var folder in basicFolders) CreateDir(Path.Combine(instancePath, folder));
            return instancePath;
        }

        private string PreparePartial(MinecraftInstance instance)
        {
            var instancePath = Path.Combine(_instancesBasePath, instance.Id);
            var sourcePath = GetExternalMinecraftPath();

            CreateDir(instancePath);
            
            CreateDir(Path.Combine(instancePath, "mods"));

            if (!Directory.Exists(sourcePath))
            {
                return instancePath;
            }
            
            var exclusionList = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "assets", 
                "libraries", 
                "versions", 
                "runtime", 
                "runtimes",
                "bin", 
                "cache", 
                "webcache",
                "crash-reports",
                "logs", 
                "mods",
                "launcher_profiles.json",
                "launcher_accounts.json",
            };


            foreach (var dirPath in Directory.GetDirectories(sourcePath))
            {
                var dirName = new DirectoryInfo(dirPath).Name;

                
                if (exclusionList.Contains(dirName)) continue;

                
                LinkFolder(instancePath, sourcePath, dirName);
            }

            
            foreach (var filePath in Directory.GetFiles(sourcePath))
            {
                var fileName = Path.GetFileName(filePath);

                if (exclusionList.Contains(fileName)) continue;

                LinkFile(instancePath, sourcePath, fileName);
            }

            return instancePath;
        }

        private void CreateDir(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        private void LinkFolder(string instanceRoot, string sourceRoot, string folderName)
        {
            var linkPath = Path.Combine(instanceRoot, folderName);
            var targetPath = Path.Combine(sourceRoot, folderName);

            if (Directory.Exists(linkPath)) return;
            if (!Directory.Exists(targetPath)) return; 

            try 
            { 
                JunctionHelper.CreateJunctionSimple(linkPath, targetPath); 
            }
            catch (Exception ex)
            { 
                System.Diagnostics.Debug.WriteLine($"Failed to link folder {folderName}: {ex.Message}");
                CreateDir(linkPath); 
            }
        }

        private void LinkFile(string instanceRoot, string sourceRoot, string fileName)
        {
            var linkPath = Path.Combine(instanceRoot, fileName);
            var targetPath = Path.Combine(sourceRoot, fileName);

            if (File.Exists(linkPath)) return;
            if (!File.Exists(targetPath)) return;

            try
            {
                File.CreateSymbolicLink(linkPath, targetPath);
            }
            catch
            {
                try 
                {
                    File.Copy(targetPath, linkPath, true);
                }
                catch { /**/ }
            }
        }
    }
}