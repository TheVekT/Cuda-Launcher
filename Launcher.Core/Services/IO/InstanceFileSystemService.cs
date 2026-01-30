using System;
using System.IO;
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
        private readonly string _globalBasePath; // Новое поле для кеширования пути

        public InstanceFileSystemService()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // Data/Instances
            _instancesBasePath = Path.Combine(baseDir, "Data", "Instances");
            
            // ИСПРАВЛЕНИЕ: Используем локальную папку Data/Global, а не AppData
            _globalBasePath = Path.Combine(baseDir, "Data", "Global");
        }

        public string GetGlobalMinecraftPath()
        {
            // Создаем папку, если её нет (это критично для первого запуска)
            if (!Directory.Exists(_globalBasePath))
            {
                Directory.CreateDirectory(_globalBasePath);
            }
            return _globalBasePath;
        }

        public string PrepareInstance(MinecraftInstance instance)
        {
            switch (instance.IsolationType)
            {
                case IsolationType.Global:
                    return PrepareGlobal();
                case IsolationType.Full:
                    return PrepareFull(instance);
                case IsolationType.Partial:
                    return PreparePartial(instance);
                default:
                    return PrepareGlobal();
            }
        }

        public void DeleteInstance(MinecraftInstance instance)
        {
            if (instance.IsolationType == IsolationType.Global) return;
            if (string.IsNullOrWhiteSpace(instance.Id)) return;

            var instancePath = Path.Combine(_instancesBasePath, instance.Id);
            var globalPath = GetGlobalMinecraftPath();

            if (string.Equals(instancePath, globalPath, StringComparison.OrdinalIgnoreCase)) return;
            if (!instancePath.Contains("Data") || !instancePath.Contains("Instances")) return;

            if (Directory.Exists(instancePath))
            {
                try
                {
                    Directory.Delete(instancePath, true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при удалении папки инстанса: {ex.Message}");
                }
            }
        }

        private string PrepareGlobal()
        {
            return GetGlobalMinecraftPath();
        }

        private string PrepareFull(MinecraftInstance instance)
        {
            var instancePath = Path.Combine(_instancesBasePath, instance.Id);
            CreateDir(instancePath);
            CreateStandardGameFolders(instancePath);

            CreateDir(Path.Combine(instancePath, "assets"));
            CreateDir(Path.Combine(instancePath, "libraries"));
            CreateDir(Path.Combine(instancePath, "versions"));
            CreateDir(Path.Combine(instancePath, "runtime"));
            CreateDir(Path.Combine(instancePath, "webcache"));

            return instancePath;
        }

        private string PreparePartial(MinecraftInstance instance)
        {
            var instancePath = Path.Combine(_instancesBasePath, instance.Id);
            var globalPath = GetGlobalMinecraftPath();

            CreateDir(instancePath);
            CreateStandardGameFolders(instancePath);

            // Создаем Junctions на папки из Data/Global
            LinkFolder(instancePath, globalPath, "config");
            LinkFolder(instancePath, globalPath, "saves");
            LinkFolder(instancePath, globalPath, "logs");
            LinkFolder(instancePath, globalPath, "resourcepacks");
            LinkFolder(instancePath, globalPath, "screenshots");
            LinkFolder(instancePath, globalPath, "shaderpacks");
            
            LinkFolder(instancePath, globalPath, "assets");
            LinkFolder(instancePath, globalPath, "libraries");
            LinkFolder(instancePath, globalPath, "versions");
            LinkFolder(instancePath, globalPath, "runtime");
            LinkFolder(instancePath, globalPath, "webcache");

            return instancePath;
        }

        private void CreateStandardGameFolders(string rootPath)
        {
            CreateDir(Path.Combine(rootPath, "mods"));
            CreateDir(Path.Combine(rootPath, "config"));
            CreateDir(Path.Combine(rootPath, "saves"));
            CreateDir(Path.Combine(rootPath, "resourcepacks"));
            CreateDir(Path.Combine(rootPath, "shaderpacks"));
            CreateDir(Path.Combine(rootPath, "screenshots"));
            CreateDir(Path.Combine(rootPath, "logs"));
        }

        private void CreateDir(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        private void LinkFolder(string instanceRoot, string globalRoot, string folderName)
        {
            var linkPath = Path.Combine(instanceRoot, folderName);
            var targetPath = Path.Combine(globalRoot, folderName);

            if (Directory.Exists(linkPath)) return;
            if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);

            try
            {
                JunctionHelper.CreateJunctionSimple(linkPath, targetPath);
            }
            catch 
            {
                CreateDir(linkPath);
            }
        }
    }
}