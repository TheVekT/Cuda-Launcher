using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.NeoForge;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ModLoaders.QuiltMC;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Version;
using Launcher.Core.Enums;
using Launcher.Core.Models;
using Launcher.Core.Services.IO;

namespace Launcher.Core.Services.Game
{
    public interface ILaunchService
    {
        Task<Process> LaunchGameAsync(MinecraftInstance instance, UserAccount account, GlobalLaunchSettings globalSettings, IProgress<LaunchState> progress = null);
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

        public async Task<Process> LaunchGameAsync(MinecraftInstance instance, UserAccount account, GlobalLaunchSettings globalSettings, IProgress<LaunchState> progress = null)
        {
            if (instance == null || account == null || globalSettings == null) throw new ArgumentNullException();

            progress?.Report(new LaunchState { Progress = 0, StatusText = "Preparing launch environment..." });
            var instancePath = _fileService.PrepareForLaunch(instance); 

            // === 1. УСТАНОВКА МОДОВ ===
            if (instance.LastPlayedDate == null && instance.IsolationType != IsolationType.Global)
            {
                string modsFolder = Path.Combine(instancePath, "mods");
                await _modrinthService.InstallEssentialApisAsync(instance, modsFolder, progress);

                if (instance.RequestPerformanceMods)
                {
                    try { await _modrinthService.InstallPerformanceModsAsync(instance, modsFolder, progress); }
                    catch (InvalidOperationException ex) { Console.WriteLine($"[WARNING] {ex.Message}"); throw; }
                }
            }

            progress?.Report(new LaunchState { Progress = 5, StatusText = "Initializing engine..." });

            var globalPath = _fileService.GetGlobalMinecraftPath();
            var globalMcPath = new MinecraftPath(globalPath);
            var launcher = new MinecraftLauncher(globalMcPath);

            var instanceMcPath = new MinecraftPath(instancePath) 
            {
                Assets = globalMcPath.Assets,
                Library = globalMcPath.Library,
                Runtime = globalMcPath.Runtime,
                Versions = globalMcPath.Versions
            };

            // === 2. ТАЙМЕР ЗАГРУЗКИ (Плавный прогресс от 15% до 90%) ===
            Stopwatch networkTimer = new Stopwatch();
            networkTimer.Start();
            string currentAction = "Verifying files...";

            launcher.ByteProgressChanged += (sender, args) =>
            {
                networkTimer.Restart();
                currentAction = "Downloading...";
            };

            launcher.FileProgressChanged += (sender, args) =>
            {
                if (args.TotalTasks > 0)
                {
                    // Если сеть молчит полсекунды - значит мы просто проверяем локальные файлы
                    if (networkTimer.ElapsedMilliseconds > 500) currentAction = "Verifying files...";
                    
                    // Масштабируем прогресс CmlLib (0-100%) в наш отрезок (15-90%)
                    double percent = 10 + ((double)args.ProgressedTasks / args.TotalTasks * 75);
                    progress?.Report(new LaunchState { Progress = percent, StatusText = currentAction });
                }
            };

            // === 3. ПОЛУЧЕНИЕ ВЕРСИИ И УСТАНОВКА ЛОАДЕРОВ ===
            progress?.Report(new LaunchState { Progress = 15, StatusText = $"Preparing base Minecraft {instance.GameVersion}..." });
            var baseVersion = await launcher.GetVersionAsync(instance.GameVersion);

            IVersion versionToLaunch;
            if (instance.LoaderType == GameLoaderType.Vanilla)
            {
                versionToLaunch = baseVersion;
            }
            else
            {
                progress?.Report(new LaunchState { Progress = 20, StatusText = $"Installing {instance.LoaderType}..." });
                versionToLaunch = await InstallLoaderAsync(launcher, instance);
            }

            progress?.Report(new LaunchState { Progress = 90, StatusText = "Finalizing settings..." });

            // === 4. ОПЦИИ ЗАПУСКА ===
            var safeGameSettings = instance.GameSettings ?? new GameSettings();
            int finalRam = safeGameSettings.AllocatedMemory ?? globalSettings.MaxRamMb;
            bool finalFullscreen = safeGameSettings.Fullscreen ?? globalSettings.IsFullscreen;
            string finalResolutionStr = safeGameSettings.GameResolution ?? globalSettings.Resolution;

            int screenWidth = 854;  
            int screenHeight = 480; 
            if (!string.IsNullOrEmpty(finalResolutionStr) && !finalResolutionStr.Equals("Auto", StringComparison.OrdinalIgnoreCase))
            {
                var parts = finalResolutionStr.Split('x', 'X'); 
                if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h))
                {
                    screenWidth = w; screenHeight = h;
                }
            }

            var launchOption = new MLaunchOption
            {
                MaximumRamMb = finalRam, 
                FullScreen = finalFullscreen,
                ScreenWidth = screenWidth,
                ScreenHeight = screenHeight,
                Session = new MSession(account.Username, account.AccessToken, account.UUID),
                Path = instanceMcPath, 
                VersionType = instance.LoaderType.ToString(),
                GameLauncherName = "Launcher" 
            };

            progress?.Report(new LaunchState { Progress = 95, StatusText = "Starting game process..." });
            
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
            progress?.Report(new LaunchState { Progress = 100, StatusText = "Game started!" });
            return process;
        }

        private async Task<IVersion> InstallLoaderAsync(MinecraftLauncher launcher, MinecraftInstance instance)
        {
            var mcVersion = instance.GameVersion;
            var loaderVersion = instance.LoaderVersion;

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