using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
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

        public LaunchService(IInstanceFileSystemService fileService)
        {
            _fileService = fileService;
            _httpClient = new HttpClient(); 
        }

        public async Task<Process> LaunchGameAsync(MinecraftInstance instance, UserAccount account, IProgress<double> progress = null)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (account == null) throw new ArgumentNullException(nameof(account));

            // 1. Определяем путь запуска (куда игра будет писать конфиги)
            var instancePath = _fileService.PrepareInstance(instance); 
            
            // 2. Определяем путь хранилища (откуда качать библиотеки/ассеты)
            // Если режим Global — качаем прямо в %APPDATA%. Если нет — в портативную папку.
            var storagePath = (instance.IsolationType == IsolationType.Global) 
                ? instancePath 
                : _fileService.GetGlobalMinecraftPath();

            var storageMinecraftPath = new MinecraftPath(storagePath);
            
            // Use shared runtime folder even for Global isolation
            storageMinecraftPath.Runtime = Path.Combine(_fileService.GetGlobalMinecraftPath(), "runtime");

            var launcher = new MinecraftLauncher(storageMinecraftPath);

            launcher.FileProgressChanged += (sender, args) =>
            {
                if (args.TotalTasks > 0)
                {
                    double percent = (double)args.ProgressedTasks / args.TotalTasks * 100;
                    progress?.Report(percent);
                }
            };

            // 3. Получаем версию
            IVersion versionToLaunch;
            if (instance.LoaderType == GameLoaderType.Vanilla)
                versionToLaunch = await launcher.GetVersionAsync(instance.GameVersion);
            else
                versionToLaunch = await InstallLoaderAsync(launcher, instance);

            // 4. Настройка опций запуска
            var launchOption = new MLaunchOption
            {
                MaximumRamMb = 4096, 
                Session = new MSession(account.Username, account.AccessToken, account.UUID),
                
                // Путь запуска (BaseDir) — это папка инстанса.
                // Остальные пути (Assets, Library и т.д.) перенаправляем на хранилище.
                Path = new MinecraftPath(instancePath) 
                {
                    Assets = storageMinecraftPath.Assets,
                    Library = storageMinecraftPath.Library,
                    Runtime = storageMinecraftPath.Runtime,
                    Versions = storageMinecraftPath.Versions
                },
                VersionType = instance.LoaderType.ToString(),
                GameLauncherName = "Launcher"
            };

            // 5. Создание процесса
            var process = await launcher.InstallAndBuildProcessAsync(versionToLaunch.Id, launchOption);

            // Настройка логирования
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"[GAME OUT] {e.Data}"); };
            process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"[GAME ERR] {e.Data}"); };

            process.Start();
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
                    var forge = new ForgeInstaller(launcher); 
                    return await launcher.GetVersionAsync(await forge.Install(mcVersion));
                case GameLoaderType.Fabric:
                    var fabric = new FabricInstaller(_httpClient);
                    return await launcher.GetVersionAsync(await fabric.Install(mcVersion, launcher.MinecraftPath));
                case GameLoaderType.NeoForge:
                    var neo = new NeoForgeInstaller(launcher);
                    return await launcher.GetVersionAsync(await neo.Install(mcVersion));
                case GameLoaderType.Quilt:
                    var quilt = new QuiltInstaller(_httpClient);
                    return await launcher.GetVersionAsync(await quilt.Install(mcVersion, launcher.MinecraftPath));
                default:
                    return await launcher.GetVersionAsync(mcVersion);
            }
        }
    }
}