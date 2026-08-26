using System.Collections.Concurrent;
using System.Diagnostics;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.NeoForge;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ModLoaders.QuiltMC;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Version;
using FluentResults;
using Launcher.Core.Common.Enums;
using Launcher.Core.Common.Messages;
using Launcher.Core.Common.Models;
using Launcher.Core.Game.Abstractions;
using Launcher.Core.Identity.Models;
using Launcher.Core.Instances.Abstractions;
using Launcher.Core.Instances.Models;
using Launcher.Core.Mods.Abstractions;

namespace Launcher.Core.Game;

public class LaunchService : ILaunchService
{
    private const int MaxLogBufferLines = 60;
    private const int MaxCrashReportSnippetLines = 35;

    private readonly IInstanceFileSystemService _fileService;
    private readonly IModrinthService _modrinthService;
    private readonly HttpClient _httpClient;
    
    public event Action<MinecraftInstance, GameCrashReport>? GameCrashed;

    public LaunchService(IInstanceFileSystemService fileService, 
        IModrinthService modrinthService)
    {
        _fileService = fileService;
        _modrinthService = modrinthService;
        _httpClient = new HttpClient(); 
    }

    public async Task<Result<Process>> LaunchGameAsync(
        MinecraftInstance instance, 
        UserAccount account, 
        GlobalLaunchSettings globalSettings, 
        IProgress<GameLaunchProgressMessage> progress)
    {
        if (instance == null || account == null || globalSettings == null)
            return Result.Fail<Process>("Required launch parameters are null.");

        try
        {
            progress?.Report(new GameLaunchProgressMessage(0, "Preparing..."));
            
            var instancePath = _fileService.PrepareForLaunch(instance); 
            
            var localProgress = new Progress<LaunchState>(state => 
            {
                progress?.Report(new GameLaunchProgressMessage(state.Progress, state.StatusText));
            });
            
            if (instance.LastPlayedDate == null && instance.IsolationType != IsolationType.Global)
            {
                string modsFolder = Path.Combine(instancePath, "mods");
                await _modrinthService.InstallEssentialApisAsync(instance, modsFolder, localProgress);

                if (instance.RequestPerformanceMods)
                {
                    try
                    {
                        await _modrinthService.InstallPerformanceModsAsync(instance, modsFolder, localProgress);
                    }
                    catch (InvalidOperationException ex)
                    {
                        instance.RequestPerformanceMods = false;
                        Console.WriteLine($"[WARNING] {ex.Message}");
                    }
                }
            }

            progress?.Report(new GameLaunchProgressMessage(5, "Initializing..."));

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
                    
                    progress?.Report(new GameLaunchProgressMessage(percent, currentAction));
                }
            };
            
            progress?.Report(new GameLaunchProgressMessage(15, $"Preparing Minecraft {instance.GameVersion}..."));
            var baseVersion = await launcher.GetVersionAsync(instance.GameVersion);

            IVersion versionToLaunch;
            if (instance.LoaderType == GameLoaderType.Vanilla)
            {
                versionToLaunch = baseVersion;
            }
            else
            {
                progress?.Report(new GameLaunchProgressMessage(20, $"Installing {instance.LoaderType}..."));
                versionToLaunch = await InstallLoaderAsync(launcher, instance);
            }

            progress?.Report(new GameLaunchProgressMessage(90, "Finalizing..."));
            
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

            progress?.Report(new GameLaunchProgressMessage(95, "Starting game..."));
            
            var process = await launcher.InstallAndBuildProcessAsync(versionToLaunch.Id, launchOption);

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.EnableRaisingEvents = true;

            var logBuffer = new ConcurrentQueue<string>();

            process.OutputDataReceived += (s, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;
                AppendToLogBuffer(logBuffer, e.Data);
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;
                AppendToLogBuffer(logBuffer, e.Data);
                Debug.WriteLine($"[GAME ERR] {e.Data}");
            };

            DateTime processStartTime = DateTime.UtcNow;

            process.Exited += (s, e) =>
            {
                int exitCode = process.ExitCode;
                if (exitCode != 0)
                {
                    var (snippet, reportPath) = ExtractCrashDetails(instancePath, processStartTime, logBuffer);
                    var crashReport = new GameCrashReport(exitCode, snippet, reportPath);

                    GameCrashed?.Invoke(instance, crashReport);
                }
                else
                {
                    Debug.WriteLine("[GAME] Process finished normally (exit code: 0).");
                }

                process.Dispose();
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            instance.LastPlayedDate = DateTime.Now;
            progress?.Report(new GameLaunchProgressMessage(100, "Game started!"));
            
            return Result.Ok(process);
        }
        catch (Exception ex)
        {
            return Result.Fail<Process>(new Error($"Launch failed: {ex.Message}").CausedBy(ex));
        }
    }

    private static void AppendToLogBuffer(ConcurrentQueue<string> buffer, string line)
    {
        buffer.Enqueue(line);
        while (buffer.Count > MaxLogBufferLines)
        {
            buffer.TryDequeue(out _);
        }
    }

    private static (string Snippet, string? ReportPath) ExtractCrashDetails(
        string instancePath, 
        DateTime processStartTime, 
        ConcurrentQueue<string> logBuffer)
    {
        try
        {
            string crashReportsDir = Path.Combine(instancePath, "crash-reports");
            if (Directory.Exists(crashReportsDir))
            {
                var directoryInfo = new DirectoryInfo(crashReportsDir);
                var latestReport = directoryInfo.GetFiles("crash-*.txt")
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .FirstOrDefault();

                if (latestReport != null && latestReport.LastWriteTimeUtc >= processStartTime.AddMinutes(-1))
                {
                    var lines = File.ReadLines(latestReport.FullName).Take(MaxCrashReportSnippetLines);
                    return ($"{latestReport.Name}\n" + string.Join(Environment.NewLine, lines), latestReport.FullName);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WARNING] Failed to inspect crash-reports directory: {ex.Message}");
        }

        if (!logBuffer.IsEmpty)
        {
            return ("[From stderr/stdout buffer]\n" + string.Join(Environment.NewLine, logBuffer.ToArray()), null);
        }

        return ("No crash report file or standard output captured.", null);
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