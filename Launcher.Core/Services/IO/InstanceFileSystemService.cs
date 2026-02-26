
using System.Diagnostics;
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

            if (!Directory.Exists(sourcePath)) return;
    
            var exclusionList = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "assets", "libraries", "versions", "runtime", "runtimes",
                "bin", "cache", "webcache", "crash-reports", "logs", 
                "mods", "launcher_profiles.json", "launcher_accounts.json",
            };
    
            try 
            {
                // Уводим тяжелую работу (создание процессов и ожидание UAC) в фоновый поток
                await Task.Run(() => 
                {
                    AdminSymlinkHelper.CreateSymlinksElevated(sourcePath, instancePath, exclusionList);
                });
            }
            catch (Exception ex)
            {
                // Удаляем папку, чтобы не оставлять мусор
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
    }
}