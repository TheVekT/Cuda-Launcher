
using System.Diagnostics;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Version;
using CmlLib.Core.Installer.Forge;      
using CmlLib.Core.Installer.NeoForge; 
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ModLoaders.QuiltMC;
using Launcher.Core.Models;
using Launcher.Core.Services.IO;

namespace Launcher.Core.Services.Game
{
    public interface ILaunchService
    {
        Task<Process> LaunchGameAsync(MinecraftInstance instance, UserAccount account, IProgress<double> progress = null);
    }

    public class LaunchService : ILaunchService
    {
        private readonly IInstanceFileSystemService _fileService;
        private readonly HttpClient _httpClient;

        public LaunchService(
            IInstanceFileSystemService fileService)
        {
            _fileService = fileService;
            _httpClient = new HttpClient(); 
        }

        public async Task<Process> LaunchGameAsync(MinecraftInstance instance, UserAccount account, IProgress<double> progress = null)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (account == null) throw new ArgumentNullException(nameof(account));

            var globalPath = _fileService.GetGlobalMinecraftPath();
            var instancePath = _fileService.PrepareInstance(instance); 

            var globalMinecraftPath = new MinecraftPath(globalPath);
            var launcher = new MinecraftLauncher(globalMinecraftPath);

            // Оставляем прогресс для файлов
            launcher.FileProgressChanged += (sender, args) =>
            {
                if (args.TotalTasks > 0)
                {
                    double percent = (double)args.ProgressedTasks / args.TotalTasks * 100;
                    progress?.Report(percent);
                }
            };

            // --- ПЛАН Б: ЗАКОММЕНТИРУЙ ЭТОТ ВЫЗОВ ---
            // var javaPath = await _javaService.GetJavaPathForInstanceAsync(instance, progress);
            
            IVersion versionToLaunch;
            if (instance.LoaderType == GameLoaderType.Vanilla)
                versionToLaunch = await launcher.GetVersionAsync(instance.GameVersion);
            else
                versionToLaunch = await InstallLoaderAsync(launcher, instance);

            var launchOption = new MLaunchOption
            {
                MaximumRamMb = 4096, 
                Session = new MSession(account.Username, account.AccessToken, account.UUID),
                
                // --- ПЛАН Б: УДАЛИ ИЛИ ЗАКОММЕНТИРУЙ ЭТУ СТРОКУ ---
                // JavaPath = javaPath, 
                
                Path = new MinecraftPath(instancePath) 
                {
                    Assets = globalMinecraftPath.Assets,
                    Library = globalMinecraftPath.Library,
                    Runtime = globalMinecraftPath.Runtime,
                    Versions = globalMinecraftPath.Versions
                },
                VersionType = instance.LoaderType.ToString(),
                GameLauncherName = "Launcher"
            };

            // Метод InstallAndBuildProcessAsync сам должен обнаружить отсутствие Java 
            // и запустить встроенный экстрактор.
            // Используйте этот метод, если хотите полный контроль
            var process = await launcher.InstallAndBuildProcessAsync(versionToLaunch.Id, launchOption);

            // ВКЛЮЧАЕМ ПЕРЕХВАТ ЛОГОВ
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.OutputDataReceived += (s, e) => {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine($"[GAME OUT] {e.Data}");
            };
            process.ErrorDataReceived += (s, e) => {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine($"[GAME ERR] {e.Data}");
            };

            process.Start(); // Начинаем чтение
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            return process;
        }

        private async Task<IVersion> InstallLoaderAsync(MinecraftLauncher launcher, MinecraftInstance instance)
        {
            var mcVersion = instance.GameVersion;

            switch (instance.LoaderType)
            {
                case GameLoaderType.Forge:
                {
                    var forge = new ForgeInstaller(launcher); 
                    var versionName = await forge.Install(mcVersion);
                    return await launcher.GetVersionAsync(versionName);
                }
                case GameLoaderType.Fabric:
                {
                    // FabricInstaller требует HttpClient
                    var fabric = new FabricInstaller(_httpClient);
                    var versionName = await fabric.Install(mcVersion, launcher.MinecraftPath);
                    return await launcher.GetVersionAsync(versionName);
                }
                case GameLoaderType.NeoForge:
                {
                    var neo = new NeoForgeInstaller(launcher);
                    var versionName = await neo.Install(mcVersion);
                    return await launcher.GetVersionAsync(versionName);
                }
                case GameLoaderType.Quilt:
                {
                    var quilt = new QuiltInstaller(_httpClient);
                    var versionName = await quilt.Install(mcVersion, launcher.MinecraftPath);
                    return await launcher.GetVersionAsync(versionName);
                }
                default:
                    return await launcher.GetVersionAsync(mcVersion);
            }
        }
    }
}