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
        private readonly string _portableGlobalPath;

        public InstanceFileSystemService()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // Путь для портативных инстансов: .../Data/Instances
            _instancesBasePath = Path.Combine(baseDir, "Data", "Instances");
            // Путь для портативного хранилища (Warehouse): .../Data/Global
            _portableGlobalPath = Path.Combine(baseDir, "Data", "Global");
        }

        // Возвращает путь к портативному хранилищу ресурсов
        public string GetGlobalMinecraftPath()
        {
            if (!Directory.Exists(_portableGlobalPath))
            {
                Directory.CreateDirectory(_portableGlobalPath);
            }
            return _portableGlobalPath;
        }

        // Возвращает путь к системному .minecraft в AppData
        private string GetExternalMinecraftPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
        }

        public string PrepareInstance(MinecraftInstance instance)
        {
            switch (instance.IsolationType)
            {
                case IsolationType.Global:
                    // Используем системную папку напрямую
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
            CreateStandardGameFolders(instancePath);
            return instancePath;
        }

        private string PreparePartial(MinecraftInstance instance)
        {
            var instancePath = Path.Combine(_instancesBasePath, instance.Id);
            var globalPath = GetGlobalMinecraftPath();

            CreateDir(instancePath);
            CreateStandardGameFolders(instancePath);

            // Создаем Junctions на общие папки данных
            string[] foldersToLink = { "assets", "libraries", "versions", "runtime", "config", "saves", "resourcepacks", "shaderpacks" };
            foreach (var folder in foldersToLink)
            {
                LinkFolder(instancePath, globalPath, folder);
            }

            return instancePath;
        }

        private void CreateStandardGameFolders(string rootPath)
        {
            string[] folders = { "mods", "config", "saves", "resourcepacks", "shaderpacks", "screenshots", "logs" };
            foreach (var folder in folders) CreateDir(Path.Combine(rootPath, folder));
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

            try { JunctionHelper.CreateJunctionSimple(linkPath, targetPath); }
            catch { CreateDir(linkPath); }
        }
    }
}