using System.Diagnostics;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.NeoForge;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ModLoaders.QuiltMC;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Version;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Common.Enums;
using Launcher.Core.Common.Messaging;
using Launcher.Core.Common.Models;
using Launcher.Core.Game.Abstractions;
using Launcher.Core.Identity.Models;
using Launcher.Core.Instances.Abstractions;
using Launcher.Core.Instances.Models;
using Launcher.Core.Mods.Abstractions;
using Launcher.Core.UI.Abstractions;

namespace Launcher.Core.Game;

public class LaunchService : ILaunchService
{
    private readonly IInstanceFileSystemService _fileService;
    private readonly IModrinthService _modrinthService;
    private readonly INotificationService _notificationService;
    private readonly ILocalizationService _localizationService;
    private readonly HttpClient _httpClient;

    public LaunchService(IInstanceFileSystemService fileService, 
        IModrinthService modrinthService, 
        INotificationService notificationService, 
        ILocalizationService localizationService)
    {
        _fileService = fileService;
        _modrinthService = modrinthService;
        _notificationService = notificationService;
        _localizationService = localizationService;
        _httpClient = new HttpClient(); 
    }

    public async Task<Process> LaunchGameAsync(MinecraftInstance instance, UserAccount account, GlobalLaunchSettings globalSettings)
    {
        if (instance == null || account == null || globalSettings == null) throw new ArgumentNullException();

        // Отправляем первое сообщение напрямую в шину
        WeakReferenceMessenger.Default.Send(new GameLaunchProgressMessage(0, "Preparing..."));
        
        var instancePath = _fileService.PrepareForLaunch(instance); 

        // АДАПТЕР: Создаем локальный IProgress для старых сервисов (Modrinth), 
        // который будет перенаправлять их прогресс в наш Messenger
        var localProgress = new Progress<LaunchState>(state => 
        {
            WeakReferenceMessenger.Default.Send(new GameLaunchProgressMessage(state.Progress, state.StatusText));
        });

        // === 1. УСТАНОВКА МОДОВ ===
        if (instance.LastPlayedDate == null && instance.IsolationType != IsolationType.Global)
        {
            string modsFolder = Path.Combine(instancePath, "mods");
            // Передаем адаптер
            await _modrinthService.InstallEssentialApisAsync(instance, modsFolder, localProgress);

            if (instance.RequestPerformanceMods)
            {
                try
                {
                    await _modrinthService.InstallPerformanceModsAsync(instance, modsFolder, localProgress);
                }
                catch (InvalidOperationException ex)
                {
                    var title = _localizationService["Errors.Iris&SodiumNotSupportedTitle"];
                    var desc = string.Format(_localizationService["Errors.Iris&SodiumNotSupportedDesc"], instance.GameVersion, instance.LoaderType);
                    _notificationService.ShowError(title, desc);
                    instance.RequestPerformanceMods = false;
                    Console.WriteLine($"[WARNING] {ex.Message}");
                }
            }
        }

        WeakReferenceMessenger.Default.Send(new GameLaunchProgressMessage(5, "Initializing..."));

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

        // === 2. ТАЙМЕР ЗАГРУЗКИ ===
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
                if (networkTimer.ElapsedMilliseconds > 500) currentAction = "Verifying files...";
                double percent = 10 + ((double)args.ProgressedTasks / args.TotalTasks * 75);
                
                WeakReferenceMessenger.Default.Send(new GameLaunchProgressMessage(percent, currentAction));
            }
        };

        // === 3. ПОЛУЧЕНИЕ ВЕРСИИ И УСТАНОВКА ЛОАДЕРОВ ===
        WeakReferenceMessenger.Default.Send(new GameLaunchProgressMessage(15, $"Preparing Minecraft {instance.GameVersion}..."));
        var baseVersion = await launcher.GetVersionAsync(instance.GameVersion);

        IVersion versionToLaunch;
        if (instance.LoaderType == GameLoaderType.Vanilla)
        {
            versionToLaunch = baseVersion;
        }
        else
        {
            WeakReferenceMessenger.Default.Send(new GameLaunchProgressMessage(20, $"Installing {instance.LoaderType}..."));
            versionToLaunch = await InstallLoaderAsync(launcher, instance);
        }

        WeakReferenceMessenger.Default.Send(new GameLaunchProgressMessage(90, "Finalizing..."));

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

        WeakReferenceMessenger.Default.Send(new GameLaunchProgressMessage(95, "Starting game..."));
        
        var process = await launcher.InstallAndBuildProcessAsync(versionToLaunch.Id, launchOption);

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"[GAME OUT] {e.Data}"); };
        process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"[GAME ERR] {e.Data}"); };
        
        process.EnableRaisingEvents = true;
        process.Exited += (s, e) => 
        {
            WeakReferenceMessenger.Default.Send(new GameLaunchStateMessage(false, null));
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        instance.LastPlayedDate = DateTime.Now;
        WeakReferenceMessenger.Default.Send(new GameLaunchProgressMessage(100, "Game started!"));
        
        WeakReferenceMessenger.Default.Send(new GameLaunchStateMessage(true, process));
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