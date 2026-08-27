using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.NeoForge;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ModLoaders.QuiltMC;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Version;
using CmlLib.Core.VersionLoader;
using FluentResults;
using Launcher.Core.Common.Enums;
using Launcher.Core.Common.Messages;
using Launcher.Core.Common.Models;
using Launcher.Core.Game.Abstractions;
using Launcher.Core.Identity.Models;
using Launcher.Core.Instances.Abstractions;
using Launcher.Core.Instances.Models;
using Launcher.Core.Mods.Abstractions;
using Launcher.Core.System.Abstractions;

namespace Launcher.Core.Game;

public class LaunchService : ILaunchService
{
    private const int MaxLogBufferLines = 60;
    private const int MaxCrashReportSnippetLines = 35;

    private readonly IInstanceFileSystemService _fileService;
    private readonly IModrinthService _modrinthService;
    private readonly IConnectivityService _connectivityService;
    private readonly HttpClient _httpClient;
    
    public event Action<MinecraftInstance, GameCrashReport>? GameCrashed;

    public LaunchService(
        IInstanceFileSystemService fileService, 
        IModrinthService modrinthService, 
        IConnectivityService connectivityService)
    {
        _fileService = fileService;
        _modrinthService = modrinthService;
        _connectivityService = connectivityService;
        _httpClient = new HttpClient(); 
    }

    public async Task<Result<GameLaunchResult>> LaunchGameAsync(
        MinecraftInstance instance, 
        UserAccount account, 
        GlobalLaunchSettings globalSettings, 
        IProgress<GameLaunchProgressMessage> progress)
    {
        if (instance == null || account == null || globalSettings == null)
            return Result.Fail<GameLaunchResult>("Required launch parameters are null.");

        try
        {
            progress?.Report(new GameLaunchProgressMessage(0, "Preparing..."));
            
            bool isConnected = await _connectivityService.CheckInternetAccessAsync();
            var instancePath = _fileService.PrepareForLaunch(instance);
            
            var localProgress = new Progress<LaunchState>(state => 
                progress?.Report(new GameLaunchProgressMessage(state.Progress, state.StatusText)));

            var (isEssentialApisInstalled, isPerformanceModsInstalled) = 
                await InstallInitialModsAsync(instance, instancePath, isConnected, localProgress);

            progress?.Report(new GameLaunchProgressMessage(5, "Initializing..."));

            var globalMcPath = new MinecraftPath(_fileService.GetGlobalMinecraftPath());
            var launcher = new MinecraftLauncher(globalMcPath);

            SetupFileVerificationProgress(launcher, progress);
            
            progress?.Report(new GameLaunchProgressMessage(15, $"Preparing Minecraft {instance.GameVersion}..."));

            string versionIdToLaunch = await ResolveVersionIdAsync(launcher, instance, globalMcPath, isConnected, progress);

            progress?.Report(new GameLaunchProgressMessage(90, "Finalizing..."));
            
            var launchOption = CreateLaunchOption(instance, account, globalSettings, instancePath, globalMcPath);

            progress?.Report(new GameLaunchProgressMessage(95, "Starting game..."));
            
            var process = await BuildProcessAsync(launcher, globalMcPath, versionIdToLaunch, launchOption, isConnected);
            var logBuffer = new ConcurrentQueue<string>();
            ConfigureProcess(process, instance, instancePath, logBuffer);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            instance.LastPlayedDate = DateTime.Now;
            progress?.Report(new GameLaunchProgressMessage(100, "Game started!"));
            
            return Result.Ok(new GameLaunchResult(process, isPerformanceModsInstalled, isEssentialApisInstalled));
        }
        catch (Exception ex)
        {
            return Result.Fail<GameLaunchResult>(new Error($"Launch failed: {ex.Message}").CausedBy(ex));
        }
    }

    private async Task<(bool? Essential, bool? Performance)> InstallInitialModsAsync(
        MinecraftInstance instance, 
        string instancePath, 
        bool isConnected, 
        IProgress<LaunchState> progress)
    {
        if (instance.LastPlayedDate != null || instance.IsolationType == IsolationType.Global)
            return (null, null);

        if (!isConnected)
        {
            if (instance.RequestPerformanceMods)
                instance.RequestPerformanceMods = false;
            return (false, false);
        }

        bool? essentialInstalled = null;
        bool? performanceInstalled = null;
        string modsFolder = Path.Combine(instancePath, "mods");

        try
        {
            essentialInstalled = await _modrinthService.InstallEssentialApisAsync(instance, modsFolder, progress);
        }
        catch (Exception ex)
        {
            essentialInstalled = false;
            Debug.WriteLine($"[Launch Warn] Failed to install Essential APIs: {ex.Message}");
        }

        if (instance.RequestPerformanceMods)
        {
            try
            {
                performanceInstalled = await _modrinthService.InstallPerformanceModsAsync(instance, modsFolder, progress);
            }
            catch (Exception ex)
            {
                performanceInstalled = false;
                Debug.WriteLine($"[Launch Warn] Failed to install Performance mods: {ex.Message}");
            }
            finally
            {
                instance.RequestPerformanceMods = false;
            }
        }

        return (essentialInstalled, performanceInstalled);
    }

    private static void SetupFileVerificationProgress(MinecraftLauncher launcher, IProgress<GameLaunchProgressMessage>? progress)
    {
        var networkTimer = Stopwatch.StartNew();
        string currentAction = "Verifying files...";

        launcher.ByteProgressChanged += (_, _) =>
        {
            networkTimer.Restart();
            currentAction = "Downloading...";
        };

        launcher.FileProgressChanged += (_, args) =>
        {
            if (args.TotalTasks > 0)
            {
                if (networkTimer.ElapsedMilliseconds > 500) 
                    currentAction = "Verifying files...";
                double percent = 10 + ((double)args.ProgressedTasks / args.TotalTasks * 75);
                progress?.Report(new GameLaunchProgressMessage(percent, currentAction));
            }
        };
    }

    private async Task<string> ResolveVersionIdAsync(
        MinecraftLauncher launcher, 
        MinecraftInstance instance, 
        MinecraftPath globalMcPath, 
        bool isConnected, 
        IProgress<GameLaunchProgressMessage>? progress)
    {
        if (isConnected)
        {
            try
            {
                if (instance.LoaderType != GameLoaderType.Vanilla)
                    progress?.Report(new GameLaunchProgressMessage(20, $"Installing {instance.LoaderType}..."));

                var version = await InstallLoaderAsync(launcher, instance);
                return version.Id;
            }
            catch (Exception ex) when (ex is HttpRequestException or SocketException or TaskCanceledException)
            {
                Debug.WriteLine($"[Launch Warn] Network retrieval failed. Checking local version: {ex.Message}");
                var localId = ResolveLocalVersionId(instance, globalMcPath);
                if (string.IsNullOrEmpty(localId)) throw;
                return localId;
            }
        }

        var offlineId = ResolveLocalVersionId(instance, globalMcPath);
        if (string.IsNullOrEmpty(offlineId))
            throw new SocketException((int)SocketError.NetworkDown);

        return offlineId;
    }

    private static MLaunchOption CreateLaunchOption(
        MinecraftInstance instance, 
        UserAccount account, 
        GlobalLaunchSettings globalSettings, 
        string instancePath, 
        MinecraftPath globalMcPath)
    {
        var safeGameSettings = instance.GameSettings ?? new GameSettings();
        var (screenWidth, screenHeight) = ParseResolution(safeGameSettings.GameResolution ?? globalSettings.Resolution);

        var instanceMcPath = new MinecraftPath(instancePath)
        {
            Assets = globalMcPath.Assets,
            Library = globalMcPath.Library,
            Runtime = globalMcPath.Runtime,
            Versions = globalMcPath.Versions
        };

        var option = new MLaunchOption
        {
            MaximumRamMb = safeGameSettings.AllocatedMemory ?? globalSettings.MaxRamMb,
            FullScreen = safeGameSettings.Fullscreen ?? globalSettings.IsFullscreen,
            ScreenWidth = screenWidth,
            ScreenHeight = screenHeight,
            Session = new MSession(account.Username, account.AccessToken, account.UUID),
            Path = instanceMcPath,
            VersionType = instance.LoaderType.ToString(),
            GameLauncherName = "Launcher",
            JavaPath = ResolveJavaPath(globalMcPath, instance)
        };

        if (!string.IsNullOrWhiteSpace(safeGameSettings.JvmArgs))
        {
            option.ExtraJvmArguments = new[] { MArgument.FromCommandLine(safeGameSettings.JvmArgs) };
        }

        return option;
    }

    private static (int Width, int Height) ParseResolution(string? resolutionStr)
    {
        if (!string.IsNullOrEmpty(resolutionStr) && !resolutionStr.Equals("Auto", StringComparison.OrdinalIgnoreCase))
        {
            var parts = resolutionStr.Split('x', 'X');
            if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h))
                return (w, h);
        }
        return (854, 480);
    }

    private static async Task<Process> BuildProcessAsync(
        MinecraftLauncher launcher,
        MinecraftPath globalMcPath,
        string versionId,
        MLaunchOption launchOption,
        bool isConnected)
    {
        if (isConnected)
        {
            try
            {
                return await launcher.InstallAndBuildProcessAsync(versionId, launchOption);
            }
            catch (Exception ex) when (ex is HttpRequestException or SocketException or TaskCanceledException)
            {
                string localJson = Path.Combine(globalMcPath.Versions, versionId, $"{versionId}.json");
                if (!File.Exists(localJson)) throw;
                Debug.WriteLine("[Launch Warn] Online verification failed. Building process offline.");
            }
        }

        var parameters = MinecraftLauncherParameters.CreateDefault(globalMcPath);
        parameters.VersionLoader = new LocalJsonVersionLoader(globalMcPath);
        return await new MinecraftLauncher(parameters).BuildProcessAsync(versionId, launchOption);
    }

    private void ConfigureProcess(
        Process process, 
        MinecraftInstance instance, 
        string instancePath, 
        ConcurrentQueue<string> logBuffer)
    {
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.EnableRaisingEvents = true;

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                AppendToLogBuffer(logBuffer, e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                AppendToLogBuffer(logBuffer, e.Data);
                Debug.WriteLine($"[GAME ERR] {e.Data}");
            }
        };

        var processStartTime = DateTime.UtcNow;
        process.Exited += (_, _) =>
        {
            if (process.ExitCode != 0)
            {
                var (snippet, reportPath) = ExtractCrashDetails(instancePath, processStartTime, logBuffer);
                GameCrashed?.Invoke(instance, new GameCrashReport(process.ExitCode, snippet, reportPath));
            }
            else
            {
                Debug.WriteLine("[GAME] Process finished normally (exit code: 0).");
            }
            process.Dispose();
        };
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

        return instance.LoaderType switch
        {
            GameLoaderType.Forge => await launcher.GetVersionAsync(await new ForgeInstaller(launcher).Install(mcVersion, loaderVersion)),
            GameLoaderType.Fabric => await launcher.GetVersionAsync(await new FabricInstaller(_httpClient).Install(mcVersion, loaderVersion, launcher.MinecraftPath)),
            GameLoaderType.NeoForge => await launcher.GetVersionAsync(await new NeoForgeInstaller(launcher).Install(mcVersion, loaderVersion)),
            GameLoaderType.Quilt => await launcher.GetVersionAsync(await new QuiltInstaller(_httpClient).Install(mcVersion, loaderVersion, launcher.MinecraftPath)),
            _ => await launcher.GetVersionAsync(mcVersion)
        };
    }

    private static string? ResolveLocalVersionId(MinecraftInstance instance, MinecraftPath globalMcPath)
    {
        string versionsDir = globalMcPath.Versions;
        if (!Directory.Exists(versionsDir))
            return null;

        string baseVersionJson = Path.Combine(versionsDir, instance.GameVersion, $"{instance.GameVersion}.json");
        if (!File.Exists(baseVersionJson))
            return null;

        if (instance.LoaderType == GameLoaderType.Vanilla)
            return instance.GameVersion;

        var directories = Directory.GetDirectories(versionsDir)
            .Select(Path.GetFileName)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        // 1. Exact match
        if (!string.IsNullOrEmpty(instance.LoaderVersion))
        {
            var match = directories.FirstOrDefault(id =>
                (instance.LoaderType == GameLoaderType.Fabric && id.Equals($"fabric-loader-{instance.LoaderVersion}-{instance.GameVersion}", StringComparison.OrdinalIgnoreCase)) ||
                (instance.LoaderType == GameLoaderType.Quilt && id.Equals($"quilt-loader-{instance.LoaderVersion}-{instance.GameVersion}", StringComparison.OrdinalIgnoreCase)) ||
                (instance.LoaderType == GameLoaderType.NeoForge && (id.Equals($"neoforge-{instance.LoaderVersion}", StringComparison.OrdinalIgnoreCase) || id.Equals($"{instance.GameVersion}-neoforge-{instance.LoaderVersion}", StringComparison.OrdinalIgnoreCase))) ||
                (instance.LoaderType == GameLoaderType.Forge && (id.Equals($"{instance.GameVersion}-forge-{instance.LoaderVersion}", StringComparison.OrdinalIgnoreCase) || id.Equals($"{instance.GameVersion}-forge{instance.LoaderVersion}", StringComparison.OrdinalIgnoreCase)))
            );
            if (match != null) return match;
        }

        // 2. Fuzzy match
        string loaderTag = instance.LoaderType.ToString();
        return directories.FirstOrDefault(id =>
            id.Contains(loaderTag, StringComparison.OrdinalIgnoreCase) &&
            id.Contains(instance.GameVersion, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ResolveJavaPath(MinecraftPath globalMcPath, MinecraftInstance instance)
    {
        string runtimeDir = globalMcPath.Runtime;
        if (!Directory.Exists(runtimeDir))
            return null;

        string osArch = OperatingSystem.IsWindows() ? "windows-x64" : (OperatingSystem.IsMacOS() ? "mac-os" : "linux");
        string exe = OperatingSystem.IsWindows() ? "javaw.exe" : "java";

        string? FindJava(string component)
        {
            var p1 = Path.Combine(runtimeDir, osArch, component, "bin", exe);
            if (File.Exists(p1)) return p1;
            var p2 = Path.Combine(runtimeDir, component, "bin", exe);
            return File.Exists(p2) ? p2 : null;
        }

        // 1. Check base version JSON manifest
        string baseJson = Path.Combine(globalMcPath.Versions, instance.GameVersion, $"{instance.GameVersion}.json");
        if (File.Exists(baseJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(baseJson));
                if (doc.RootElement.TryGetProperty("javaVersion", out var jv) &&
                    jv.TryGetProperty("component", out var comp) &&
                    comp.GetString() is { Length: > 0 } componentName)
                {
                    var found = FindJava(componentName);
                    if (found != null) return found;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Launch Warn] Failed to parse javaVersion from {baseJson}: {ex.Message}");
            }
        }

        // 2. Version heuristic fallback
        string target = instance.GameVersion switch
        {
            var v when v.StartsWith("26", StringComparison.OrdinalIgnoreCase) => "java-runtime-epsilon",
            var v when IsVersionAtLeast(v, 1, 20, 5) => "java-runtime-delta",
            var v when IsVersionAtLeast(v, 1, 18) => "java-runtime-gamma",
            var v when IsVersionAtLeast(v, 1, 17) => "java-runtime-alpha",
            _ => "jre-legacy"
        };

        return FindJava(target);
    }

    private static bool IsVersionAtLeast(string versionStr, int targetMajor, int targetMinor, int targetBuild = 0)
    {
        var parts = versionStr.Split('.');
        if (parts.Length >= 2 && int.TryParse(parts[0], out int major) && int.TryParse(parts[1], out int minor))
        {
            int build = parts.Length >= 3 && int.TryParse(parts[2], out int b) ? b : 0;
            if (major != targetMajor) return major > targetMajor;
            if (minor != targetMinor) return minor > targetMinor;
            return build >= targetBuild;
        }
        return false;
    }
}