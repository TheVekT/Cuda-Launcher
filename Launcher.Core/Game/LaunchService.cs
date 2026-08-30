using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
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
using Launcher.Core.Game.Exceptions;
using Launcher.Core.Game.Models;
using Launcher.Core.Game.Validation;
using Launcher.Core.Identity.Models;
using Launcher.Core.Instances.Abstractions;
using Launcher.Core.Instances.Models;
using Launcher.Core.Mods.Abstractions;
using Launcher.Core.System.Abstractions;

namespace Launcher.Core.Game;

public class LaunchService(
    IInstanceFileSystemService fileService,
    IModrinthService modrinthService,
    IConnectivityService connectivityService,
    IJavaPathResolver javaPathResolver,
    JvmArgumentsValidator jvmValidator)
    : ILaunchService
{
    private const int MaxLogBufferLines = 60;
    private const int MaxCrashReportSnippetLines = 35;

    private readonly HttpClient _httpClient = new();
    
    public event Action<MinecraftInstance, GameCrashReport>? GameCrashed;

    public async Task<Result<GameLaunchResult>> LaunchGameAsync(
        MinecraftInstance instance, 
        UserAccount account, 
        GlobalLaunchSettings globalSettings, 
        IProgress<GameLaunchProgressMessage> progress)
    {

        try
        {
            progress.Report(new GameLaunchProgressMessage(0, "Preparing..."));
            
            var isConnected = await connectivityService.CheckInternetAccessAsync();
            var instancePath = fileService.PrepareForLaunch(instance);
            
            var localProgress = new Progress<LaunchState>(state => 
                progress.Report(new GameLaunchProgressMessage(state.Progress, state.StatusText)));

            var (isEssentialApisInstalled, isPerformanceModsInstalled) = 
                await InstallInitialModsAsync(instance, instancePath, isConnected, localProgress);

            progress.Report(new GameLaunchProgressMessage(5, "Initializing..."));

            var globalMcPath = new MinecraftPath(fileService.GetSharedGameDataPath());
            var launcher = new MinecraftLauncher(globalMcPath);

            SetupFileVerificationProgress(launcher, progress);
            
            progress.Report(new GameLaunchProgressMessage(10, $"Preparing Minecraft {instance.GameVersion}..."));

            string versionIdToLaunch = await ResolveVersionIdAsync(launcher, instance, globalMcPath, isConnected, progress);

            
            string javaBinaryPath = javaPathResolver.ResolveJavaPath(globalMcPath, instance) ?? "javaw.exe";

            var safeGameSettings = instance.GameSettings;
            string? instanceJvmArgs = safeGameSettings.JvmArgs;
            string? globalJvmArgs = globalSettings.JvmArgs;

            string? effectiveJvmArgs = null;
            string? skippedGlobalArgsWarning = null;

            if (!string.IsNullOrWhiteSpace(instanceJvmArgs))
            {
                // Priority 1: Instance JVM arguments (fails launch if invalid)
                var validation = await jvmValidator.ValidateAsync(javaBinaryPath, instanceJvmArgs);
                if (!validation.IsValid)
                {
                    string errorMsg = !string.IsNullOrWhiteSpace(validation.RejectedArgument)
                        ? $"Invalid argument: '{validation.RejectedArgument}'"
                        : (validation.ErrorMessage ?? "Unknown JVM error");

                    var ex = new InvalidJvmArgumentsException(errorMsg, validation.RejectedArgument);
                    return Result.Fail<GameLaunchResult>(new ExceptionalError(ex));
                }

                foreach (var issue in validation.SemanticIssues)
                {
                    Debug.WriteLine($"[Launch Warn] Instance JVM semantic issue: {issue.Message}");
                }

                effectiveJvmArgs = instanceJvmArgs;
            }
            else if (!string.IsNullOrWhiteSpace(globalJvmArgs))
            {
                // Priority 2: Global JVM arguments fallback (launch proceeds without them if invalid)
                var validation = await jvmValidator.ValidateAsync(javaBinaryPath, globalJvmArgs);
                if (validation.IsValid)
                {
                    foreach (var issue in validation.SemanticIssues)
                    {
                        Debug.WriteLine($"[Launch Warn] Global JVM semantic issue: {issue.Message}");
                    }

                    effectiveJvmArgs = globalJvmArgs;
                }
                else
                {
                    effectiveJvmArgs = null;
                    skippedGlobalArgsWarning = !string.IsNullOrWhiteSpace(validation.RejectedArgument)
                        ? validation.RejectedArgument
                        : (validation.ErrorMessage ?? "Incompatible JVM flags");

                    Debug.WriteLine($"[Launch Warn] Ignored invalid global JVM arguments: {skippedGlobalArgsWarning}");
                }
            }

            var launchOption = CreateLaunchOption(instance, account, globalSettings, instancePath, globalMcPath, effectiveJvmArgs);
            
            var process = await BuildProcessAsync(launcher, globalMcPath, versionIdToLaunch, launchOption, isConnected);
            
            progress.Report(new GameLaunchProgressMessage(95, "Starting game..."));

            var logBuffer = new ConcurrentQueue<string>();
            ConfigureProcess(process, instance, instancePath, logBuffer);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            instance.LastPlayedDate = DateTime.Now;
            progress.Report(new GameLaunchProgressMessage(100, "Game started!"));
            
            return Result.Ok(new GameLaunchResult(process, isPerformanceModsInstalled, isEssentialApisInstalled, skippedGlobalArgsWarning));
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

        if (instance.LoaderType == GameLoaderType.Fabric || instance.LoaderType == GameLoaderType.Quilt)
        {
            try
            {
                essentialInstalled = await modrinthService.InstallEssentialApisAsync(instance, modsFolder, progress);
            }
            catch (Exception ex)
            {
                essentialInstalled = false;
                Debug.WriteLine($"[Launch Warn] Failed to install Essential APIs: {ex.Message}");
            }
        }
        
        if (instance.RequestPerformanceMods)
        {
            try
            {
                performanceInstalled = await modrinthService.InstallPerformanceModsAsync(instance, modsFolder, progress);
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
        var currentAction = "Verifying files...";

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
                double percent = 20 + ((double)args.ProgressedTasks / args.TotalTasks * 74);
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

    private MLaunchOption CreateLaunchOption(
        MinecraftInstance instance, 
        UserAccount account, 
        GlobalLaunchSettings globalSettings, 
        string instancePath, 
        MinecraftPath globalMcPath,
        string? effectiveJvmArgs)
    {
        var safeGameSettings = instance.GameSettings;
        var (screenWidth, screenHeight) = ParseResolution(safeGameSettings.GameResolution ?? globalSettings.GameResolution);

        var instanceMcPath = new MinecraftPath(instancePath)
        {
            Assets = globalMcPath.Assets,
            Library = globalMcPath.Library,
            Runtime = globalMcPath.Runtime,
            Versions = globalMcPath.Versions
        };

        var option = new MLaunchOption
        {
            MaximumRamMb = safeGameSettings.AllocatedMemory ?? globalSettings.AllocatedMemory,
            FullScreen = safeGameSettings.Fullscreen ?? globalSettings.Fullscreen,
            ScreenWidth = screenWidth,
            ScreenHeight = screenHeight,
            Session = new MSession(account.Username, account.AccessToken, account.UUID),
            Path = instanceMcPath,
            VersionType = instance.LoaderType.ToString(),
            GameLauncherName = "Launcher",
            JavaPath = javaPathResolver.ResolveJavaPath(globalMcPath, instance)
        };

        if (!string.IsNullOrWhiteSpace(effectiveJvmArgs))
        {
            option.ExtraJvmArguments = [MArgument.FromCommandLine(effectiveJvmArgs)];
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
            GameLoaderType.Forge => await launcher.GetVersionAsync(await new ForgeInstaller(launcher).Install(mcVersion, loaderVersion!)),
            GameLoaderType.Fabric => await launcher.GetVersionAsync(await new FabricInstaller(_httpClient).Install(mcVersion, loaderVersion!, launcher.MinecraftPath)),
            GameLoaderType.NeoForge => await launcher.GetVersionAsync(await new NeoForgeInstaller(launcher).Install(mcVersion, loaderVersion!)),
            GameLoaderType.Quilt => await launcher.GetVersionAsync(await new QuiltInstaller(_httpClient).Install(mcVersion, loaderVersion!, launcher.MinecraftPath)),
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
            .OfType<string>()
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
}