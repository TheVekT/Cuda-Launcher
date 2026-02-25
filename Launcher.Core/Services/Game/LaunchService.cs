using System.Diagnostics;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.NeoForge;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ModLoaders.QuiltMC;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Version;
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

            // 1. Получаем путь (БЕЗ АДМИН ПРАВ)
            var instancePath = _fileService.PrepareForLaunch(instance); 
            
            // 2. Настраиваем логику путей
            // Global Path - это где лежат версии, библиотеки, ассеты.
            // Instance Path - это где игра запустится (mods, configs, saves).
            var globalPath = _fileService.GetGlobalMinecraftPath();
            var globalMcPath = new MinecraftPath(globalPath);

            // 3. Лаунчер инициализируем с ГЛОБАЛЬНЫМ путем (для загрузки версий)
            var launcher = new MinecraftLauncher(globalMcPath);

            launcher.FileProgressChanged += (sender, args) =>
            {
                if (args.TotalTasks > 0)
                {
                    double percent = (double)args.ProgressedTasks / args.TotalTasks * 100;
                    progress?.Report(percent);
                }
            };

            // 4. Получаем версию
            IVersion versionToLaunch;
            if (instance.LoaderType == GameLoaderType.Vanilla)
                versionToLaunch = await launcher.GetVersionAsync(instance.GameVersion);
            else
                versionToLaunch = await InstallLoaderAsync(launcher, instance);

            // 5. Опции запуска
            var launchOption = new MLaunchOption
            {
                MaximumRamMb = 4096, 
                Session = new MSession(account.Username, account.AccessToken, account.UUID),
                
                // ВАЖНО: Тут мы говорим CmlLib "Используй instancePath как рабочую папку"
                Path = new MinecraftPath(instancePath) 
                {
                    // Но ресурсы и версии бери из общего хранилища!
                    Assets = globalMcPath.Assets,
                    Library = globalMcPath.Library,
                    Runtime = globalMcPath.Runtime,
                    Versions = globalMcPath.Versions
                },
                
                VersionType = instance.LoaderType.ToString(),
                GameLauncherName = "Launcher"
            };

            // 6. Создание процесса
            var process = await launcher.InstallAndBuildProcessAsync(versionToLaunch.Id, launchOption);

            // ... Настройка потоков вывода (как у тебя было) ...
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"[GAME OUT] {e.Data}"); };
            process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"[GAME ERR] {e.Data}"); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            instance.LastPlayedDate = DateTime.Now;
            Console.WriteLine($"Launched instance at time: {instance.LastPlayedDate}");
            return process;
        }

        private async Task<IVersion> InstallLoaderAsync(MinecraftLauncher launcher, MinecraftInstance instance)
        {
            var mcVersion = instance.GameVersion;
            var loaderVersion = instance.LoaderVersion; // Теперь он гарантированно есть

            Console.WriteLine($"Installing {instance.LoaderType} (Version: {loaderVersion}) for Minecraft {mcVersion}...");

            switch (instance.LoaderType)
            {
                case GameLoaderType.Forge:
                    var forge = new ForgeInstaller(launcher); 
                    // Передаем конкретную версию лоадера вторым параметром
                    var installedForgeId = await forge.Install(mcVersion, loaderVersion);
                    return await launcher.GetVersionAsync(installedForgeId);

                case GameLoaderType.Fabric:
                    var fabric = new FabricInstaller(_httpClient);
                    var installedFabricId = await fabric.Install(mcVersion, loaderVersion, launcher.MinecraftPath);
                    return await launcher.GetVersionAsync(installedFabricId);

                case GameLoaderType.NeoForge:
                    var neo = new NeoForgeInstaller(launcher);
                    // Вся огромная логика поиска и сортировок удалена! Просто просим установить нужную.
                    var installedNeoId = await neo.Install(mcVersion, loaderVersion);
                    return await launcher.GetVersionAsync(installedNeoId);

                case GameLoaderType.Quilt:
                    var quilt = new QuiltInstaller(_httpClient);
                    var installedQuiltId = await quilt.Install(mcVersion, loaderVersion, launcher.MinecraftPath);
                    return await launcher.GetVersionAsync(installedQuiltId);

                default:
                    return await launcher.GetVersionAsync(mcVersion);
            }
        }
    }
}