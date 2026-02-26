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
        Task<Process> LaunchGameAsync(MinecraftInstance instance, UserAccount account, GlobalLaunchSettings globalSettings, IProgress<double> progress = null);
    }

    public class LaunchService : ILaunchService
    {
        private readonly IInstanceFileSystemService _fileService;
        private readonly IModrinthService _modrinthService;
        private readonly HttpClient _httpClient;

        public LaunchService(IInstanceFileSystemService fileService, IModrinthService modrinthService)
        {
            _fileService = fileService;
            _modrinthService = modrinthService;
            _httpClient = new HttpClient(); 
        }

        public async Task<Process> LaunchGameAsync(MinecraftInstance instance, UserAccount account, GlobalLaunchSettings globalSettings, IProgress<double> progress = null)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (account == null) throw new ArgumentNullException(nameof(account));
            if (globalSettings == null) throw new ArgumentNullException(nameof(globalSettings));

            // 1. Получаем путь (БЕЗ АДМИН ПРАВ)
            var instancePath = _fileService.PrepareForLaunch(instance); 
            // === УСТАНОВКА МОДОВ (ТОЛЬКО ПРИ ПЕРВОМ ЗАПУСКЕ) ===
            if (instance.LastPlayedDate == null && instance.IsolationType != IsolationType.Global)
            {
                string modsFolder = Path.Combine(instancePath, "mods");

                // 1. Обязательные API (тихие, без исключений)
                await _modrinthService.InstallEssentialApisAsync(instance, modsFolder);

                // 2. Моды на оптимизацию (кидают исключение, если недоступны)
                if (instance.RequestPerformanceMods)
                {
                    try
                    {
                        await _modrinthService.InstallPerformanceModsAsync(instance, modsFolder);
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Здесь мы ловим то самое исключение.
                        // Ты можешь либо прокинуть его выше во ViewModel, чтобы показать окно,
                        // либо временно залогировать.
                        Console.WriteLine($"[WARNING] {ex.Message}");
                        throw; // Прокидываем в UI
                    }
                }
            }
            // 2. Настраиваем логику путей
            var globalPath = _fileService.GetGlobalMinecraftPath();
            var globalMcPath = new MinecraftPath(globalPath);

            // 3. Лаунчер инициализируем с ГЛОБАЛЬНЫМ путем
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

            // === 5. Умное определение параметров запуска ===
            // Защита от NullReference, если вдруг GameSettings не инициализирован
            var safeGameSettings = instance.GameSettings ?? new GameSettings();

            // Определяем финальные значения (Если в инстансе null -> берем глобальные)
            int finalRam = safeGameSettings.AllocatedMemory ?? globalSettings.MaxRamMb;
            bool finalFullscreen = safeGameSettings.Fullscreen ?? globalSettings.IsFullscreen;
            string finalResolutionStr = safeGameSettings.GameResolution ?? globalSettings.Resolution;

            // Парсинг разрешения экрана
            int screenWidth = 854;  // Стандартная ширина Minecraft
            int screenHeight = 480; // Стандартная высота Minecraft

            if (!string.IsNullOrEmpty(finalResolutionStr) && !finalResolutionStr.Equals("Auto", StringComparison.OrdinalIgnoreCase))
            {
                var parts = finalResolutionStr.Split('x', 'X'); // Учитываем 'x' и 'X'
                if (parts.Length == 2 && 
                    int.TryParse(parts[0], out int w) && 
                    int.TryParse(parts[1], out int h))
                {
                    screenWidth = w;
                    screenHeight = h;
                }
            }

            // 6. Опции запуска
            var launchOption = new MLaunchOption
            {
                MaximumRamMb = finalRam, 
                FullScreen = finalFullscreen,
                ScreenWidth = screenWidth,
                ScreenHeight = screenHeight,
                Session = new MSession(account.Username, account.AccessToken, account.UUID),
                
                // ВАЖНО: Рабочая папка - папка инстанса
                Path = new MinecraftPath(instancePath) 
                {
                    Assets = globalMcPath.Assets,
                    Library = globalMcPath.Library,
                    Runtime = globalMcPath.Runtime,
                    Versions = globalMcPath.Versions
                },
                
                VersionType = instance.LoaderType.ToString(),
                GameLauncherName = "Launcher" // Название твоего лаунчера в игре
            };

            // 7. Создание процесса
            var process = await launcher.InstallAndBuildProcessAsync(versionToLaunch.Id, launchOption);

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"[GAME OUT] {e.Data}"); };
            process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"[GAME ERR] {e.Data}"); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            instance.LastPlayedDate = DateTime.Now;
            Console.WriteLine($"Launched instance at time: {instance.LastPlayedDate} | RAM: {finalRam}MB | Fullscreen: {finalFullscreen} | Resolution: {screenWidth}x{screenHeight}");
            
            return process;
        }

        private async Task<IVersion> InstallLoaderAsync(MinecraftLauncher launcher, MinecraftInstance instance)
        {
            var mcVersion = instance.GameVersion;
            var loaderVersion = instance.LoaderVersion;

            Console.WriteLine($"Installing {instance.LoaderType} (Version: {loaderVersion}) for Minecraft {mcVersion}...");

            switch (instance.LoaderType)
            {
                case GameLoaderType.Forge:
                    var forge = new ForgeInstaller(launcher); 
                    var installedForgeId = await forge.Install(mcVersion, loaderVersion);
                    return await launcher.GetVersionAsync(installedForgeId);

                case GameLoaderType.Fabric:
                    var fabric = new FabricInstaller(_httpClient);
                    var installedFabricId = await fabric.Install(mcVersion, loaderVersion, launcher.MinecraftPath);
                    return await launcher.GetVersionAsync(installedFabricId);

                case GameLoaderType.NeoForge:
                    var neo = new NeoForgeInstaller(launcher);
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