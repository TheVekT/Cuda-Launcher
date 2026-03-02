
using System.Diagnostics;
using Launcher.Core.Enums;
using Launcher.Core.Helpers;
using Launcher.Core.Models;

namespace Launcher.Core.Services.IO
{
    public interface IInstanceFileSystemService
    {
        // Вызывается 1 раз при создании (ViewModel). Может запросить права Админа.
        Task InitializeOnCreation(MinecraftInstance instance);
        
        // Вызывается перед каждым запуском (LaunchService).
        string PrepareForLaunch(MinecraftInstance instance);
        
        string GetGlobalMinecraftPath();
        void DeleteInstance(MinecraftInstance instance);
        
        void OpenInstanceFolder(MinecraftInstance instance);
        void OpenInstanceModsFolder(MinecraftInstance instance);
        void OpenRootMinecraftFolder();
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
            if (!Directory.Exists(_portableGlobalPath)) Directory.CreateDirectory(_portableGlobalPath);
            return _portableGlobalPath;
        }

        private string GetExternalMinecraftPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
        }

        // --- ЭТАП 1: ИНИЦИАЛИЗАЦИЯ (UI) ---
        public async Task InitializeOnCreation(MinecraftInstance instance)
        {
            var instancePath = Path.Combine(_instancesBasePath, instance.Id);

            switch (instance.IsolationType)
            {
                case IsolationType.Partial:
                    // Самый важный момент: здесь вызываем UAC и создаем ссылки
                    await PreparePartial(instancePath); 
                    break;

                case IsolationType.Full:
                    // Просто создаем папку, файлы скопируем потом (при запуске)
                    if (!Directory.Exists(instancePath)) Directory.CreateDirectory(instancePath);
                    break;

                case IsolationType.Global:
                    // Ничего делать не надо
                    break;
            }
        }

        // --- ЭТАП 2: ПОДГОТОВКА К ЗАПУСКУ (GAME) ---
        public string PrepareForLaunch(MinecraftInstance instance)
        {
            var instancePath = Path.Combine(_instancesBasePath, instance.Id);

            switch (instance.IsolationType)
            {
                case IsolationType.Global:
                    return GetExternalMinecraftPath(); // Или _portableGlobalPath, смотря как у тебя настроено

                case IsolationType.Partial:
                    // Проверяем, жив ли инстанс. Если папки нет — беда, нужны права админа чтобы восстановить.
                    if (!Directory.Exists(instancePath))
                    {
                        throw new DirectoryNotFoundException("Instance folder not found. Please recreate the installation.");
                    }
                    return instancePath;

                case IsolationType.Full:
                    // Ленивая загрузка: если файлов нет, копируем их сейчас
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

        // --- ВНУТРЕННИЕ МЕТОДЫ ---

        private async Task PreparePartial(string instancePath)
        {
            var sourcePath = GetExternalMinecraftPath();

            CreateDir(instancePath);
            CreateDir(Path.Combine(instancePath, "mods")); // Своя папка модов

            // 1. Создаем глобальный "каркас" (чтобы линкам было на что ссылаться)
            CreateDir(sourcePath);
            
            var whitelistFolders = new[] { "config", "resourcepacks", "saves", "schematics", "screenshots", "shaderpacks" };
            var whitelistFiles = new[] { "options.txt", "optionsof.txt" };

            // Создаем папки
            foreach (var folder in whitelistFolders)
            {
                CreateDir(Path.Combine(sourcePath, folder));
            }

            // Создаем файлы
            foreach (var file in whitelistFiles)
            {
                var filePath = Path.Combine(sourcePath, file);
                if (!File.Exists(filePath))
                {
                    File.WriteAllText(filePath, ""); // Пустой файл как заглушка
                }
            }

            // Формируем единый белый список для хелпера
            var inclusionList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in whitelistFolders) inclusionList.Add(item);
            foreach (var item in whitelistFiles) inclusionList.Add(item);
    
            try 
            {
                await Task.Run(() => 
                {
                    // Передаем inclusionList. 
                    // Важно: в самом AdminSymlinkHelper нужно будет инвертировать логику проверок!
                    AdminSymlinkHelper.CreateSymlinksElevated(sourcePath, instancePath, inclusionList);
                });
            }
            catch (Exception ex)
            {
                // Удаляем папку инстанса, чтобы не оставлять мусор при отмене UAC
                if (Directory.Exists(instancePath)) Directory.Delete(instancePath, true);
                throw new Exception("Administrator rights are required to create Partial Isolation links!", ex);
            }
        }

        private bool IsFullInstanceReady(string instancePath)
        {
            // Считаем инстанс готовым, если есть options.txt или папка resourcepacks
            return File.Exists(Path.Combine(instancePath, "options.txt"));
        }

        private void PrepareFullLazy(string instancePath)
        {
            CreateDir(instancePath);
            // Копируем базовые файлы из оригинала для старта
            string[] basicFolders = { "mods", "config", "saves", "screenshots", "resourcepacks" };
            foreach (var folder in basicFolders) CreateDir(Path.Combine(instancePath, folder));
            
            // Тут можно добавить логику копирования options.txt из глобала, если нужно
        }

        private void CreateDir(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
        public void OpenRootMinecraftFolder()
        {
            var path = GetExternalMinecraftPath();
            CreateDir(path); // Безопасно: если там симлинк или папка есть - ничего не сломает
            OpenFolderInExplorer(path);
        }

        public void OpenInstanceFolder(MinecraftInstance instance)
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

        public void OpenInstanceModsFolder(MinecraftInstance instance)
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
                // Правильный способ открыть папку в стандартном проводнике Windows
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
    
}